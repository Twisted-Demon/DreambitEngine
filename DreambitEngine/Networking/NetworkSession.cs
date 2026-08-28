using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dreambit.ECS;
using Dreambit.Networking.Messaging;
using Dreambit.Networking.Protocol;
using Dreambit.Networking.Replication;
using Dreambit.Networking.Session;
using Dreambit.Networking.Transport;
using Dreambit.Networking.World;
using Microsoft.Xna.Framework;

namespace Dreambit.Networking;

/// <summary>Owns one active networking connection/session and survives scene transitions.</summary>
internal sealed class NetworkSession : IDisposable
{
    private readonly Queue<QueuedTransportEvent> _transportEvents = [];
    private readonly Dictionary<TransportConnectionId, NetworkPeer> _peersByConnection = [];
    private readonly Dictionary<NetworkPeerId, NetworkPeer> _peersById = [];
    private readonly NetworkOptions _options;
    private readonly NetworkMessageRegistry _messages;
    private readonly NetworkReplicationRegistry _replication;
    private bool _disposed;
    private bool _acceptTransportEvents = true;
    private uint _nextPeerId = 1;
    private ulong _nextEntityId = 1;
    private TransportConnectionId _serverConnection;
    private string? _pendingSceneKey;
    private NetworkSceneEpoch _pendingSceneEpoch;
    private readonly Dictionary<ReplicationStateKey, uint> _lastStateSequences = [];
    private readonly Dictionary<ClientTransformStateKey, uint> _lastClientTransformSequences = [];
    private readonly Dictionary<NetworkEntityId, PendingLiveSpawn> _pendingLiveSpawns = [];
    private double _snapshotElapsed;
    private uint _stateSequence;
    private ClientBaselineState? _clientBaseline;
    private bool _clientSceneLoadedSent;
    private bool _clientSceneReady;

    public NetworkSession(
        NetworkRole role,
        INetworkTransport transport,
        NetworkOptions options,
        NetworkMessageRegistry messages,
        NetworkReplicationRegistry replication)
    {
        if (role == NetworkRole.Offline)
            throw new ArgumentOutOfRangeException(nameof(role));

        Role = role;
        Transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Snapshot();
        _messages = messages ?? throw new ArgumentNullException(nameof(messages));
        _replication = replication ?? throw new ArgumentNullException(nameof(replication));
        _options.Validate();
        Transport.Capabilities.Validate();
        if (Transport.Capabilities.MaxChannels < 4)
            throw new ArgumentException(
                "Dreambit networking requires at least four transport channels.",
                nameof(transport));
        _replication.ValidateForTransport(Transport.Capabilities, _options.MaxProtocolPayload);
        SessionId = IsServer ? Guid.NewGuid() : Guid.Empty;
        ValidateControlPacketsForTransport();
    }

    public event Action<NetworkPeerId>? PeerConnected;
    public event Action<NetworkPeerId, TransportDisconnectReason, string?>? PeerDisconnected;
    public event Action<TransportDisconnectReason, string?>? ConnectionFailed;
    public event Action<string, NetworkSceneEpoch>? SceneChangeRequested;

