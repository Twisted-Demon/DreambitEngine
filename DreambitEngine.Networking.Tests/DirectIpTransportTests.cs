using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Dreambit;
using Dreambit.Networking;
using Dreambit.Networking.Direct;
using Dreambit.Networking.Messaging;
using Dreambit.Networking.Replication;
using Dreambit.Networking.Transport;
using Dreambit.Networking.Protocol;
using Dreambit.Networking.World;
using Dreambit.ECS;
using Xunit;

namespace DreambitEngine.Networking.Tests;

public sealed class DirectIpTransportTests
{
    [Fact]
    public void LocalhostTransportCarriesReliableAndUnreliablePayloads()
    {
        var port = ReservePort();
        using var server = DirectIpTransport.Listen(port, address: IPAddress.Loopback);
        using var client = DirectIpTransport.Connect("127.0.0.1", port);

        server.StartServer();
        client.Connect();
        var (serverConnection, clientConnection) = WaitForConnections(server, client);

        client.Send(clientConnection, [1, 2, 3], NetworkDelivery.ReliableOrdered, 0);
        var reliable = WaitForData(server);
        Assert.Equal(NetworkDelivery.ReliableOrdered, reliable.Delivery);
        Assert.Equal(new byte[] { 1, 2, 3 }, reliable.Payload.ToArray());

        client.Send(clientConnection, [4, 5, 6], NetworkDelivery.UnreliableSequenced, 3);
        var unreliable = WaitForData(server);
        Assert.Equal(NetworkDelivery.UnreliableSequenced, unreliable.Delivery);
        Assert.Equal((byte)3, unreliable.Channel);
        Assert.Equal(new byte[] { 4, 5, 6 }, unreliable.Payload.ToArray());

        server.Disconnect(serverConnection);
        var disconnected = WaitForEvent(client, TransportEventKind.Disconnected);
        Assert.Equal(TransportDisconnectReason.LocalShutdown, disconnected.Reason);
        Assert.Equal(TransportState.Stopped, client.State);
    }

    [Fact]
    public void SessionHandshakeCompletesOverDirectIpTransport()
    {
        var port = ReservePort();
        using var serverTransport = DirectIpTransport.Listen(port, address: IPAddress.Loopback);
        using var clientTransport = DirectIpTransport.Connect("127.0.0.1", port);
        using var server = CreateSession(NetworkRole.Server, serverTransport);
        using var client = CreateSession(NetworkRole.Client, clientTransport);

        server.Start();
        client.Start();
        PumpUntil(() => client.IsConnected, server, client);

        Assert.Equal(server.SessionId, client.SessionId);
        Assert.True(client.LocalPeerId.IsValid);
        Assert.Equal(1, server.ReadyPeerCount);
    }

