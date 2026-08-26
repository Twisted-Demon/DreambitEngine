using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dreambit.ECS;
using Dreambit.Networking.Replication;

namespace Dreambit.Networking.World;

/// <summary>Runtime-only network identity and ownership for exactly one Scene.</summary>
internal sealed class NetworkWorld : IDisposable
{
    private readonly Dictionary<NetworkEntityId, NetworkEntityRecord> _byNetworkId = [];
    private readonly Dictionary<Entity, NetworkEntityId> _byEntity =
        new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<NetworkPeerId, NetworkEntityId> _playerEntities = [];
    private readonly List<NetworkEntityRecord> _records = [];
    private readonly List<NetworkAuthoredBinding> _authoredBindings = [];
    private readonly NetworkReplicationRegistry _replication;
    private readonly int _maxNetworkEntities;
    private bool _disposed;

    public NetworkWorld(
        Scene scene,
        NetworkSceneEpoch sceneEpoch,
        bool server,
        NetworkReplicationRegistry? replication = null,
        int maxNetworkEntities = 100_000)
    {
        Scene = scene ?? throw new ArgumentNullException(nameof(scene));
        if (!sceneEpoch.IsValid)
            throw new ArgumentOutOfRangeException(nameof(sceneEpoch));
        SceneEpoch = sceneEpoch;
        IsServer = server;
        _replication = replication ?? new NetworkReplicationRegistry();
        if (maxNetworkEntities < 1)
            throw new ArgumentOutOfRangeException(nameof(maxNetworkEntities));
        _maxNetworkEntities = maxNetworkEntities;
    }

    public event Action<NetworkEntityId>? EntityRemoved;

    public Scene Scene { get; }
    public NetworkSceneEpoch SceneEpoch { get; }
    public bool IsServer { get; }
    public bool AuthoredEntitiesBound { get; private set; }
    public IReadOnlyList<NetworkEntityRecord> Records => _records;
    public IReadOnlyList<NetworkAuthoredBinding> AuthoredBindings => _authoredBindings;
    public IReadOnlyDictionary<NetworkPeerId, NetworkEntityId> PlayerEntities => _playerEntities;

    public IReadOnlyList<NetworkAuthoredBinding> BindServerAuthoredEntities(
        Func<NetworkEntityId> allocateNetworkId)
    {
        ThrowIfDisposed();
        if (!IsServer)
            throw new InvalidOperationException("Only a server world can assign authored network IDs.");
        if (AuthoredEntitiesBound)
            throw new InvalidOperationException("Authored network entities are already bound.");
        ArgumentNullException.ThrowIfNull(allocateNetworkId);

        var sceneEntities = Scene.GetAllEntities();
        var replicatedEntities = new List<(Entity Entity, NetworkObject Marker)>();
        foreach (var entity in sceneEntities)
        {
            var marker = entity.GetComponent<NetworkObject>();
            if (marker is null)
                continue;
            if (marker.Presence == NetworkPresence.Replicated)
            {
                _replication.ValidateEntityShape(entity);
                replicatedEntities.Add((entity, marker));
            }
            else if (!Enum.IsDefined(marker.Presence))
                throw new InvalidOperationException(
                    $"Entity '{entity.Name}' has unsupported network presence {marker.Presence}.");
        }
        EnsureRegistrationCapacity(replicatedEntities.Count);

        var bindings = new List<NetworkAuthoredBinding>(replicatedEntities.Count);
        foreach (var entity in sceneEntities)
        {
            var marker = entity.GetComponent<NetworkObject>();
            if (marker is null)
                continue;
            switch (marker.Presence)
            {
                case NetworkPresence.ClientOnly:
                    DisableAndDestroy(entity);
                    continue;
                case NetworkPresence.ServerOnly:
                    continue;
                case NetworkPresence.Replicated:
                    var id = allocateNetworkId();
                    Register(
                        entity,
                        marker,
                        id,
                        NetworkPeerId.None,
                        NetworkSpawnOrigin.AuthoredScene,
                        entity.Id,
                        AssetId.Empty,
                        null,
                        true);
                    bindings.Add(new NetworkAuthoredBinding(entity.Id, id, NetworkPeerId.None));
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Entity '{entity.Name}' has unsupported network presence {marker.Presence}.");
            }
        }

        AuthoredEntitiesBound = true;
        _authoredBindings.AddRange(bindings);
        return bindings;
    }

