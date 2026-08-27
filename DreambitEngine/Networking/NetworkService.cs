using System;
using Dreambit.ECS;
using Dreambit.Networking.Direct;
using Dreambit.Networking.Messaging;
using Dreambit.Networking.Replication;
using Dreambit.Networking.Scenes;
using Dreambit.Networking.Transport;
using Dreambit.Networking.World;

namespace Dreambit.Networking;

/// <summary>
/// Core-owned networking façade. The service lives for the Core lifetime and owns at most one session.
/// </summary>
public sealed class NetworkService : IDisposable
{
    private readonly Core _core;
    private NetworkSession? _session;
    private bool _disposed;

    internal NetworkService(Core core)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core));
    }

    /// <summary>
    /// Configuration copied into each session when it starts. Options can be prepared before
    /// a session and changed again after <see cref="Stop"/> for the next session.
    /// </summary>
    public NetworkOptions Options { get; } = new();
    public NetworkMessageRegistry Messages { get; } = new();
    public NetworkReplicationRegistry Replication { get; } = new();
    public NetworkSceneCatalog Scenes { get; } = new();

    public NetworkRole Role => _session?.Role ?? NetworkRole.Offline;
    public bool IsServer => Role is NetworkRole.Server or NetworkRole.Host;
    public bool IsClient => Role is NetworkRole.Client or NetworkRole.Host;
    public bool IsHost => Role == NetworkRole.Host;
    public bool IsConnected => _session?.IsConnected == true;
    public NetworkPeerId LocalPeerId => _session?.LocalPeerId ?? NetworkPeerId.None;
    public NetworkSceneEpoch SceneEpoch => _session?.SceneEpoch ?? NetworkSceneEpoch.None;
    public string? CurrentSceneKey => _session?.CurrentSceneKey;
    public ulong ServerTick => _session?.ServerTick ?? 0;
    public Entity? LocalPlayerEntity =>
        _session is { World: { } world, LocalPeerId.IsValid: true } session &&
        world.TryGetPlayerEntity(session.LocalPeerId, out var entity)
            ? entity
            : null;

    public event Action<NetworkPeerId>? PeerConnected;
    public event Action<NetworkPeerId, TransportDisconnectReason, string?>? PeerDisconnected;
    public event Action<TransportDisconnectReason, string?>? ConnectionFailed;

    /// <summary>
    /// Starts an authoritative server. An existing local Scene remains local; use
    /// <see cref="ChangeScene"/> when the server is ready to enter a synchronized Scene.
    /// </summary>
    public void StartServer(INetworkTransport transport) =>
        StartSession(NetworkRole.Server, transport);

    /// <summary>
    /// Starts an authoritative listen server/host. An existing local Scene remains local; use
    /// <see cref="ChangeScene"/> when the host is ready to enter a synchronized Scene.
    /// </summary>
    public void StartHost(INetworkTransport transport) =>
        StartSession(NetworkRole.Host, transport);

    /// <summary>
    /// Starts a client connection. An existing local Scene remains active until the server
    /// requests a catalog-driven synchronized Scene transition.
    /// </summary>
    public void Connect(INetworkTransport transport) =>
        StartSession(NetworkRole.Client, transport);

    public void StartServer(int port, DirectIpOptions? options = null) =>
        StartServer(DirectIpTransport.Listen(port, options));

    public void StartHost(int port, DirectIpOptions? options = null) =>
        StartHost(DirectIpTransport.Listen(port, options));

    public void Connect(string host, int port, DirectIpOptions? options = null) =>
        Connect(DirectIpTransport.Connect(host, port, options));

    public void ChangeScene(string sceneKey)
    {
        if (!IsServer || _session is null)
            throw new InvalidOperationException("Only an active server can change the synchronized Scene.");
        var scene = Scenes.Create(sceneKey);
        try
        {
            _session.BeginServerSceneChange(sceneKey);
        }
        catch
        {
            scene.Terminate();
            throw;
        }
        // SetNextScene takes ownership immediately, including when terminating a displaced
        // pending Scene reports a cleanup failure.
        _core.SetNextSceneFromNetworking(scene, this);
    }

    public Entity Spawn(EntityBlueprint blueprint, NetworkSpawnOptions? options = null) =>
        _session?.Spawn(blueprint, options) ??
        throw new InvalidOperationException("A networking session is not active.");

    public void Despawn(Entity entity)
    {
        if (_session is null)
            throw new InvalidOperationException("A networking session is not active.");
        _session.Despawn(entity);
    }

    public void SendToServer<T>(
        T message,
        NetworkDelivery delivery = NetworkDelivery.ReliableOrdered)
    {
        if (_session is null)
            throw new InvalidOperationException("A networking session is not active.");
        _session.SendToServer(message, delivery);
    }

    public void Send<T>(
        NetworkPeerId peer,
        T message,
        NetworkDelivery delivery = NetworkDelivery.ReliableOrdered)
    {
        if (_session is null)
            throw new InvalidOperationException("A networking session is not active.");
        _session.Send(peer, message, delivery);
    }

    public bool TryGetNetworkId(Entity entity, out NetworkEntityId id)
    {
        if (_session?.World is { } world)
            return world.TryGetNetworkId(entity, out id);
        id = NetworkEntityId.None;
        return false;
    }

    public bool TryGetEntity(NetworkEntityId id, out Entity? entity)
    {
        if (_session?.World is { } world)
            return world.TryGetEntity(id, out entity);
        entity = null;
        return false;
    }

    public bool TryResolve(NetworkEntityRef reference, out Entity? entity)
    {
        if (_session?.World is { } world)
            return world.TryResolve(reference, out entity);
        entity = null;
        return false;
    }

    public bool IsOwnedByLocalPeer(Entity entity) =>
        _session is { World: { } world, LocalPeerId.IsValid: true } session &&
        world.IsOwnedBy(session.LocalPeerId, entity);

    public void SetPlayerEntity(NetworkPeerId peer, Entity entity)
    {
        if (_session is null)
            throw new InvalidOperationException("A networking session is not active.");
        _session.SetPlayerEntity(peer, entity);
    }

    public void SetOwner(Entity entity, NetworkPeerId owner)
    {
        if (_session is null)
            throw new InvalidOperationException("A networking session is not active.");
        _session.SetOwner(entity, owner);
    }

    public void Stop()
    {
        var session = _session;
        _session = null;
        if (session is null)
            return;

        try
        {
            session.Dispose();
        }
        finally
        {
            Messages.Unfreeze();
            Replication.Unfreeze();
            Scenes.Unfreeze();
        }
    }

    internal NetworkSession? ActiveSession => _session;
    internal void PollTransport() => _session?.PollTransport();
    internal void ApplyInbound() => _session?.ApplyInbound();
    internal void BeforeFixedStep(Scene scene) => _session?.BeforeFixedStep(scene);
    internal void AfterFixedStep(Scene scene) => _session?.AfterFixedStep(scene);
    internal void AfterSceneTick(Scene scene) => _session?.AfterSceneTick(scene);
    internal void BeforeSceneUnload(Scene scene) => _session?.BeforeSceneUnload(scene);
    internal void AfterSceneAssigned(Scene scene)
    {
        // A session can begin while a local menu/bootstrap Scene is already active, and a
        // local Scene may already be pending when it begins. Only a catalog-driven network
        // transition is allowed to create a NetworkWorld and consume a network Scene epoch.
        if (_session?.HasPendingSynchronizedScene == true)
            _session.AfterSceneAssigned(scene);
    }

    internal void StopIntake() => _session?.StopIntake();

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Stop();
    }

    private void StartSession(NetworkRole role, INetworkTransport transport)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(transport);
        if (_session is not null)
            throw new InvalidOperationException("A networking session is already active.");

        NetworkSession? session = null;
        var registriesFrozen = false;
        try
        {
            var defaultContentFingerprint = Options.ContentFingerprint is null
                ? Resources.ContentFingerprint
                : null;
            var sessionOptions = Options.Snapshot(defaultContentFingerprint);
            sessionOptions.Validate();
            Messages.Freeze();
            Replication.Freeze();
            Scenes.Freeze();
            registriesFrozen = true;
            session = new NetworkSession(role, transport, sessionOptions, Messages, Replication);
            session.PeerConnected += peer => PeerConnected?.Invoke(peer);
            session.PeerDisconnected += (peer, reason, diagnostic) =>
                PeerDisconnected?.Invoke(peer, reason, diagnostic);
            session.ConnectionFailed += (reason, diagnostic) =>
                ConnectionFailed?.Invoke(reason, diagnostic);
            session.SceneChangeRequested += HandleSceneChangeRequested;
            session.Start();
            _session = session;
        }
        catch
        {
            if (session is not null)
                session.Dispose();
            else
                transport.Dispose();
            if (registriesFrozen)
            {
                Messages.Unfreeze();
                Replication.Unfreeze();
                Scenes.Unfreeze();
            }
            throw;
        }
    }

    private void HandleSceneChangeRequested(string sceneKey, NetworkSceneEpoch sceneEpoch)
    {
        var scene = Scenes.Create(sceneKey);
        _core.SetNextSceneFromNetworking(scene, this);
    }
}