    public NetworkRole Role { get; }
    public INetworkTransport Transport { get; }
    public Guid SessionId { get; private set; }
    public NetworkPeerId LocalPeerId { get; private set; }
    public bool IsServer => Role is NetworkRole.Server or NetworkRole.Host;
    public bool IsClient => Role is NetworkRole.Client or NetworkRole.Host;
    public bool IsHost => Role == NetworkRole.Host;
    public bool IsConnected => IsHost || (Role == NetworkRole.Client && LocalPeerId.IsValid);
    public int ReadyPeerCount => _peersById.Values.Count(peer => peer.Phase == NetworkConnectionPhase.Ready);
    public NetworkSceneEpoch SceneEpoch { get; private set; }
    public NetworkStructuralRevision StructuralRevision { get; private set; }
    public ulong ServerTick { get; private set; }
    public string? CurrentSceneKey { get; private set; }
    public NetworkWorld? World { get; private set; }
    internal bool HasPendingSynchronizedScene =>
        _pendingSceneEpoch.IsValid && _pendingSceneKey is not null;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsHost)
        {
            var localPeer = new NetworkPeer
            {
                Connection = TransportConnectionId.None,
                PeerId = AllocatePeerId(),
                Phase = NetworkConnectionPhase.Ready,
                IsLocal = true
            };
            LocalPeerId = localPeer.PeerId;
            _peersById.Add(localPeer.PeerId, localPeer);
        }

        if (IsServer)
            Transport.StartServer();
        else
            Transport.Connect();
    }

    public void PollTransport()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_acceptTransportEvents)
            return;

        while (Transport.TryPollEvent(out var transportEvent))
        {
            if (_transportEvents.Count >= _options.MaxQueuedTransportEvents)
            {
                if (transportEvent.Connection.IsValid)
                    Transport.Disconnect(transportEvent.Connection, TransportDisconnectReason.ProtocolError);
                continue;
            }

            if (transportEvent.Payload.Length > _options.MaxProtocolPayload + NetworkProtocol.HeaderLength)
            {
                if (transportEvent.Connection.IsValid)
                    Transport.Disconnect(transportEvent.Connection, TransportDisconnectReason.ProtocolError);
                continue;
            }

            // Transport payload ownership ends at the next poll. Copy into bounded protocol work.
            _transportEvents.Enqueue(new QueuedTransportEvent(
                transportEvent.Kind,
                transportEvent.Connection,
                transportEvent.Payload.ToArray(),
                transportEvent.Delivery,
                transportEvent.Channel,
                transportEvent.Reason,
                transportEvent.Diagnostic));
        }
    }

    public void ApplyInbound()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        while (_transportEvents.TryDequeue(out var transportEvent))
        {
            try
            {
                switch (transportEvent.Kind)
                {
                    case TransportEventKind.Connected:
                        HandleConnected(transportEvent.Connection);
                        break;
                    case TransportEventKind.Data:
                        HandleData(transportEvent);
                        break;
                    case TransportEventKind.Disconnected:
                    case TransportEventKind.Error:
                        HandleDisconnected(
                            transportEvent.Connection,
                            transportEvent.Reason == TransportDisconnectReason.None
                                ? TransportDisconnectReason.TransportError
                                : transportEvent.Reason,
                            transportEvent.Diagnostic);
                        break;
                    default:
                        throw new NetworkProtocolException(
                            $"Unknown transport event kind {(byte)transportEvent.Kind}.");
                }
            }
            catch (NetworkProtocolException exception)
            {
                RejectProtocolError(transportEvent.Connection, exception.Message);
            }
        }
    }

    public void BeforeFixedStep(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
    }

    public void AfterFixedStep(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (IsServer)
            ServerTick++;

        if (IsServer || Role == NetworkRole.Client)
            _snapshotElapsed += 1d / 60d;
    }

    public void AfterSceneTick(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (ReferenceEquals(World?.Scene, scene))
        {
            World.ReconcileDestroyedEntities();
            if (_snapshotElapsed >= 1d / _options.ReplicationRate)
            {
                _snapshotElapsed %= 1d / _options.ReplicationRate;
                if (IsServer)
                    SendSnapshotNow();
                else if (Role == NetworkRole.Client)
                    SendClientTransformsNow();
            }
        }
    }

    public void BeforeSceneUnload(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        scene.SetStartPreparationGate(null);
        if (ReferenceEquals(World?.Scene, scene))
        {
            World.EntityRemoved -= HandleWorldEntityRemoved;
            World.Dispose(false);
            World = null;
            _lastStateSequences.Clear();
            _lastClientTransformSequences.Clear();
            _pendingLiveSpawns.Clear();
            _clientBaseline = null;
            _clientSceneLoadedSent = false;
            _clientSceneReady = false;
        }
    }

    public void AfterSceneAssigned(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (World is not null)
            throw new InvalidOperationException("The previous NetworkWorld was not released before assigning a Scene.");
        var synchronizedClientAssignment = !IsServer && _pendingSceneEpoch.IsValid;
        SceneEpoch = _pendingSceneEpoch.IsValid
            ? _pendingSceneEpoch
            : NextSceneEpoch(SceneEpoch);
        if (_pendingSceneKey is not null)
            CurrentSceneKey = _pendingSceneKey;
        _pendingSceneEpoch = NetworkSceneEpoch.None;
        _pendingSceneKey = null;
        StructuralRevision = NetworkStructuralRevision.None;
        World = new NetworkWorld(
            scene,
            SceneEpoch,
            IsServer,
            _replication,
            _options.MaxNetworkEntities);
        World.EntityRemoved += HandleWorldEntityRemoved;
        scene.SetStartPreparationGate(
            IsServer || synchronizedClientAssignment
                ? PrepareSceneStart
                : null);
        _clientBaseline = null;
        _clientSceneLoadedSent = false;
        _clientSceneReady = IsServer || !synchronizedClientAssignment;
        if (IsServer && scene.State == SceneState.Running && !World.AuthoredEntitiesBound)
            World.BindServerAuthoredEntities(AllocateEntityId);
    }

    public bool PrepareSceneStart(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (!ReferenceEquals(World?.Scene, scene))
            throw new InvalidOperationException("Scene startup was requested for an unbound NetworkWorld.");
        if (IsServer && !World.AuthoredEntitiesBound)
            World.BindServerAuthoredEntities(AllocateEntityId);
        if (IsServer)
            return true;
        if (_clientSceneReady)
            return true;
        if (!_clientSceneLoadedSent)
        {
            if (!_serverConnection.IsValid)
                return false;
            SendPacket(
                _serverConnection,
                NetworkProtocolMessage.SceneLoaded,
                writer => writer.WriteString(CurrentSceneKey, 256),
                SceneEpoch,
                ServerTick,
                StructuralRevision);
            _clientSceneLoadedSent = true;
        }
        return false;
    }

    public void StopIntake()
    {
        _acceptTransportEvents = false;
    }

    internal Entity Spawn(EntityBlueprint blueprint, NetworkSpawnOptions? options = null)
    {
        if (!IsServer)
            throw new InvalidOperationException("Only the server can spawn network entities.");
        if (World is null)
            throw new InvalidOperationException("A Scene must be assigned before spawning a network entity.");
        ArgumentNullException.ThrowIfNull(blueprint);
        if (blueprint.AssetId.IsEmpty)
            throw new InvalidOperationException(
                "A remotely replicated Blueprint must have a stable AssetId.");
        if (blueprint.AssetName is not null && Encoding.UTF8.GetByteCount(blueprint.AssetName) > 1024)
            throw new InvalidOperationException(
                "A remotely replicated Blueprint fallback name cannot exceed 1024 UTF-8 bytes.");

        options ??= new NetworkSpawnOptions();
        Entity? entity = null;
        var networkId = NetworkEntityId.None;
        var registered = false;
        byte[] spawnPacket;
        List<byte[]> initialStatePackets;
        byte[] readyPacket;
        NetworkStructuralRevision revision;
        try
        {
            entity = World.Scene.CreateNetworkEntity(
                blueprint,
                options.Enabled,
                options.Position,
                options.Rotation,
                options.Scale);
            networkId = AllocateEntityId();
            World.RegisterDynamicEntity(
                entity,
                networkId,
                options.Owner,
                blueprint.AssetId,
                blueprint.AssetName,
                options.DestroyWithOwner);
            registered = true;

            var record = World.Records.First(candidate => candidate.Id == networkId);
            revision = NextStructuralRevision(StructuralRevision);
            spawnPacket = EncodePacket(
                NetworkProtocolMessage.Spawn,
                writer => WriteSpawn(writer, entity, networkId, blueprint, options),
                SceneEpoch,
                ServerTick,
                revision);
            EnsurePacketFitsReliableTransport(spawnPacket, "Spawn");

            initialStatePackets = new List<byte[]>(record.ReplicationBindings.Count);
            var sequence = NextStateSequence();
            foreach (var binding in record.ReplicationBindings)
                initialStatePackets.Add(EncodeStateRecord(networkId, binding, sequence, revision));
            readyPacket = EncodePacket(
                NetworkProtocolMessage.SpawnReady,
                writer => writer.WriteUInt64(networkId.Value),
                SceneEpoch,
                ServerTick,
                revision);
            EnsurePacketFitsReliableTransport(readyPacket, "SpawnReady");
        }
        catch (Exception exception)
        {
            var cleanupErrors = new List<Exception>();
            if (registered && networkId.IsValid)
                try
                {
                    World.Unregister(networkId, false);
                }
                catch (Exception cleanupException)
                {
                    cleanupErrors.Add(cleanupException);
                }
            if (entity is not null)
                try
                {
                    World.Scene.DestroyEntityHierarchyImmediately(entity);
                }
                catch (Exception cleanupException)
                {
                    cleanupErrors.Add(cleanupException);
                }
            if (cleanupErrors.Count != 0)
            {
                cleanupErrors.Insert(0, exception);
                throw new AggregateException(
                    "Network spawn failed and one or more rollback operations also failed.",
                    cleanupErrors);
            }
            throw;
        }

        StructuralRevision = revision;
        foreach (var peer in _peersById.Values)
        {
            if (peer.IsLocal || !ReceivesLiveStructure(peer))
                continue;
            Transport.Send(peer.Connection, spawnPacket, NetworkDelivery.ReliableOrdered, 0);
            foreach (var statePacket in initialStatePackets)
                Transport.Send(peer.Connection, statePacket, NetworkDelivery.ReliableOrdered, 0);
            Transport.Send(peer.Connection, readyPacket, NetworkDelivery.ReliableOrdered, 0);
        }
        return entity;
    }

    internal void BeginServerSceneChange(string sceneKey)
    {
        if (!IsServer)
            throw new InvalidOperationException("Only the server controls synchronized Scene changes.");
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneKey);
        if (Encoding.UTF8.GetByteCount(sceneKey) > 256)
            throw new ArgumentException(
                "A synchronized Scene key cannot exceed 256 UTF-8 bytes.",
                nameof(sceneKey));
        if (_pendingSceneEpoch.IsValid)
            throw new InvalidOperationException("A synchronized Scene change is already pending.");

        _pendingSceneKey = sceneKey;
        _pendingSceneEpoch = NextSceneEpoch(SceneEpoch);
        foreach (var peer in _peersById.Values)
        {
            if (peer.IsLocal || !peer.PeerId.IsValid)
                continue;
            peer.Phase = NetworkConnectionPhase.AwaitingSceneLoad;
            SendPacket(
                peer.Connection,
                NetworkProtocolMessage.SceneChange,
                writer => writer.WriteString(sceneKey, 256),
                _pendingSceneEpoch,
                ServerTick,
                NetworkStructuralRevision.None);
        }
    }

    internal void Despawn(Entity entity)
    {
        if (!IsServer)
            throw new InvalidOperationException("Only the server can despawn network entities.");
        ArgumentNullException.ThrowIfNull(entity);
        if (World is null || !World.TryGetNetworkId(entity, out var id))
            throw new InvalidOperationException("The Entity is not registered in the current NetworkWorld.");
        DespawnAuthoritative(id);
    }

    internal void SetPlayerEntity(NetworkPeerId peerId, Entity entity)
    {
        if (!IsServer || World is null)
            throw new InvalidOperationException("Only an active server can assign player entities.");
        if (!World.TryGetNetworkId(entity, out var entityId))
            throw new InvalidOperationException("The player Entity is not registered in the current NetworkWorld.");
        World.SetPlayerEntity(peerId, entityId);
        var revision = AdvanceStructuralRevision();
        BroadcastStructural(
            NetworkProtocolMessage.PlayerEntity,
            writer =>
            {
                writer.WriteUInt32(peerId.Value);
                writer.WriteUInt64(entityId.Value);
            },
            revision);
    }

    internal void SetOwner(Entity entity, NetworkPeerId owner)
    {
        if (!IsServer || World is null)
            throw new InvalidOperationException("Only an active server can change network ownership.");
        if (!World.TryGetNetworkId(entity, out var entityId))
            throw new InvalidOperationException("The Entity is not registered in the current NetworkWorld.");
        World.SetOwner(entityId, owner);
        var revision = AdvanceStructuralRevision();
        BroadcastStructural(
            NetworkProtocolMessage.Ownership,
            writer =>
            {
                writer.WriteUInt64(entityId.Value);
                writer.WriteUInt32(owner.Value);
            },
            revision);
    }

    internal void SendToServer<T>(
        T message,
        NetworkDelivery delivery = NetworkDelivery.ReliableOrdered)
    {
        if (!IsClient || !LocalPeerId.IsValid)
            throw new InvalidOperationException("An active client or host is required to send to the server.");
        var registration = _messages.GetByType(typeof(T));
        if (registration.Direction is not (
                NetworkMessageDirection.ClientToServer or NetworkMessageDirection.Bidirectional))
            throw new InvalidOperationException(
                $"Networking message '{typeof(T).FullName}' is not registered for client-to-server delivery.");
        var payload = EncodeUserMessage(registration, message!);

        if (IsHost)
        {
            DispatchUserMessage(
                payload,
                inboundFromClient: true,
                LocalPeerId,
                SceneEpoch,
                ServerTick);
            return;
        }

        if (!_serverConnection.IsValid)
            throw new InvalidOperationException("The client has no active server connection.");
        SendPacket(
            _serverConnection,
            NetworkProtocolMessage.UserMessage,
            writer => writer.WriteBytes(payload),
            SceneEpoch,
            ServerTick,
            StructuralRevision,
            delivery,
            delivery == NetworkDelivery.ReliableOrdered ? (byte)1 : (byte)3);
    }

    internal void Send<T>(
        NetworkPeerId peerId,
        T message,
        NetworkDelivery delivery = NetworkDelivery.ReliableOrdered)
    {
        if (!IsServer)
            throw new InvalidOperationException("Only the server can send directly to a peer.");
        var registration = _messages.GetByType(typeof(T));
        if (registration.Direction is not (
                NetworkMessageDirection.ServerToClient or NetworkMessageDirection.Bidirectional))
            throw new InvalidOperationException(
                $"Networking message '{typeof(T).FullName}' is not registered for server-to-client delivery.");
        if (!_peersById.TryGetValue(peerId, out var peer) ||
            peer.Phase != NetworkConnectionPhase.Ready)
            throw new KeyNotFoundException($"Network peer {peerId} is not ready.");
        var payload = EncodeUserMessage(registration, message!);
        if (peer.IsLocal)
        {
            DispatchUserMessage(
                payload,
                inboundFromClient: false,
                NetworkPeerId.None,
                SceneEpoch,
                ServerTick);
            return;
        }
        SendPacket(
            peer.Connection,
            NetworkProtocolMessage.UserMessage,
            writer => writer.WriteBytes(payload),
            SceneEpoch,
            ServerTick,
            StructuralRevision,
            delivery,
            delivery == NetworkDelivery.ReliableOrdered ? (byte)1 : (byte)2);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _acceptTransportEvents = false;
        _transportEvents.Clear();
        _peersByConnection.Clear();
        _peersById.Clear();
        _pendingLiveSpawns.Clear();
        _lastClientTransformSequences.Clear();
        if (World is not null)
        {
            World.Scene.SetStartPreparationGate(null);
            World.EntityRemoved -= HandleWorldEntityRemoved;
        }
        World?.Dispose(true);
        World = null;
        try
        {
            Transport.Stop();
        }
        finally
        {
            Transport.Dispose();
        }
    }

    private void HandleConnected(TransportConnectionId connection)
    {
        if (!connection.IsValid)
            throw new NetworkProtocolException("Transport reported a connection with ID zero.");
        if (_peersByConnection.ContainsKey(connection))
            throw new NetworkProtocolException($"Transport connection {connection} was reported twice.");

        if (IsServer)
        {
            _peersByConnection.Add(connection, new NetworkPeer
            {
                Connection = connection,
                Phase = NetworkConnectionPhase.AwaitingHello
            });
            return;
        }

        if (_serverConnection.IsValid)
            throw new NetworkProtocolException("Client transport reported more than one server connection.");

        _serverConnection = connection;
        _peersByConnection.Add(connection, new NetworkPeer
        {
            Connection = connection,
            Phase = NetworkConnectionPhase.AwaitingWelcome
        });
        SendHello(connection);
    }

    private void HandleData(QueuedTransportEvent transportEvent)
    {
        if (!_peersByConnection.TryGetValue(transportEvent.Connection, out var peer))
            throw new NetworkProtocolException(
                $"Received data for unknown transport connection {transportEvent.Connection}.");
        if (!NetworkProtocol.TryDecode(
                transportEvent.Payload,
                _options.MaxProtocolPayload,
                out var packet,
                out var error))
            throw new NetworkProtocolException(error ?? "Malformed network packet.");

        switch (packet.Header.Message)
        {
            case NetworkProtocolMessage.Hello:
                RequireDelivery(transportEvent, NetworkDelivery.ReliableOrdered, 0);
                HandleHello(peer, packet);
                break;
            case NetworkProtocolMessage.Welcome:
                RequireDelivery(transportEvent, NetworkDelivery.ReliableOrdered, 0);
                HandleWelcome(peer, packet);
                break;
            case NetworkProtocolMessage.Reject:
                RequireDelivery(transportEvent, NetworkDelivery.ReliableOrdered, 0);
                HandleReject(peer, packet);
                break;
            case NetworkProtocolMessage.Spawn:
                RequireDelivery(transportEvent, NetworkDelivery.ReliableOrdered, 0);
                ValidateReadyPacket(peer, packet.Header);
                if (IsStaleScenePacket(packet.Header))
                    return;
                HandleSpawn(packet);
                break;
            case NetworkProtocolMessage.SpawnReady:
                RequireDelivery(transportEvent, NetworkDelivery.ReliableOrdered, 0);
                ValidateReadyPacket(peer, packet.Header);
                if (IsStaleScenePacket(packet.Header))
                    return;
                HandleSpawnReady(packet);
                break;
            case NetworkProtocolMessage.Despawn:
                RequireDelivery(transportEvent, NetworkDelivery.ReliableOrdered, 0);
                ValidateReadyPacket(peer, packet.Header);
                if (IsStaleScenePacket(packet.Header))
                    return;
                HandleDespawn(packet);
                break;
            case NetworkProtocolMessage.SceneChange:
                RequireDelivery(transportEvent, NetworkDelivery.ReliableOrdered, 0);
                ValidateReadyPacket(peer, packet.Header);
                if (IsStaleScenePacket(packet.Header))
                    return;
                HandleSceneChange(peer, packet);
                break;
            case NetworkProtocolMessage.SceneLoaded:
                RequireDelivery(transportEvent, NetworkDelivery.ReliableOrdered, 0);
                ValidateReadyPacket(peer, packet.Header);
                HandleSceneLoaded(peer, packet);
                break;
            case NetworkProtocolMessage.Baseline:
                RequireDelivery(transportEvent, NetworkDelivery.ReliableOrdered, 0);
                ValidateReadyPacket(peer, packet.Header);
                if (IsStaleScenePacket(packet.Header))
                    return;
                HandleBaseline(peer, packet);
                break;
            case NetworkProtocolMessage.Ready:
                RequireDelivery(transportEvent, NetworkDelivery.ReliableOrdered, 0);
                ValidateReadyPacket(peer, packet.Header);
                HandleReady(peer, packet);
                break;
            case NetworkProtocolMessage.PlayerEntity:
                RequireDelivery(transportEvent, NetworkDelivery.ReliableOrdered, 0);
                ValidateReadyPacket(peer, packet.Header);
                if (IsStaleScenePacket(packet.Header))
                    return;
                HandlePlayerEntity(packet);
                break;
            case NetworkProtocolMessage.Ownership:
                RequireDelivery(transportEvent, NetworkDelivery.ReliableOrdered, 0);
                ValidateReadyPacket(peer, packet.Header);
                if (IsStaleScenePacket(packet.Header))
                    return;
                HandleOwnership(packet);
                break;
            case NetworkProtocolMessage.UserMessage:
                ValidateReadyPacket(peer, packet.Header);
                RequireGameplayDelivery(transportEvent, inboundToServer: IsServer);
                if (IsStaleScenePacket(packet.Header))
                    return;
                ValidateCurrentScenePacket(packet.Header, requireNextRevision: false);
                DispatchUserMessage(
                    packet.Payload.Span,
                    inboundFromClient: IsServer,
                    IsServer ? peer.PeerId : NetworkPeerId.None,
                    packet.Header.SceneEpoch,
                    packet.Header.ServerTick);
                break;
            case NetworkProtocolMessage.Snapshot:
                ValidateReadyPacket(peer, packet.Header);
                RequireSnapshotDelivery(transportEvent);
                HandleSnapshot(packet);
                break;
            case NetworkProtocolMessage.ClientTransform:
                ValidateReadyPacket(peer, packet.Header);
                RequireClientTransformDelivery(transportEvent);
                HandleClientTransform(peer, packet);
                break;
            default:
                ValidateReadyPacket(peer, packet.Header);
                throw new NetworkProtocolException(
                    $"Protocol message {packet.Header.Message} is not valid in the current connection state.");
        }

        if (!IsServer &&
            packet.Header.SessionId == SessionId &&
            packet.Header.ServerTick > ServerTick)
            ServerTick = packet.Header.ServerTick;
    }

    private void HandleHello(NetworkPeer peer, NetworkPacket packet)
    {
        if (!IsServer || peer.Phase != NetworkConnectionPhase.AwaitingHello)
            throw new NetworkProtocolException("Unexpected Hello message.");
        if (packet.Header.SessionId != Guid.Empty)
            throw new NetworkProtocolException("Hello message must not claim a session ID.");

        var reader = new NetworkReader(packet.Payload.Span);
        var protocolVersion = reader.ReadUInt16();
        var buildId = reader.ReadString(256) ?? string.Empty;
        var contentFingerprint = reader.ReadString(256);
        var messageSchema = reader.ReadString(64) ?? string.Empty;
        var replicationSchema = reader.ReadString(64) ?? string.Empty;
        reader.EnsureComplete();

        string? mismatch = null;
        if (protocolVersion != NetworkProtocol.Version)
            mismatch = $"Protocol version mismatch: expected {NetworkProtocol.Version}, received {protocolVersion}.";
        else if (!string.Equals(buildId, _options.GameBuildId, StringComparison.Ordinal))
            mismatch = $"Game build mismatch: expected '{_options.GameBuildId}', received '{buildId}'.";
        else if (!string.Equals(contentFingerprint, _options.ContentFingerprint, StringComparison.Ordinal))
            mismatch = "Baked-content fingerprint mismatch.";
        else if (!string.Equals(messageSchema, _messages.SchemaHash.Hex, StringComparison.Ordinal))
            mismatch = "Networking message schema mismatch.";
        else if (!string.Equals(replicationSchema, _replication.SchemaHash.Hex, StringComparison.Ordinal))
            mismatch = "Component replication schema mismatch.";

        if (mismatch is not null)
        {
            SendReject(peer.Connection, mismatch);
            peer.Phase = NetworkConnectionPhase.Rejected;
            Transport.Disconnect(peer.Connection, TransportDisconnectReason.Incompatible);
            return;
        }

        peer.PeerId = AllocatePeerId();
        peer.Phase = NetworkConnectionPhase.Ready;
        _peersById.Add(peer.PeerId, peer);
        SendPacket(
            peer.Connection,
            NetworkProtocolMessage.Welcome,
            writer => writer.WriteUInt32(peer.PeerId.Value));
        var synchronizationKey = _pendingSceneKey ?? CurrentSceneKey;
        var synchronizationEpoch = _pendingSceneEpoch.IsValid
            ? _pendingSceneEpoch
            : SceneEpoch;
        if (synchronizationKey is not null &&
            (_pendingSceneEpoch.IsValid || World is not null))
        {
            peer.Phase = NetworkConnectionPhase.AwaitingSceneLoad;
            SendPacket(
                peer.Connection,
                NetworkProtocolMessage.SceneChange,
                writer => writer.WriteString(synchronizationKey, 256),
                synchronizationEpoch,
                ServerTick,
                _pendingSceneEpoch.IsValid
                    ? NetworkStructuralRevision.None
                    : StructuralRevision);
        }
        PeerConnected?.Invoke(peer.PeerId);
    }

    private void HandleWelcome(NetworkPeer peer, NetworkPacket packet)
    {
        if (Role != NetworkRole.Client || peer.Phase != NetworkConnectionPhase.AwaitingWelcome)
            throw new NetworkProtocolException("Unexpected Welcome message.");
        if (packet.Header.SessionId == Guid.Empty)
            throw new NetworkProtocolException("Welcome message has an empty session ID.");

        var reader = new NetworkReader(packet.Payload.Span);
        var peerId = new NetworkPeerId(reader.ReadUInt32());
        reader.EnsureComplete();
        if (!peerId.IsValid)
            throw new NetworkProtocolException("Server assigned peer ID zero.");

        SessionId = packet.Header.SessionId;
        LocalPeerId = peerId;
        peer.PeerId = peerId;
        peer.Phase = NetworkConnectionPhase.Ready;
        _peersById.Add(peerId, peer);
        PeerConnected?.Invoke(peerId);
    }

    private void HandleReject(NetworkPeer peer, NetworkPacket packet)
    {
        if (IsServer || peer.Phase == NetworkConnectionPhase.Rejected)
            throw new NetworkProtocolException("Unexpected Reject message.");

        var reader = new NetworkReader(packet.Payload.Span);
        var diagnostic = reader.ReadString(1024) ?? "Connection rejected.";
        reader.EnsureComplete();
        peer.RemoteDiagnostic = diagnostic;
        var handshakeFailure = !peer.PeerId.IsValid;
        peer.Phase = NetworkConnectionPhase.Rejected;
        if (handshakeFailure)
            ConnectionFailed?.Invoke(TransportDisconnectReason.Incompatible, diagnostic);
        Transport.Disconnect(
            peer.Connection,
            handshakeFailure
                ? TransportDisconnectReason.Incompatible
                : TransportDisconnectReason.ProtocolError);
    }

    private void HandleSceneChange(NetworkPeer peer, NetworkPacket packet)
    {
        if (IsServer)
            throw new NetworkProtocolException("A server cannot receive SceneChange.");
        var reader = new NetworkReader(packet.Payload.Span);
        var sceneKey = reader.ReadString(256);
        reader.EnsureComplete();
        if (string.IsNullOrWhiteSpace(sceneKey) || !packet.Header.SceneEpoch.IsValid)
            throw new NetworkProtocolException("SceneChange contains an empty Scene key or epoch.");
        if (packet.Header.SceneEpoch.Value < SceneEpoch.Value)
            return;
        if (packet.Header.SceneEpoch == SceneEpoch &&
            string.Equals(CurrentSceneKey, sceneKey, StringComparison.Ordinal) &&
            World is not null)
            return;
        if (_pendingSceneEpoch.IsValid)
            throw new NetworkProtocolException(
                $"Received SceneChange for epoch {packet.Header.SceneEpoch} while epoch " +
                $"{_pendingSceneEpoch} is still loading.");

        _pendingSceneKey = sceneKey;
        _pendingSceneEpoch = packet.Header.SceneEpoch;
        _clientSceneReady = false;
        _clientSceneLoadedSent = false;
        _clientBaseline = null;
        peer.Phase = NetworkConnectionPhase.AwaitingSceneLoad;
        try
        {
            if (SceneChangeRequested is null)
                throw new InvalidOperationException(
                    "No Scene catalog handler is attached to the client NetworkSession.");
            SceneChangeRequested.Invoke(sceneKey, packet.Header.SceneEpoch);
        }
        catch (Exception exception) when (exception is not NetworkProtocolException)
        {
            _pendingSceneKey = null;
            _pendingSceneEpoch = NetworkSceneEpoch.None;
            throw new NetworkProtocolException(
                $"Could not construct synchronized Scene '{sceneKey}': {exception.Message}");
        }
    }

    private void HandleSceneLoaded(NetworkPeer peer, NetworkPacket packet)
    {
        if (!IsServer || peer.Phase != NetworkConnectionPhase.AwaitingSceneLoad)
            throw new NetworkProtocolException("Unexpected SceneLoaded message.");
        if (packet.Header.SceneEpoch != SceneEpoch)
            throw new NetworkProtocolException(
                $"Peer {peer.PeerId} loaded Scene epoch {packet.Header.SceneEpoch}; server is at {SceneEpoch}.");
        var reader = new NetworkReader(packet.Payload.Span);
        var sceneKey = reader.ReadString(256);
        reader.EnsureComplete();
        if (!string.Equals(sceneKey, CurrentSceneKey, StringComparison.Ordinal))
            throw new NetworkProtocolException(
                $"Peer {peer.PeerId} loaded Scene '{sceneKey}', expected '{CurrentSceneKey}'.");
        if (World is null)
            throw new NetworkProtocolException("The server has no NetworkWorld for the loaded Scene.");
        if (!World.AuthoredEntitiesBound)
            World.BindServerAuthoredEntities(AllocateEntityId);

        peer.Phase = NetworkConnectionPhase.Synchronizing;
        try
        {
            SendBaseline(peer);
        }
        catch (NetworkSynchronizationException exception)
        {
            peer.RemoteDiagnostic = exception.Message;
            peer.Phase = NetworkConnectionPhase.Rejected;
            SendReject(peer.Connection, exception.Message);
            Transport.Disconnect(peer.Connection, TransportDisconnectReason.Incompatible);
        }
    }

    private void SendBaseline(NetworkPeer peer)
    {
        if (World is null)
            throw new InvalidOperationException("Cannot capture a baseline without a NetworkWorld.");
        World.ReconcileDestroyedEntities();
        var revision = StructuralRevision;
        var tick = ServerTick;
        var authored = World.AuthoredBindings.ToArray();
        var dynamic = World.Records
            .Where(record => record.Origin == NetworkSpawnOrigin.DynamicBlueprint)
            .ToArray();
        var players = World.PlayerEntities.ToArray();
        var componentCount = World.Records.Sum(record => record.ReplicationBindings.Count);
        if (authored.Length + dynamic.Length > _options.MaxNetworkEntities ||
            players.Length > _options.MaxNetworkEntities ||
            componentCount > _options.MaxBaselineComponentRecords)
            throw new NetworkSynchronizationException(
                $"Peer {peer.PeerId} cannot be synchronized because the current NetworkWorld baseline " +
                $"contains {authored.Length + dynamic.Length} Entities, {players.Length} player mappings, " +
                $"and {componentCount} Component states; configured limits are " +
                $"{_options.MaxNetworkEntities}, {_options.MaxNetworkEntities}, and " +
                $"{_options.MaxBaselineComponentRecords} respectively.");

        var packets = new List<byte[]>(
            checked(2 + authored.Length + dynamic.Length + players.Length + componentCount));
        AddBaselinePacket(
            NetworkBaselineRecordKind.Begin,
            writer =>
            {
                writer.WriteInt32(authored.Length);
                writer.WriteInt32(dynamic.Length);
                writer.WriteInt32(players.Length);
                writer.WriteInt32(componentCount);
            },
            "baseline Begin");

        foreach (var binding in authored)
            AddBaselinePacket(
                NetworkBaselineRecordKind.AuthoredEntity,
                writer =>
                {
                    writer.WriteGuid(binding.SourceGuid);
                    writer.WriteUInt64(binding.NetworkEntityId.Value);
                    writer.WriteUInt32(binding.Owner.Value);
                },
                $"authored Entity {binding.NetworkEntityId}");

        foreach (var record in dynamic)
            AddBaselinePacket(
                NetworkBaselineRecordKind.DynamicEntity,
                writer => WriteDynamicBaselineRecord(writer, record),
                $"dynamic Entity {record.Id}");

        foreach (var player in players)
            AddBaselinePacket(
                NetworkBaselineRecordKind.PlayerEntity,
                writer =>
                {
                    writer.WriteUInt32(player.Key.Value);
                    writer.WriteUInt64(player.Value.Value);
                },
                $"player mapping {player.Key}");

        foreach (var record in World.Records)
            foreach (var binding in record.ReplicationBindings)
            {
                var payload = binding.Capture();
                AddBaselinePacket(
                    NetworkBaselineRecordKind.ComponentState,
                    writer =>
                    {
                        writer.WriteUInt64(record.Id.Value);
                        writer.WriteUInt16(binding.Descriptor.Id);
                        writer.WriteLengthPrefixedBytes(payload, binding.Descriptor.MaximumPayload);
                    },
                    $"Component {binding.Descriptor.Id} state on Entity {record.Id}");
            }

        AddBaselinePacket(NetworkBaselineRecordKind.End, null, "baseline End");
        foreach (var packet in packets)
            Transport.Send(peer.Connection, packet, NetworkDelivery.ReliableOrdered, 0);

        void AddBaselinePacket(
            NetworkBaselineRecordKind kind,
            Action<NetworkWriter>? writePayload,
            string label)
        {
            var packet = EncodePacket(
                NetworkProtocolMessage.Baseline,
                writer =>
                {
                    writer.WriteByte((byte)kind);
                    writePayload?.Invoke(writer);
                },
                SceneEpoch,
                tick,
                revision);
            if (packet.Length > Transport.Capabilities.MaxReliablePayload)
            {
                throw new NetworkSynchronizationException(
                    $"Peer {peer.PeerId} cannot be synchronized because {label} requires a " +
                    $"{packet.Length}-byte reliable packet, exceeding the active transport limit " +
                    $"of {Transport.Capabilities.MaxReliablePayload} bytes.");
            }
            packets.Add(packet);
        }
    }

    private static void WriteDynamicBaselineRecord(
        NetworkWriter writer,
        NetworkEntityRecord record)
    {
        writer.WriteUInt64(record.Id.Value);
        writer.WriteGuid(record.BlueprintAssetId.Value);
        writer.WriteString(record.BlueprintAssetName, 1024);
        writer.WriteUInt32(record.Owner.Value);
        writer.WriteBoolean(record.DestroyWithOwner);
        writer.WriteBoolean(record.Entity.LocallyEnabled);
        WriteVector3(writer, record.Entity.Transform.Position);
        var rotation = record.Entity.Transform.Rotation;
        writer.WriteSingle(rotation.X);
        writer.WriteSingle(rotation.Y);
        writer.WriteSingle(rotation.Z);
        writer.WriteSingle(rotation.W);
        WriteVector3(writer, record.Entity.Transform.Scale);
    }

    private void HandleBaseline(NetworkPeer peer, NetworkPacket packet)
    {
        if (IsServer)
            throw new NetworkProtocolException("A server cannot receive a world baseline.");
        if (packet.Header.SceneEpoch != SceneEpoch || World is null)
            throw new NetworkProtocolException(
                $"Baseline epoch {packet.Header.SceneEpoch} does not match the active client Scene {SceneEpoch}.");
        peer.Phase = NetworkConnectionPhase.Synchronizing;
        var reader = new NetworkReader(packet.Payload.Span);
        var kindValue = reader.ReadByte();
        if (!Enum.IsDefined(typeof(NetworkBaselineRecordKind), kindValue))
            throw new NetworkProtocolException($"Unknown baseline record kind {kindValue}.");
        var kind = (NetworkBaselineRecordKind)kindValue;

        if (kind == NetworkBaselineRecordKind.Begin)
        {
            if (_clientBaseline is not null)
                throw new NetworkProtocolException("Received a second baseline Begin record.");
            var authored = ReadBoundedCount(ref reader, _options.MaxNetworkEntities, "authored entities");
            var dynamic = ReadBoundedCount(ref reader, _options.MaxNetworkEntities, "dynamic entities");
            if (authored > _options.MaxNetworkEntities - dynamic)
                throw new NetworkProtocolException("Baseline entity count exceeds the configured limit.");
            var players = ReadBoundedCount(ref reader, _options.MaxNetworkEntities, "player mappings");
            var components = ReadBoundedCount(
                ref reader,
                _options.MaxBaselineComponentRecords,
                "Component states");
            reader.EnsureComplete();
            _clientBaseline = new ClientBaselineState
            {
                SceneEpoch = packet.Header.SceneEpoch,
                StructuralRevision = packet.Header.StructuralRevision,
                ServerTick = packet.Header.ServerTick,
                ExpectedAuthored = authored,
                ExpectedDynamic = dynamic,
                ExpectedPlayers = players,
                ExpectedComponents = components
            };
            return;
        }

        var baseline = _clientBaseline ??
                       throw new NetworkProtocolException("Baseline record arrived before Begin.");
        if (baseline.SceneEpoch != packet.Header.SceneEpoch ||
            baseline.StructuralRevision != packet.Header.StructuralRevision)
            throw new NetworkProtocolException("Baseline record header changed during synchronization.");

        switch (kind)
        {
            case NetworkBaselineRecordKind.AuthoredEntity:
                EnsureRecordCapacity(baseline.Authored.Count, baseline.ExpectedAuthored, "authored Entity");
                baseline.Authored.Add(new NetworkAuthoredBinding(
                    reader.ReadGuid(),
                    new NetworkEntityId(reader.ReadUInt64()),
                    new NetworkPeerId(reader.ReadUInt32())));
                break;
            case NetworkBaselineRecordKind.DynamicEntity:
                EnsureRecordCapacity(baseline.Dynamic.Count, baseline.ExpectedDynamic, "dynamic Entity");
                baseline.Dynamic.Add(ReadDynamicBaselineRecord(ref reader));
                break;
            case NetworkBaselineRecordKind.PlayerEntity:
                EnsureRecordCapacity(baseline.Players.Count, baseline.ExpectedPlayers, "player mapping");
                baseline.Players.Add(new KeyValuePair<NetworkPeerId, NetworkEntityId>(
                    new NetworkPeerId(reader.ReadUInt32()),
                    new NetworkEntityId(reader.ReadUInt64())));
                break;
            case NetworkBaselineRecordKind.ComponentState:
                EnsureRecordCapacity(
                    baseline.Components.Count,
                    baseline.ExpectedComponents,
                    "Component state");
                var entityId = new NetworkEntityId(reader.ReadUInt64());
                var componentId = reader.ReadUInt16();
                var descriptor = _replication.GetById(componentId);
                baseline.Components.Add(new NetworkComponentStateRecord(
                    entityId,
                    componentId,
                    reader.ReadLengthPrefixedBytes(descriptor.MaximumPayload).ToArray()));
                break;
            case NetworkBaselineRecordKind.End:
                reader.EnsureComplete();
                ApplyClientBaseline(peer, baseline);
                return;
            default:
                throw new NetworkProtocolException($"Unexpected baseline record kind {kind}.");
        }
        reader.EnsureComplete();
    }

    private void ApplyClientBaseline(NetworkPeer peer, ClientBaselineState baseline)
    {
        if (World is null)
            throw new NetworkProtocolException("The client NetworkWorld disappeared during baseline application.");
        ValidateBaselineCount(baseline.Authored.Count, baseline.ExpectedAuthored, "authored entities");
        ValidateBaselineCount(baseline.Dynamic.Count, baseline.ExpectedDynamic, "dynamic entities");
        ValidateBaselineCount(baseline.Players.Count, baseline.ExpectedPlayers, "player mappings");
        ValidateBaselineCount(baseline.Components.Count, baseline.ExpectedComponents, "Component states");

        try
        {
            World.BindClientAuthoredEntities(baseline.Authored);
            foreach (var spawn in baseline.Dynamic)
                MaterializeDynamicSpawn(spawn);
            foreach (var player in baseline.Players)
                World.SetPlayerEntity(player.Key, player.Value);
            foreach (var state in baseline.Components)
            {
                if (!World.TryGetReplicationBinding(
                        state.EntityId,
                        state.ComponentId,
                        out var binding) || binding is null)
                    throw new InvalidOperationException(
                        $"Baseline references missing Component {state.ComponentId} on Entity {state.EntityId}.");
                binding.Apply(state.Payload);
            }
        }
        catch (Exception exception) when (exception is not NetworkProtocolException)
        {
            throw new NetworkProtocolException($"Could not apply initial world baseline: {exception.Message}");
        }

        StructuralRevision = baseline.StructuralRevision;
        ServerTick = baseline.ServerTick;
        _clientBaseline = null;
        _clientSceneReady = true;
        peer.Phase = NetworkConnectionPhase.Ready;
        SendPacket(
            peer.Connection,
            NetworkProtocolMessage.Ready,
            null,
            SceneEpoch,
            ServerTick,
            StructuralRevision);
    }

    private void MaterializeDynamicSpawn(NetworkDynamicSpawnRecord spawn)
    {
        if (World is null || !spawn.EntityId.IsValid || spawn.BlueprintAssetId.IsEmpty)
            throw new InvalidOperationException("Dynamic baseline spawn contains an invalid identity.");
        var blueprint = Resources.LoadDreambitAsset(
                            spawn.BlueprintAssetId,
                            spawn.BlueprintAssetName,
                            typeof(EntityBlueprint)) as EntityBlueprint
                        ?? throw new InvalidOperationException(
                            $"Blueprint '{spawn.BlueprintAssetName}' ({spawn.BlueprintAssetId}) could not be loaded.");
        Entity? entity = null;
        var registered = false;
        try
        {
            entity = World.Scene.CreateNetworkEntity(
                blueprint,
                spawn.Enabled,
                spawn.Position,
                null,
                spawn.Scale);
            entity.Transform.Rotation = spawn.Rotation;
            World.RegisterDynamicEntity(
                entity,
                spawn.EntityId,
                spawn.Owner,
                spawn.BlueprintAssetId,
                spawn.BlueprintAssetName,
                spawn.DestroyWithOwner);
            registered = true;
        }
        catch (Exception exception)
        {
            var cleanupErrors = new List<Exception>();
            if (registered)
                try
                {
                    World.Unregister(spawn.EntityId, false);
                }
                catch (Exception cleanupException)
                {
                    cleanupErrors.Add(cleanupException);
                }
            if (entity is not null)
                try
                {
                    World.Scene.DestroyEntityHierarchyImmediately(entity);
                }
                catch (Exception cleanupException)
                {
                    cleanupErrors.Add(cleanupException);
                }
            if (cleanupErrors.Count != 0)
            {
                cleanupErrors.Insert(0, exception);
                throw new AggregateException(
                    "Dynamic baseline materialization failed and rollback also reported errors.",
                    cleanupErrors);
            }
            throw;
        }
    }

    private static NetworkDynamicSpawnRecord ReadDynamicBaselineRecord(ref NetworkReader reader)
    {
        var entityId = new NetworkEntityId(reader.ReadUInt64());
        var assetId = new AssetId(reader.ReadGuid());
        var assetName = reader.ReadString(1024);
        var owner = new NetworkPeerId(reader.ReadUInt32());
        var destroyWithOwner = reader.ReadBoolean();
        var enabled = reader.ReadBoolean();
        var position = ReadVector3(ref reader);
        var rotation = new Quaternion(
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle());
        var scale = ReadVector3(ref reader);
        return new NetworkDynamicSpawnRecord(
            entityId,
            assetId,
            assetName,
            owner,
            destroyWithOwner,
            enabled,
            position,
            rotation,
            scale);
    }

    private void HandleReady(NetworkPeer peer, NetworkPacket packet)
    {
        if (!IsServer || peer.Phase != NetworkConnectionPhase.Synchronizing)
            throw new NetworkProtocolException("Unexpected Ready message.");
        if (packet.Header.SceneEpoch != SceneEpoch)
            throw new NetworkProtocolException(
                $"Peer {peer.PeerId} became ready for stale Scene epoch {packet.Header.SceneEpoch}.");
        var reader = new NetworkReader(packet.Payload.Span);
        reader.EnsureComplete();
        peer.Phase = NetworkConnectionPhase.Ready;
    }

    private static int ReadBoundedCount(ref NetworkReader reader, int maximum, string label)
    {
        var count = reader.ReadInt32();
        if (count < 0 || count > maximum)
            throw new NetworkProtocolException(
                $"Baseline {label} count {count} is outside 0..{maximum}.");
        return count;
    }

    private static void EnsureRecordCapacity(int actual, int expected, string label)
    {
        if (actual >= expected)
            throw new NetworkProtocolException(
                $"Baseline contains more {label} records than declared ({expected}).");
    }

    private static void ValidateBaselineCount(int actual, int expected, string label)
    {
        if (actual != expected)
            throw new NetworkProtocolException(
                $"Baseline ended with {actual} {label}; expected {expected}.");
    }

    private void ValidateReadyPacket(NetworkPeer peer, NetworkPacketHeader header)
    {
        if (!peer.PeerId.IsValid || peer.Phase is
            NetworkConnectionPhase.AwaitingHello or
            NetworkConnectionPhase.AwaitingWelcome or
            NetworkConnectionPhase.Rejected)
            throw new NetworkProtocolException("Received a session packet before handshake completion.");
        if (header.SessionId != SessionId)
            throw new NetworkProtocolException(
                $"Session identity mismatch for peer {peer.PeerId}: expected {SessionId}, received {header.SessionId}.");
    }

    private void HandleDisconnected(
        TransportConnectionId connection,
        TransportDisconnectReason reason,
        string? diagnostic)
    {
        if (!_peersByConnection.Remove(connection, out var peer))
            return;

        diagnostic = peer.RemoteDiagnostic ?? diagnostic;

        if (peer.PeerId.IsValid)
        {
            // Remove the dead peer before authoritative cleanup broadcasts structural changes.
            // Its transport connection has already been closed and must not be a send target.
            _peersById.Remove(peer.PeerId);
            if (IsServer && World is not null)
            {
                foreach (var entityId in World.GetOwnedEntities(peer.PeerId))
                    if (World.GetDestroyWithOwner(entityId))
                        DespawnAuthoritative(entityId);
                    else
                        SetOwnerAuthoritative(entityId, NetworkPeerId.None);
                ClearPlayerEntityAuthoritative(peer.PeerId);
            }
            PeerDisconnected?.Invoke(peer.PeerId, reason, diagnostic);
        }
        else if (!IsServer && peer.Phase != NetworkConnectionPhase.Rejected)
        {
            ConnectionFailed?.Invoke(reason, diagnostic);
        }

        if (!IsServer && connection == _serverConnection)
        {
            _serverConnection = TransportConnectionId.None;
            LocalPeerId = NetworkPeerId.None;
            SessionId = Guid.Empty;
            if (World is not null)
            {
                var disconnectedScene = World.Scene;
                World.EntityRemoved -= HandleWorldEntityRemoved;
                World.Dispose(true);
                disconnectedScene.SetStartPreparationGate(_ => false);
                World = null;
            }
            _clientBaseline = null;
            _clientSceneReady = false;
            _lastStateSequences.Clear();
            _lastClientTransformSequences.Clear();
            _pendingLiveSpawns.Clear();
        }
    }

    private void SendHello(TransportConnectionId connection)
    {
        var packet = NetworkProtocol.Encode(
            new NetworkPacketHeader(
                NetworkProtocolMessage.Hello,
                Guid.Empty,
                NetworkSceneEpoch.None,
                0,
                NetworkStructuralRevision.None),
            writer =>
            {
                writer.WriteUInt16(NetworkProtocol.Version);
                writer.WriteString(_options.GameBuildId, 256);
                writer.WriteString(_options.ContentFingerprint, 256);
                writer.WriteString(_messages.SchemaHash.Hex, 64);
                writer.WriteString(_replication.SchemaHash.Hex, 64);
            },
            _options.MaxProtocolPayload);
        Transport.Send(connection, packet, NetworkDelivery.ReliableOrdered, 0);
    }

    private void SendReject(TransportConnectionId connection, string diagnostic)
    {
        var maximumDiagnosticBytes = Math.Min(
            1024,
            Math.Min(
                _options.MaxProtocolPayload - sizeof(int),
                Transport.Capabilities.MaxReliablePayload - NetworkProtocol.HeaderLength - sizeof(int)));
        if (maximumDiagnosticBytes < 0)
            throw new InvalidOperationException(
                "The active transport cannot fit the minimum networking Reject packet.");
        var packet = NetworkProtocol.Encode(
            new NetworkPacketHeader(
                NetworkProtocolMessage.Reject,
                SessionId,
                NetworkSceneEpoch.None,
                0,
                NetworkStructuralRevision.None),
            writer => writer.WriteString(
                TruncateUtf8(diagnostic, maximumDiagnosticBytes),
                maximumDiagnosticBytes),
            _options.MaxProtocolPayload);
        Transport.Send(connection, packet, NetworkDelivery.ReliableOrdered, 0);
    }

    private void ValidateControlPacketsForTransport()
    {
        if (Transport.Capabilities.MaxReliablePayload < NetworkProtocol.HeaderLength + sizeof(int))
            throw new InvalidOperationException(
                $"The active transport's reliable payload limit of " +
                $"{Transport.Capabilities.MaxReliablePayload} bytes cannot fit the minimum Dreambit " +
                $"network packet size of {NetworkProtocol.HeaderLength + sizeof(int)} bytes.");
        if (Transport.Capabilities.MaxUnreliablePayload < NetworkProtocol.HeaderLength)
            throw new InvalidOperationException(
                $"The active transport's unreliable payload limit of " +
                $"{Transport.Capabilities.MaxUnreliablePayload} bytes cannot fit the " +
                $"{NetworkProtocol.HeaderLength}-byte Dreambit network header.");

        var hello = NetworkProtocol.Encode(
            new NetworkPacketHeader(
                NetworkProtocolMessage.Hello,
                Guid.Empty,
                NetworkSceneEpoch.None,
                0,
                NetworkStructuralRevision.None),
            writer =>
            {
                writer.WriteUInt16(NetworkProtocol.Version);
                writer.WriteString(_options.GameBuildId, 256);
                writer.WriteString(_options.ContentFingerprint, 256);
                writer.WriteString(_messages.SchemaHash.Hex, 64);
                writer.WriteString(_replication.SchemaHash.Hex, 64);
            },
            _options.MaxProtocolPayload);
        if (hello.Length > Transport.Capabilities.MaxReliablePayload)
            throw new InvalidOperationException(
                $"The configured handshake requires a {hello.Length}-byte reliable packet, " +
                $"exceeding the active transport limit of " +
                $"{Transport.Capabilities.MaxReliablePayload} bytes.");
    }

    private void SendPacket(
        TransportConnectionId connection,
        NetworkProtocolMessage message,
        Action<NetworkWriter>? payload,
        NetworkSceneEpoch sceneEpoch = default,
        ulong serverTick = 0,
        NetworkStructuralRevision structuralRevision = default,
        NetworkDelivery delivery = NetworkDelivery.ReliableOrdered,
        byte channel = 0)
    {
        var packet = EncodePacket(
            message,
            payload,
            sceneEpoch,
            serverTick,
            structuralRevision);
        Transport.Send(connection, packet, delivery, channel);
    }

    private byte[] EncodePacket(
        NetworkProtocolMessage message,
        Action<NetworkWriter>? payload,
        NetworkSceneEpoch sceneEpoch = default,
        ulong serverTick = 0,
        NetworkStructuralRevision structuralRevision = default) =>
        NetworkProtocol.Encode(
            new NetworkPacketHeader(
                message,
                SessionId,
                sceneEpoch,
                serverTick,
                structuralRevision),
            payload,
            _options.MaxProtocolPayload);

    private static void WriteSpawn(
        NetworkWriter writer,
        Entity entity,
        NetworkEntityId id,
        EntityBlueprint blueprint,
        NetworkSpawnOptions options)
    {
        writer.WriteUInt64(id.Value);
        writer.WriteGuid(blueprint.AssetId.Value);
        writer.WriteString(blueprint.AssetName, 1024);
        writer.WriteUInt32(options.Owner.Value);
        writer.WriteBoolean(options.DestroyWithOwner);
        writer.WriteBoolean(entity.LocallyEnabled);
        byte flags = 0;
        if (options.Position.HasValue) flags |= 2;
        if (options.Rotation.HasValue) flags |= 4;
        if (options.Scale.HasValue) flags |= 8;
        writer.WriteByte(flags);
        if (options.Position is { } position) WriteVector3(writer, position);
        if (options.Rotation is { } rotation) WriteVector3(writer, rotation);
        if (options.Scale is { } scale) WriteVector3(writer, scale);
    }

    private void HandleSpawn(NetworkPacket packet)
    {
        if (IsServer)
            throw new NetworkProtocolException("A server cannot receive an authoritative Spawn message.");
        ValidateCurrentScenePacket(packet.Header, requireNextRevision: true);
        if (World is null)
            throw new NetworkProtocolException("Received a Spawn without an active NetworkWorld.");

        var reader = new NetworkReader(packet.Payload.Span);
        var id = new NetworkEntityId(reader.ReadUInt64());
        var assetId = new AssetId(reader.ReadGuid());
        var fallbackName = reader.ReadString(1024);
        var owner = new NetworkPeerId(reader.ReadUInt32());
        var destroyWithOwner = reader.ReadBoolean();
        var intendedEnabled = reader.ReadBoolean();
        var flags = reader.ReadByte();
        if ((flags & ~14) != 0)
            throw new NetworkProtocolException($"Spawn options contain unknown flags {flags}.");
        Vector3? position = (flags & 2) != 0 ? ReadVector3(ref reader) : null;
        Vector3? rotation = (flags & 4) != 0 ? ReadVector3(ref reader) : null;
        Vector3? scale = (flags & 8) != 0 ? ReadVector3(ref reader) : null;
        reader.EnsureComplete();
        if (!id.IsValid || assetId.IsEmpty)
            throw new NetworkProtocolException("Spawn contains an empty network or Blueprint identity.");

        Entity? entity = null;
        var registered = false;
        try
        {
            var blueprint = Resources.LoadDreambitAsset(assetId, fallbackName, typeof(EntityBlueprint))
                            as EntityBlueprint
                            ?? throw new InvalidOperationException(
                                $"Blueprint '{fallbackName}' ({assetId}) could not be loaded.");
            entity = World.Scene.CreateNetworkEntity(
                blueprint,
                intendedEnabled,
                position,
                rotation,
                scale);
            SetHierarchyUpdatesSuspended(entity, true);
            World.RegisterDynamicEntity(
                entity,
                id,
                owner,
                assetId,
                fallbackName,
                destroyWithOwner);
            registered = true;
            var record = World.Records.First(candidate => candidate.Id == id);
            _pendingLiveSpawns.Add(
                id,
                new PendingLiveSpawn(
                    entity,
                    intendedEnabled,
                    record.ReplicationBindings));
            StructuralRevision = packet.Header.StructuralRevision;
        }
        catch (Exception exception) when (exception is not NetworkProtocolException)
        {
            var cleanupErrors = new List<Exception>();
            _pendingLiveSpawns.Remove(id);
            if (registered)
                try
                {
                    World.Unregister(id, false);
                }
                catch (Exception cleanupException)
                {
                    cleanupErrors.Add(cleanupException);
                }
            if (entity is not null)
                try
                {
                    World.Scene.DestroyEntityHierarchyImmediately(entity);
                }
                catch (Exception cleanupException)
                {
                    cleanupErrors.Add(cleanupException);
                }
            Exception failure = exception;
            if (cleanupErrors.Count != 0)
            {
                cleanupErrors.Insert(0, exception);
                failure = new AggregateException(
                    "Remote network spawn failed and rollback also reported errors.",
                    cleanupErrors);
            }
            throw new NetworkProtocolException(
                $"Could not materialize network Blueprint '{fallbackName}' ({assetId}): {failure.Message}",
                failure);
        }
    }

    private void HandleSpawnReady(NetworkPacket packet)
    {
        if (IsServer)
            throw new NetworkProtocolException("A server cannot receive SpawnReady.");
        ValidateCurrentScenePacket(packet.Header, requireNextRevision: false);
        if (packet.Header.StructuralRevision != StructuralRevision)
            throw new NetworkProtocolException(
                $"SpawnReady revision {packet.Header.StructuralRevision} does not match " +
                $"the active structural revision {StructuralRevision}.");
        var reader = new NetworkReader(packet.Payload.Span);
        var id = new NetworkEntityId(reader.ReadUInt64());
        reader.EnsureComplete();
        if (!_pendingLiveSpawns.Remove(id, out var pending))
            throw new NetworkProtocolException($"SpawnReady references non-pending network Entity {id}.");
        if (pending.RemainingComponentIds.Count != 0)
            throw new NetworkProtocolException(
                $"SpawnReady for network Entity {id} arrived before initial state for Component(s) " +
                $"{string.Join(", ", pending.RemainingComponentIds)}.");

        pending.Entity.Enabled = pending.IntendedEnabled;
        SetHierarchyUpdatesSuspended(pending.Entity, false);
    }

    private void HandleDespawn(NetworkPacket packet)
    {
        if (IsServer)
            throw new NetworkProtocolException("A server cannot receive an authoritative Despawn message.");
        ValidateCurrentScenePacket(packet.Header, requireNextRevision: true);
        var reader = new NetworkReader(packet.Payload.Span);
        var id = new NetworkEntityId(reader.ReadUInt64());
        reader.EnsureComplete();
        if (World is null || !World.TryGetEntity(id, out _))
            throw new NetworkProtocolException(
                $"Despawn references unknown network Entity {id} in Scene epoch {SceneEpoch}.");
        World.DespawnLocal(id, false);
        StructuralRevision = packet.Header.StructuralRevision;
    }

    private void DespawnAuthoritative(NetworkEntityId id)
    {
        if (World is null)
            throw new InvalidOperationException("There is no active NetworkWorld.");
        var revision = AdvanceStructuralRevision();
        foreach (var peer in _peersById.Values)
        {
            if (peer.IsLocal || !ReceivesLiveStructure(peer))
                continue;
            SendPacket(
                peer.Connection,
                NetworkProtocolMessage.Despawn,
                writer => writer.WriteUInt64(id.Value),
                SceneEpoch,
                ServerTick,
                revision);
        }
        World.DespawnLocal(id, false);
    }

    private void SetOwnerAuthoritative(NetworkEntityId id, NetworkPeerId owner)
    {
        if (World is null)
            return;
        World.SetOwner(id, owner);
        var revision = AdvanceStructuralRevision();
        BroadcastStructural(
            NetworkProtocolMessage.Ownership,
            writer =>
            {
                writer.WriteUInt64(id.Value);
                writer.WriteUInt32(owner.Value);
            },
            revision);
    }

    private void ClearPlayerEntityAuthoritative(NetworkPeerId peerId)
    {
        if (World is null || !World.RemovePlayerEntity(peerId))
            return;
        var revision = AdvanceStructuralRevision();
        BroadcastStructural(
            NetworkProtocolMessage.PlayerEntity,
            writer =>
            {
                writer.WriteUInt32(peerId.Value);
                writer.WriteUInt64(NetworkEntityId.None.Value);
            },
            revision);
    }

    private void HandlePlayerEntity(NetworkPacket packet)
    {
        if (IsServer)
            throw new NetworkProtocolException("A server cannot receive PlayerEntity assignments.");
        ValidateCurrentScenePacket(packet.Header, requireNextRevision: true);
        var reader = new NetworkReader(packet.Payload.Span);
        var peerId = new NetworkPeerId(reader.ReadUInt32());
        var entityId = new NetworkEntityId(reader.ReadUInt64());
        reader.EnsureComplete();
        if (World is null)
            throw new NetworkProtocolException("PlayerEntity arrived without a NetworkWorld.");
        if (!peerId.IsValid)
            throw new NetworkProtocolException("PlayerEntity contains peer ID zero.");
        if (entityId.IsValid)
        {
            if (!World.TryGetEntity(entityId, out _))
                throw new NetworkProtocolException(
                    $"PlayerEntity references missing network Entity {entityId}.");
            World.SetPlayerEntity(peerId, entityId);
        }
        else
            World.RemovePlayerEntity(peerId);
        StructuralRevision = packet.Header.StructuralRevision;
    }

    private void HandleOwnership(NetworkPacket packet)
    {
        if (IsServer)
            throw new NetworkProtocolException("A server cannot receive ownership assignments.");
        ValidateCurrentScenePacket(packet.Header, requireNextRevision: true);
        var reader = new NetworkReader(packet.Payload.Span);
        var entityId = new NetworkEntityId(reader.ReadUInt64());
        var owner = new NetworkPeerId(reader.ReadUInt32());
        reader.EnsureComplete();
        if (World is null || !World.TryGetEntity(entityId, out _))
            throw new NetworkProtocolException(
                $"Ownership assignment references missing network Entity {entityId}.");
        World.SetOwner(entityId, owner);
        StructuralRevision = packet.Header.StructuralRevision;
    }

    private void BroadcastStructural(
        NetworkProtocolMessage message,
        Action<NetworkWriter> payload,
        NetworkStructuralRevision revision)
    {
        foreach (var peer in _peersById.Values)
        {
            if (peer.IsLocal || !ReceivesLiveStructure(peer))
                continue;
            SendPacket(
                peer.Connection,
                message,
                payload,
                SceneEpoch,
                ServerTick,
                revision);
        }
    }

    private static bool ReceivesLiveStructure(NetworkPeer peer) =>
        peer.Phase is NetworkConnectionPhase.Ready or NetworkConnectionPhase.Synchronizing;

    private void HandleWorldEntityRemoved(NetworkEntityId id)
    {
        if (!IsServer || _disposed)
            return;
        var revision = AdvanceStructuralRevision();
        foreach (var peer in _peersById.Values)
        {
            if (peer.IsLocal || !ReceivesLiveStructure(peer))
                continue;
            SendPacket(
                peer.Connection,
                NetworkProtocolMessage.Despawn,
                writer => writer.WriteUInt64(id.Value),
                SceneEpoch,
                ServerTick,
                revision);
        }
    }

    private void ValidateCurrentScenePacket(
        NetworkPacketHeader header,
        bool requireNextRevision)
    {
        if (header.SceneEpoch != SceneEpoch)
            throw new NetworkProtocolException(
                $"Scene epoch mismatch: expected {SceneEpoch}, received {header.SceneEpoch}.");
        if (requireNextRevision &&
            header.StructuralRevision.Value != StructuralRevision.Value + 1)
            throw new NetworkProtocolException(
                $"Structural revision mismatch in Scene epoch {SceneEpoch}: expected " +
                $"{StructuralRevision.Value + 1}, received {header.StructuralRevision}.");
    }

    internal void SendSnapshotNow()
    {
        if (!IsServer)
            throw new InvalidOperationException("Only the server can publish replicated state.");
        if (World is null)
            return;
        var sequence = NextStateSequence();
        foreach (var peer in _peersById.Values)
        {
            if (peer.IsLocal || peer.Phase != NetworkConnectionPhase.Ready)
                continue;
            foreach (var record in World.Records)
                foreach (var binding in record.ReplicationBindings)
                    SendStateRecord(
                        peer.Connection,
                        record.Id,
                        binding,
                        sequence,
                        NetworkDelivery.UnreliableSequenced,
                        2);
        }
    }

    internal void SendClientTransformsNow()
    {
        if (Role != NetworkRole.Client)
            throw new InvalidOperationException(
                "Only a remote client can publish client-authoritative transforms.");
        if (World is null || !LocalPeerId.IsValid || !_serverConnection.IsValid)
            return;

        var sequence = NextStateSequence();
        foreach (var record in World.Records)
        {
            if (record.Owner != LocalPeerId)
                continue;

            var transform = record.Entity.GetComponent<NetworkTransform2D>();
            if (transform is null || !transform.AllowsClientAuthority)
                continue;

            transform.CaptureAuthoritativeTransform();
            var position = transform.AuthoritativePosition;
            var rotation = transform.AuthoritativeRotation;
            var scale = transform.AuthoritativeScale;
            if (!IsFinite(position) || !float.IsFinite(rotation) || !IsFinite(scale))
                continue;

            SendPacket(
                _serverConnection,
                NetworkProtocolMessage.ClientTransform,
                writer =>
                {
                    writer.WriteUInt32(sequence);
                    writer.WriteUInt64(record.Id.Value);
                    WriteVector2(writer, position);
                    writer.WriteSingle(rotation);
                    WriteVector2(writer, scale);
                },
                SceneEpoch,
                ServerTick,
                StructuralRevision,
                NetworkDelivery.UnreliableSequenced,
                3);
        }
    }

    private byte[] EncodeStateRecord(
        NetworkEntityId entityId,
        NetworkReplicationBinding binding,
        uint sequence,
        NetworkStructuralRevision revision)
    {
        var componentPayload = binding.Capture();
        var packet = EncodePacket(
            NetworkProtocolMessage.Snapshot,
            writer =>
            {
                writer.WriteUInt32(sequence);
                writer.WriteUInt64(entityId.Value);
                writer.WriteUInt16(binding.Descriptor.Id);
                writer.WriteLengthPrefixedBytes(
                    componentPayload,
                    binding.Descriptor.MaximumPayload);
            },
            SceneEpoch,
            ServerTick,
            revision);
        EnsurePacketFitsReliableTransport(
            packet,
            $"initial Component {binding.Descriptor.Id} state for Entity {entityId}");
        return packet;
    }

    private void SendStateRecord(
        TransportConnectionId connection,
        NetworkEntityId entityId,
        NetworkReplicationBinding binding,
        uint sequence,
        NetworkDelivery delivery,
        byte channel)
    {
        var componentPayload = binding.Capture();
        var maximumPacket = delivery == NetworkDelivery.ReliableOrdered
            ? Transport.Capabilities.MaxReliablePayload
            : Transport.Capabilities.MaxUnreliablePayload;
        const int recordOverhead = sizeof(uint) + sizeof(ulong) + sizeof(ushort) + sizeof(int);
        if (NetworkProtocol.HeaderLength + recordOverhead + componentPayload.Length > maximumPacket)
            throw new InvalidOperationException(
                $"Replicated Component {binding.Descriptor.Id} on Entity {entityId} requires " +
                $"{componentPayload.Length} state bytes, exceeding the transport packet limit " +
                $"{maximumPacket}. Use a smaller/custom representation.");

        SendPacket(
            connection,
            NetworkProtocolMessage.Snapshot,
            writer =>
            {
                writer.WriteUInt32(sequence);
                writer.WriteUInt64(entityId.Value);
                writer.WriteUInt16(binding.Descriptor.Id);
                writer.WriteLengthPrefixedBytes(
                    componentPayload,
                    binding.Descriptor.MaximumPayload);
            },
            SceneEpoch,
            ServerTick,
            StructuralRevision,
            delivery,
            channel);
    }

    private void HandleSnapshot(NetworkPacket packet)
    {
        if (IsServer)
            throw new NetworkProtocolException("The authoritative server cannot receive Component snapshots.");
        if (IsStaleScenePacket(packet.Header))
            return;
        ValidateCurrentScenePacket(packet.Header, requireNextRevision: false);
        // A future topology cannot be applied safely. Full snapshots make dropping this state healable.
        if (packet.Header.StructuralRevision.Value > StructuralRevision.Value)
            return;
        if (World is null)
            return;

        var reader = new NetworkReader(packet.Payload.Span);
        var sequence = reader.ReadUInt32();
        var entityId = new NetworkEntityId(reader.ReadUInt64());
        var componentId = reader.ReadUInt16();
        var descriptor = _replication.GetById(componentId);
        var componentPayload = reader.ReadLengthPrefixedBytes(descriptor.MaximumPayload);
        reader.EnsureComplete();
        if (!entityId.IsValid)
            throw new NetworkProtocolException("A Component snapshot contains network Entity ID zero.");
        if (!World.TryGetReplicationBinding(entityId, componentId, out var binding) || binding is null)
            throw new NetworkProtocolException(
                $"Component snapshot references missing Component {componentId} on network Entity {entityId}.");

        var key = new ReplicationStateKey(entityId, componentId);
        if (_lastStateSequences.TryGetValue(key, out var previous) &&
            !IsNewerSequence(sequence, previous))
            return;
        try
        {
            binding.Apply(componentPayload);
            _lastStateSequences[key] = sequence;
            if (_pendingLiveSpawns.TryGetValue(entityId, out var pending))
                pending.RemainingComponentIds.Remove(componentId);
        }
        catch (Exception exception) when (exception is not NetworkProtocolException)
        {
            throw new NetworkProtocolException(
                $"Could not apply Component {componentId} on network Entity {entityId}: {exception.Message}");
        }
    }

    private void HandleClientTransform(NetworkPeer peer, NetworkPacket packet)
    {
        if (!IsServer)
            throw new NetworkProtocolException(
                "A client cannot receive a client-authoritative transform update.");
        if (IsStaleScenePacket(packet.Header))
            return;
        ValidateCurrentScenePacket(packet.Header, requireNextRevision: false);
        if (packet.Header.StructuralRevision != StructuralRevision || World is null)
            return;

        var reader = new NetworkReader(packet.Payload.Span);
        var sequence = reader.ReadUInt32();
        var entityId = new NetworkEntityId(reader.ReadUInt64());
        var position = ReadVector2(ref reader);
        var rotation = reader.ReadSingle();
        var scale = ReadVector2(ref reader);
        reader.EnsureComplete();

        if (!entityId.IsValid)
            throw new NetworkProtocolException(
                "A client transform update contains network Entity ID zero.");
        if (!IsFinite(position) || !float.IsFinite(rotation) || !IsFinite(scale))
            throw new NetworkProtocolException(
                $"Client transform update for Entity {entityId} contains a non-finite value.");

        // Ownership and authority may legitimately change while an unreliable packet is in flight.
        // Drop stale/unauthorized poses instead of disconnecting an otherwise valid peer.
        if (!World.TryGetEntity(entityId, out var entity) || entity is null ||
            World.GetOwner(entityId) != peer.PeerId)
        {
            return;
        }

        var transform = entity.GetComponent<NetworkTransform2D>();
        if (transform is null || !transform.AllowsClientAuthority)
            return;

        var key = new ClientTransformStateKey(peer.PeerId, entityId);
        if (_lastClientTransformSequences.TryGetValue(key, out var previous) &&
            !IsNewerSequence(sequence, previous))
        {
            return;
        }

        // A dedicated server needs the submitted pose immediately for simulation. A listen host
        // keeps the pose as a presentation target for remote Client-authority entities, avoiding
        // visible network-rate stepping in the host's local view.
        var interpolateOnHost =
            IsHost && transform.Authority == TransformAuthority.Client;
        transform.AcceptClientTransform(
            position,
            rotation,
            scale,
            applyImmediately: !interpolateOnHost);
        _lastClientTransformSequences[key] = sequence;
    }

    private byte[] EncodeUserMessage(INetworkMessageRegistration registration, object message)
    {
        using var body = new NetworkWriter(
            Math.Min(256, registration.MaximumPayload),
            registration.MaximumPayload);
        registration.Write(body, message);
        if (body.Length > registration.MaximumPayload)
            throw new NetworkProtocolException(
                $"Networking message {registration.Id} exceeds its registered payload limit.");

        using var payload = new NetworkWriter(
            Math.Min(256, 6 + body.Length),
            checked(6 + registration.MaximumPayload));
        payload.WriteUInt16(registration.Id);
        payload.WriteLengthPrefixedBytes(body.WrittenSpan, registration.MaximumPayload);
        return payload.ToArray();
    }

    private void DispatchUserMessage(
        ReadOnlySpan<byte> payload,
        bool inboundFromClient,
        NetworkPeerId sender,
        NetworkSceneEpoch sceneEpoch,
        ulong serverTick)
    {
        var reader = new NetworkReader(payload);
        var id = reader.ReadUInt16();
        var registration = _messages.GetById(id);
        var allowed = inboundFromClient
            ? registration.Direction is NetworkMessageDirection.ClientToServer or
                NetworkMessageDirection.Bidirectional
            : registration.Direction is NetworkMessageDirection.ServerToClient or
                NetworkMessageDirection.Bidirectional;
        if (!allowed)
            throw new NetworkProtocolException(
                $"Networking message {id} is not allowed in this direction.");
        var messageBytes = reader.ReadLengthPrefixedBytes(registration.MaximumPayload);
        reader.EnsureComplete();
        var messageReader = new NetworkReader(messageBytes);
        registration.ReadAndHandle(
            ref messageReader,
            new NetworkMessageContext(sender, sceneEpoch, serverTick));
    }

    private bool IsStaleScenePacket(NetworkPacketHeader header) =>
        header.SceneEpoch.IsValid && header.SceneEpoch.Value < SceneEpoch.Value;

    private static void RequireDelivery(
        QueuedTransportEvent transportEvent,
        NetworkDelivery delivery,
        byte channel)
    {
        if (transportEvent.Delivery != delivery || transportEvent.Channel != channel)
            throw new NetworkProtocolException(
                $"Protocol message used {transportEvent.Delivery} channel {transportEvent.Channel}; " +
                $"expected {delivery} channel {channel}.");
    }

    private static void RequireGameplayDelivery(
        QueuedTransportEvent transportEvent,
        bool inboundToServer)
    {
        var valid = transportEvent.Delivery switch
        {
            NetworkDelivery.ReliableOrdered => transportEvent.Channel == 1,
            NetworkDelivery.UnreliableSequenced => transportEvent.Channel ==
                (inboundToServer ? (byte)3 : (byte)2),
            _ => false
        };
        if (!valid)
            throw new NetworkProtocolException(
                $"Gameplay message used invalid {transportEvent.Delivery} channel " +
                $"{transportEvent.Channel}.");
    }

    private static void RequireSnapshotDelivery(QueuedTransportEvent transportEvent)
    {
        var valid =
            (transportEvent.Delivery == NetworkDelivery.UnreliableSequenced &&
             transportEvent.Channel == 2) ||
            (transportEvent.Delivery == NetworkDelivery.ReliableOrdered &&
             transportEvent.Channel == 0);
        if (!valid)
            throw new NetworkProtocolException(
                $"Component state used invalid {transportEvent.Delivery} channel " +
                $"{transportEvent.Channel}.");
    }

    private static void RequireClientTransformDelivery(QueuedTransportEvent transportEvent)
    {
        if (transportEvent.Delivery != NetworkDelivery.UnreliableSequenced ||
            transportEvent.Channel != 3)
        {
            throw new NetworkProtocolException(
                $"Client transform used invalid {transportEvent.Delivery} channel " +
                $"{transportEvent.Channel}.");
        }
    }

    private static void WriteVector2(NetworkWriter writer, Vector2 value)
    {
        writer.WriteSingle(value.X);
        writer.WriteSingle(value.Y);
    }

    private static Vector2 ReadVector2(ref NetworkReader reader) =>
        new(reader.ReadSingle(), reader.ReadSingle());

    private static bool IsFinite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);

    private static void WriteVector3(NetworkWriter writer, Vector3 value)
    {
        writer.WriteSingle(value.X);
        writer.WriteSingle(value.Y);
        writer.WriteSingle(value.Z);
    }

    private static Vector3 ReadVector3(ref NetworkReader reader) =>
        new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

    private void EnsurePacketFitsReliableTransport(byte[] packet, string label)
    {
        if (packet.Length > Transport.Capabilities.MaxReliablePayload)
            throw new InvalidOperationException(
                $"{label} requires a {packet.Length}-byte reliable packet, exceeding the active " +
                $"transport limit of {Transport.Capabilities.MaxReliablePayload} bytes.");
    }

    private static void SetHierarchyUpdatesSuspended(Entity root, bool suspended)
    {
        root.UpdatesSuspended = suspended;
        foreach (var child in root.Children)
            SetHierarchyUpdatesSuspended(child, suspended);
    }

    private static string TruncateUtf8(string value, int maximumBytes)
    {
        if (Encoding.UTF8.GetByteCount(value) <= maximumBytes)
            return value;
        Span<byte> buffer = stackalloc byte[maximumBytes];
        Encoding.UTF8.GetEncoder().Convert(
            value.AsSpan(),
            buffer,
            true,
            out var charsUsed,
            out _,
            out _);
        return value[..charsUsed];
    }

    private void RejectProtocolError(TransportConnectionId connection, string diagnostic)
    {
        if (!connection.IsValid)
            return;

        try
        {
            if (_peersByConnection.ContainsKey(connection))
                SendReject(connection, diagnostic);
        }
        catch (Exception)
        {
            // The peer is being disconnected anyway. Preserve the original protocol diagnostic.
        }
        Transport.Disconnect(connection, TransportDisconnectReason.ProtocolError);
    }

    private NetworkPeerId AllocatePeerId()
    {
        if (_nextPeerId == 0)
            throw new InvalidOperationException("Network peer ID space was exhausted.");
        return new NetworkPeerId(_nextPeerId++);
    }

    private uint NextStateSequence()
    {
        _stateSequence++;
        if (_stateSequence == 0)
            _stateSequence++;
        return _stateSequence;
    }

    private static bool IsNewerSequence(uint candidate, uint previous) =>
        candidate != previous && unchecked(candidate - previous) < 0x80000000U;

    internal NetworkEntityId AllocateEntityId()
    {
        if (!IsServer)
            throw new InvalidOperationException("Only the server can allocate NetworkEntityId values.");
        if (_nextEntityId == 0)
            throw new InvalidOperationException("Network entity ID space was exhausted.");
        return new NetworkEntityId(_nextEntityId++);
    }

    internal NetworkStructuralRevision AdvanceStructuralRevision()
    {
        StructuralRevision = NextStructuralRevision(StructuralRevision);
        return StructuralRevision;
    }

    private static NetworkSceneEpoch NextSceneEpoch(NetworkSceneEpoch current)
    {
        var next = current.Value + 1;
        if (next == 0)
            throw new InvalidOperationException("Network Scene epoch space was exhausted.");
        return new NetworkSceneEpoch(next);
    }

    private static NetworkStructuralRevision NextStructuralRevision(NetworkStructuralRevision current)
    {
        var next = current.Value + 1;
        if (next == 0)
            throw new InvalidOperationException("Network structural revision space was exhausted.");
        return new NetworkStructuralRevision(next);
    }

    private readonly record struct QueuedTransportEvent(
        TransportEventKind Kind,
        TransportConnectionId Connection,
        byte[] Payload,
        NetworkDelivery Delivery,
        byte Channel,
        TransportDisconnectReason Reason,
        string? Diagnostic);

    private readonly record struct ReplicationStateKey(
        NetworkEntityId EntityId,
        ushort ComponentId);

    private readonly record struct ClientTransformStateKey(
        NetworkPeerId PeerId,
        NetworkEntityId EntityId);

    private sealed class PendingLiveSpawn
    {
        public PendingLiveSpawn(
            Entity entity,
            bool intendedEnabled,
            IReadOnlyList<NetworkReplicationBinding> bindings)
        {
            Entity = entity;
            IntendedEnabled = intendedEnabled;
            RemainingComponentIds = new HashSet<ushort>();
            foreach (var binding in bindings)
                RemainingComponentIds.Add(binding.Descriptor.Id);
        }

        public Entity Entity { get; }
        public bool IntendedEnabled { get; }
        public HashSet<ushort> RemainingComponentIds { get; }
    }

    private sealed class NetworkSynchronizationException(string message) : Exception(message);
}