    public void BindClientAuthoredEntities(IReadOnlyList<NetworkAuthoredBinding> bindings)
    {
        ThrowIfDisposed();
        if (IsServer)
            throw new InvalidOperationException("A server world cannot consume authored binding data.");
        if (AuthoredEntitiesBound)
            throw new InvalidOperationException("Authored network entities are already bound.");
        ArgumentNullException.ThrowIfNull(bindings);

        var bySource = new Dictionary<Guid, NetworkAuthoredBinding>();
        foreach (var binding in bindings)
        {
            if (binding.SourceGuid == Guid.Empty || !binding.NetworkEntityId.IsValid)
                throw new InvalidOperationException("Authored network binding contains an empty identity.");
            if (!bySource.TryAdd(binding.SourceGuid, binding))
                throw new InvalidOperationException(
                    $"Authored source GUID '{binding.SourceGuid}' appears more than once in the binding table.");
        }

        EnsureRegistrationCapacity(bindings.Count);
        foreach (var entity in Scene.GetAllEntities())
        {
            var marker = entity.GetComponent<NetworkObject>();
            if (marker?.Presence == NetworkPresence.Replicated)
                _replication.ValidateEntityShape(entity);
            else if (marker is not null && !Enum.IsDefined(marker.Presence))
                throw new InvalidOperationException(
                    $"Entity '{entity.Name}' has unsupported network presence {marker.Presence}.");
        }

        var consumed = new HashSet<Guid>();
        foreach (var entity in Scene.GetAllEntities())
        {
            var marker = entity.GetComponent<NetworkObject>();
            if (marker is null)
                continue;
            switch (marker.Presence)
            {
                case NetworkPresence.ServerOnly:
                    DisableAndDestroy(entity);
                    continue;
                case NetworkPresence.ClientOnly:
                    continue;
                case NetworkPresence.Replicated:
                    if (!bySource.TryGetValue(entity.Id, out var binding))
                        throw new InvalidOperationException(
                            $"Client scene entity '{entity.Name}' ({entity.Id}) has no authored network binding.");
                    consumed.Add(entity.Id);
                    Register(
                        entity,
                        marker,
                        binding.NetworkEntityId,
                        binding.Owner,
                        NetworkSpawnOrigin.AuthoredScene,
                        entity.Id,
                        AssetId.Empty,
                        null,
                        true);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Entity '{entity.Name}' has unsupported network presence {marker.Presence}.");
            }
        }

        var missing = bySource.Keys.FirstOrDefault(source => !consumed.Contains(source));
        if (missing != Guid.Empty)
            throw new InvalidOperationException(
                $"Authored network binding '{missing}' does not resolve to a replicated NetworkObject in the client scene.");
        AuthoredEntitiesBound = true;
        _authoredBindings.AddRange(bindings);
    }