    [Fact]
    public void UnreliablePayloadBoundIsEnforced()
    {
        var options = new DirectIpOptions { MaxUnreliablePayload = 128 };
        var port = ReservePort();
        using var server = DirectIpTransport.Listen(port, options, IPAddress.Loopback);
        using var client = DirectIpTransport.Connect("127.0.0.1", port, options);
        server.StartServer();
        client.Connect();
        var (_, clientConnection) = WaitForConnections(server, client);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            client.Send(clientConnection, new byte[129], NetworkDelivery.UnreliableSequenced, 2));
    }

    [Fact]
    public void EventQueueCapacityIsValidated()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DirectIpTransport.Listen(
                ReservePort(),
                new DirectIpOptions { MaxQueuedEvents = 0 },
                IPAddress.Loopback));
    }

    [Fact]
    public void DirectIpEndToEndSynchronizesSpawnStateRequestDespawnAndDisconnect()
    {
        var port = ReservePort();
        using var serverTransport = DirectIpTransport.Listen(port, address: IPAddress.Loopback);
        using var clientTransport = DirectIpTransport.Connect("127.0.0.1", port);
        var serverReplication = new NetworkReplicationRegistry();
        var clientReplication = new NetworkReplicationRegistry();
        serverReplication.Register<DirectState>();
        clientReplication.Register<DirectState>();
        DirectInput? receivedInput = null;
        var serverMessages = CreateInputMessages((_, input) => receivedInput = input);
        var clientMessages = CreateInputMessages((_, _) => { });
        using var server = new NetworkSession(
            NetworkRole.Server,
            serverTransport,
            new NetworkOptions { GameBuildId = "direct-foundation" },
            serverMessages,
            serverReplication);
        using var client = new NetworkSession(
            NetworkRole.Client,
            clientTransport,
            new NetworkOptions { GameBuildId = "direct-foundation" },
            clientMessages,
            clientReplication);
        using var serverScene = new DirectTestScene();
        DirectTestScene? clientScene = null;
        client.SceneChangeRequested += (_, _) =>
        {
            clientScene = new DirectTestScene();
            client.AfterSceneAssigned(clientScene);
        };

        server.Start();
        server.BeginServerSceneChange("direct-arena");
        server.AfterSceneAssigned(serverScene);
        serverScene.Tick();
        client.Start();
        PumpUntil(
            () => clientScene?.State == SceneState.Running,
            server,
            client,
            () => clientScene?.Tick());

        var blueprint = new EntityBlueprint
        {
            Name = "direct-network-entity",
            Guid = Guid.NewGuid(),
            AssetId = AssetId.New(),
            AssetName = $"test/direct-network-{Guid.NewGuid():N}",
            Components =
            [
                new ComponentBlueprint { Type = typeof(NetworkObject).AssemblyQualifiedName! },
                new ComponentBlueprint { Type = typeof(DirectState).AssemblyQualifiedName! }
            ]
        };
        Assert.True(Resources.TryRegisterAsset(blueprint));
        var serverEntity = server.Spawn(
            blueprint,
            new NetworkSpawnOptions { Owner = client.LocalPeerId });
        serverEntity.GetComponent<DirectState>().Value = 404;
        Assert.True(server.World!.TryGetNetworkId(serverEntity, out var networkId));

        PumpUntil(
            () => client.World!.TryGetEntity(networkId, out _),
            server,
            client);
        server.SendSnapshotNow();
        PumpUntil(
            () => client.World!.TryGetEntity(networkId, out var entity) &&
                  entity!.GetComponent<DirectState>().Value == 404,
            server,
            client);
        client.SendToServer(new DirectInput(17), NetworkDelivery.UnreliableSequenced);
        PumpUntil(() => receivedInput == new DirectInput(17), server, client);

        server.Despawn(serverEntity);
        PumpUntil(
            () => !client.World!.TryGetEntity(networkId, out _),
            server,
            client);
        var disconnectOwned = server.Spawn(
            blueprint,
            new NetworkSpawnOptions
            {
                Owner = client.LocalPeerId,
                DestroyWithOwner = true
            });
        Assert.True(server.World.TryGetNetworkId(disconnectOwned, out var disconnectOwnedId));
        PumpUntil(
            () => client.World!.TryGetEntity(disconnectOwnedId, out _),
            server,
            client);
        client.Dispose();
        PumpServerUntil(() => server.ReadyPeerCount == 0, server);

        Assert.Equal(new DirectInput(17), receivedInput);
        Assert.True(Entity.IsDestroyed(serverEntity));
        Assert.True(Entity.IsDestroyed(disconnectOwned));
        clientScene?.Dispose();
    }

    private static NetworkSession CreateSession(NetworkRole role, INetworkTransport transport) =>
        new(
            role,
            transport,
            new NetworkOptions { GameBuildId = "direct-integration" },
            new NetworkMessageRegistry(),
            new NetworkReplicationRegistry());

    private static (TransportConnectionId Server, TransportConnectionId Client) WaitForConnections(
        INetworkTransport server,
        INetworkTransport client)
    {
        TransportConnectionId serverConnection = default;
        TransportConnectionId clientConnection = default;
        var timeout = Stopwatch.StartNew();
        while (timeout.Elapsed < TimeSpan.FromSeconds(5))
        {
            while (server.TryPollEvent(out var serverEvent))
                if (serverEvent.Kind == TransportEventKind.Connected)
                    serverConnection = serverEvent.Connection;
            while (client.TryPollEvent(out var clientEvent))
                if (clientEvent.Kind == TransportEventKind.Connected)
                    clientConnection = clientEvent.Connection;
            if (serverConnection.IsValid && clientConnection.IsValid)
                return (serverConnection, clientConnection);
            Thread.Sleep(2);
        }
        throw new TimeoutException("Direct IP transports did not report a connection.");
    }

    private static TransportEvent WaitForData(INetworkTransport transport) =>
        WaitForEvent(transport, TransportEventKind.Data);

    private static TransportEvent WaitForEvent(
        INetworkTransport transport,
        TransportEventKind kind)
    {
        var timeout = Stopwatch.StartNew();
        while (timeout.Elapsed < TimeSpan.FromSeconds(5))
        {
            while (transport.TryPollEvent(out var transportEvent))
                if (transportEvent.Kind == kind)
                    return transportEvent;
            Thread.Sleep(2);
        }
        throw new TimeoutException($"Direct IP transport did not report {kind}.");
    }

    private static void PumpUntil(
        Func<bool> condition,
        NetworkSession server,
        NetworkSession client,
        Action? perIteration = null)
    {
        var timeout = Stopwatch.StartNew();
        while (timeout.Elapsed < TimeSpan.FromSeconds(5))
        {
            server.PollTransport();
            client.PollTransport();
            server.ApplyInbound();
            client.ApplyInbound();
            perIteration?.Invoke();
            if (condition())
                return;
            Thread.Sleep(2);
        }
        throw new TimeoutException("Direct IP networking session did not complete in time.");
    }

    private static void PumpServerUntil(Func<bool> condition, NetworkSession server)
    {
        var timeout = Stopwatch.StartNew();
        while (timeout.Elapsed < TimeSpan.FromSeconds(5))
        {
            server.PollTransport();
            server.ApplyInbound();
            if (condition())
                return;
            Thread.Sleep(2);
        }
        throw new TimeoutException("Direct IP server did not observe disconnect in time.");
    }

    private static NetworkMessageRegistry CreateInputMessages(
        Action<NetworkMessageContext, DirectInput> handler)
    {
        var messages = new NetworkMessageRegistry();
        messages.Register(
            501,
            NetworkMessageDirection.ClientToServer,
            4,
            new DirectInputCodec(),
            handler);
        return messages;
    }

    private static int ReservePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private sealed class DirectTestScene : Scene
    {
        internal override void InitializeInternals()
        {
        }
    }

    [NetworkReplicated(401)]
    public sealed class DirectState : Component
    {
        [Replicated(1)] public int Value { get; set; }
    }

    private readonly record struct DirectInput(int Value);

    private sealed class DirectInputCodec : INetworkMessageCodec<DirectInput>
    {
        public void Write(NetworkWriter writer, DirectInput message) =>
            writer.WriteInt32(message.Value);

        public DirectInput Read(ref NetworkReader reader) =>
            new(reader.ReadInt32());
    }
}
