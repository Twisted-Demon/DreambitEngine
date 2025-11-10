using System;
using Microsoft.Xna.Framework.Audio;

namespace Dreambit.ECS.Audio;

[Obsolete]
public class SoundListener2d : Component
{
    private readonly AudioListener _listener = new();
    private readonly Logger<SoundListener2d> _logger = new();

    private Mover _mover;

    public override void OnAddedToEntity()
    {
        _mover = Entity.GetComponent<Mover>();
        AudioSystem.Instance.RegisterListener(_listener);
    }

    public override void OnRemovedFromEntity()
    {
        AudioSystem.Instance.DeregisterListener(_listener);
    }

    public override void OnEnabled()
    {
        AudioSystem.Instance.RegisterListener(_listener);
    }

    public override void OnDisabled()
    {
        AudioSystem.Instance.DeregisterListener(_listener);
    }

    public override void OnDestroyed()
    {
        AudioSystem.Instance.DeregisterListener(_listener);
    }

    public override void OnUpdate()
    {
        _listener.Position = Transform.WorldPosition;

        if (_mover is not null)
            _listener.Velocity = _mover.Velocity;
    }
}