    public void RegisterDynamicEntity(
        Entity entity,
        NetworkEntityId id,
        NetworkPeerId owner,
        AssetId blueprintAssetId,
        string? blueprintAssetName,
        bool destroyWithOwner = true)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(entity);
        if (blueprintAssetId.IsEmpty)
            throw new ArgumentException("A dynamic network entity requires a stable Blueprint AssetId.", nameof(blueprintAssetId));
        if (blueprintAssetName is not null && Encoding.UTF8.GetByteCount(blueprintAssetName) > 1024)
            throw new ArgumentException(
                "A dynamic network Blueprint fallback name cannot exceed 1024 UTF-8 bytes.",
                nameof(blueprintAssetName));
        var marker = entity.GetComponent<NetworkObject>() ??
                     throw new InvalidOperationException(
                         $"Dynamic network entity '{entity.Name}' has no NetworkObject component.");
        if (marker.Presence != NetworkPresence.Replicated)
            throw new InvalidOperationException("Dynamic network entities must use Replicated presence.");
        Register(
            entity,
            marker,
            id,
            owner,
            NetworkSpawnOrigin.DynamicBlueprint,
            Guid.Empty,
            blueprintAssetId,
            blueprintAssetName,
            destroyWithOwner);
    }

    public bool TryGetNetworkId(Entity entity, out NetworkEntityId id)
    {
        ThrowIfDisposed();
        if (entity is null)
        {
            id = NetworkEntityId.None;
            return false;
        }
        return _byEntity.TryGetValue(entity, out id);
    }

    public bool TryGetEntity(NetworkEntityId id, out Entity? entity)
    {
        ThrowIfDisposed();
        if (_byNetworkId.TryGetValue(id, out var record) &&
            ReferenceEquals(record.Entity.OwningScene, Scene) &&
            !Entity.IsDestroyed(record.Entity))
        {
            entity = record.Entity;
            return true;
        }
        entity = null;
        return false;
    }

    public bool TryResolve(NetworkEntityRef reference, out Entity? entity)
    {
        ThrowIfDisposed();
        if (reference.SceneEpoch == SceneEpoch)
            return TryGetEntity(reference.EntityId, out entity);
        entity = null;
        return false;
    }

    public NetworkPeerId GetOwner(NetworkEntityId id)
    {
        ThrowIfDisposed();
        return GetRecord(id).Owner;
    }

    public bool GetDestroyWithOwner(NetworkEntityId id)
    {
        ThrowIfDisposed();
        return GetRecord(id).DestroyWithOwner;
    }

    public bool TryGetReplicationBinding(
        NetworkEntityId entityId,
        ushort componentId,
        out NetworkReplicationBinding? binding)
    {
        ThrowIfDisposed();
        if (_byNetworkId.TryGetValue(entityId, out var record))
            foreach (var candidate in record.ReplicationBindings)
                if (candidate.Descriptor.Id == componentId)
                {
                    binding = candidate;
                    return true;
                }
        binding = null;
        return false;
    }

    public void SetOwner(NetworkEntityId id, NetworkPeerId owner)
    {
        ThrowIfDisposed();
        GetRecord(id).Owner = owner;
    }

    public bool IsOwnedBy(NetworkPeerId peer, Entity entity) =>
        peer.IsValid && TryGetNetworkId(entity, out var id) && GetOwner(id) == peer;

    public void SetPlayerEntity(NetworkPeerId peer, NetworkEntityId entityId)
    {
        ThrowIfDisposed();
        if (!peer.IsValid)
            throw new ArgumentOutOfRangeException(nameof(peer));
        GetRecord(entityId);
        _playerEntities[peer] = entityId;
    }

    public bool TryGetPlayerEntity(NetworkPeerId peer, out Entity? entity)
    {
        ThrowIfDisposed();
        if (_playerEntities.TryGetValue(peer, out var id))
            return TryGetEntity(id, out entity);
        entity = null;
        return false;
    }

    public bool RemovePlayerEntity(NetworkPeerId peer)
    {
        ThrowIfDisposed();
        return _playerEntities.Remove(peer);
    }

    public bool Unregister(NetworkEntityId id, bool notify = true)
    {
        ThrowIfDisposed();
        if (!_byNetworkId.Remove(id, out var record))
            return false;
        _byEntity.Remove(record.Entity);
        _records.Remove(record);
        record.Marker.UnbindDestroyed();
        foreach (var peer in _playerEntities.Where(pair => pair.Value == id).Select(pair => pair.Key).ToArray())
            _playerEntities.Remove(peer);
        if (notify)
            EntityRemoved?.Invoke(id);
        return true;
    }

    public void DespawnLocal(NetworkEntityId id, bool notify = true)
    {
        ThrowIfDisposed();
        var record = GetRecord(id);
        Unregister(id, notify);
        DisableAndDestroy(record.Entity);
    }

    public void ReconcileDestroyedEntities()
    {
        ThrowIfDisposed();
        for (var index = _records.Count - 1; index >= 0; index--)
        {
            var record = _records[index];
            if (Entity.IsDestroyed(record.Entity))
                Unregister(record.Id);
        }
    }

    public IReadOnlyList<NetworkEntityId> GetOwnedEntities(NetworkPeerId peer)
    {
        ThrowIfDisposed();
        var result = new List<NetworkEntityId>();
        foreach (var record in _records)
            if (record.Owner == peer)
                result.Add(record.Id);
        return result;
    }

    public void Dispose() => Dispose(false);

    public void Dispose(bool destroyDynamicEntities)
    {
        if (_disposed)
            return;
        _disposed = true;

        var records = _records.ToArray();
        foreach (var record in records)
            record.Marker.UnbindDestroyed();
        _records.Clear();
        _byNetworkId.Clear();
        _byEntity.Clear();
        _playerEntities.Clear();
        _authoredBindings.Clear();

        if (!destroyDynamicEntities)
            return;
        foreach (var record in records)
            if (record.Origin == NetworkSpawnOrigin.DynamicBlueprint &&
                !Entity.IsDestroyed(record.Entity))
                DisableAndDestroy(record.Entity);
    }

    private void Register(
        Entity entity,
        NetworkObject marker,
        NetworkEntityId id,
        NetworkPeerId owner,
        NetworkSpawnOrigin origin,
        Guid sourceGuid,
        AssetId blueprintAssetId,
        string? blueprintAssetName,
        bool destroyWithOwner)
    {
        if (!id.IsValid)
            throw new ArgumentOutOfRangeException(nameof(id));
        if (!ReferenceEquals(entity.OwningScene, Scene))
            throw new InvalidOperationException("Cannot register an Entity owned by another Scene.");
        if (_byNetworkId.ContainsKey(id))
            throw new InvalidOperationException($"Network entity ID {id} is already registered.");
        if (_byEntity.ContainsKey(entity))
            throw new InvalidOperationException($"Entity '{entity.Name}' is already registered for networking.");
        EnsureRegistrationCapacity(1);

        var record = new NetworkEntityRecord
        {
            Id = id,
            Entity = entity,
            Marker = marker,
            Owner = owner,
            Origin = origin,
            SourceGuid = sourceGuid,
            BlueprintAssetId = blueprintAssetId,
            BlueprintAssetName = blueprintAssetName,
            DestroyWithOwner = destroyWithOwner,
            ReplicationBindings = _replication.CreateBindings(entity)
        };
        _byNetworkId.Add(id, record);
        _byEntity.Add(entity, id);
        _records.Add(record);
        marker.BindDestroyed(HandleEntityDestroyed);
    }

    private void EnsureRegistrationCapacity(int additionalEntities)
    {
        if (additionalEntities < 0 || additionalEntities > _maxNetworkEntities - _records.Count)
            throw new InvalidOperationException(
                $"Registering {additionalEntities} network Entity/Entities would exceed the configured " +
                $"MaxNetworkEntities limit of {_maxNetworkEntities} for Scene epoch {SceneEpoch}.");
    }

    private void HandleEntityDestroyed(Entity entity)
    {
        if (_disposed)
            return;
        if (_byEntity.TryGetValue(entity, out var id))
            Unregister(id);
    }

    private NetworkEntityRecord GetRecord(NetworkEntityId id) =>
        _byNetworkId.TryGetValue(id, out var record)
            ? record
            : throw new KeyNotFoundException(
                $"Network entity {id} is not registered in Scene epoch {SceneEpoch}.");

    private static void DisableAndDestroy(Entity entity)
    {
        if (Entity.IsDestroyed(entity))
            return;
        entity.Enabled = false;
        Entity.Destroy(entity);
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
