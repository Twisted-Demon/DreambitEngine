using System;
using Dreambit.ECS;

namespace Dreambit.Networking;

public enum NetworkPresence : byte
{
    Replicated = 0,
    ServerOnly = 1,
    ClientOnly = 2
}

/// <summary>
/// Inert authored marker for network-managed entities. Runtime identity and ownership live in
/// NetworkWorld and are deliberately not Dreambit-serialized here.
/// </summary>
public sealed class NetworkObject : Component
{
    private Action<Entity>? _destroyed;

    [DreambitSerialize]
    public NetworkPresence Presence { get; set; } = NetworkPresence.Replicated;

    internal void BindDestroyed(Action<Entity> destroyed)
    {
        _destroyed = destroyed ?? throw new ArgumentNullException(nameof(destroyed));
    }

    internal void UnbindDestroyed()
    {
        _destroyed = null;
    }

    public override void OnDestroyed()
    {
        var entity = Entity;
        if (entity is not null)
            _destroyed?.Invoke(entity);
        _destroyed = null;
        base.OnDestroyed();
    }
}
