using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Dreambit.ECS.Audio;

[Obsolete]
public class SoundEmitter2d : Component
{
    private readonly Logger<SoundEmitter2d> _logger = new();

    private Mover _mover;
    
    private string _soundEffectPath = string.Empty;
    public string SoundEffectPath
    {
        get => _soundEffectPath;
        set
        {
            var sfx = Resources.LoadAsset<SoundEffect>(value);

            if (sfx is null) return;

            _soundEffect = sfx;
            _soundEffectPath = value;
        }
    }

    private SoundEffect _soundEffect;
    private readonly AudioEmitter _emitter = new();

    public float Volume = 1.0f;
    public float Pitch = 0.0f;
    public float Pan = 1.0f;
    public float DopplerScale = 1.0f;

    public bool UseDoppler = true;

    public override void OnAddedToEntity()
    {
        _mover = Entity.GetComponent<Mover>();
        AudioSystem.Instance.RegisterEmitter(_emitter);
    }

    public override void OnRemovedFromEntity()
    {
        AudioSystem.Instance.DeregisterEmitter(_emitter);
    }

    public override void OnDestroyed()
    {
        AudioSystem.Instance.DeregisterEmitter(_emitter);
    }

    public override void OnDisabled()
    {
        AudioSystem.Instance.DeregisterEmitter(_emitter);
    }

    public override void OnEnabled()
    {
        AudioSystem.Instance.RegisterEmitter(_emitter);
    }

    public override void OnUpdate()
    {
        _emitter.Position = Transform.WorldPosition;
        
        if(_mover is not null)
            _emitter.Velocity = _mover.Velocity;
        
        if (!UseDoppler)
            _emitter.DopplerScale = 0.0f;
        else
            _emitter.DopplerScale = DopplerScale;
    }

    public SoundEffectInstance Play()
    {
        var instance = _soundEffect.CreateInstance();
        
        instance.Volume = Volume;
        instance.Pitch = Pitch;
        instance.Pan = Pan;
        
        instance.Play();
        
        return instance;
    }

    public SoundEffectInstance Play3D()
    {
        var instance = _soundEffect.CreateInstance();
        instance.Volume = Volume;
        instance.Pitch = Pitch;
        instance.Pan = Pan;
        
        instance.Play();
        
        var listeners = AudioSystem.Instance.Listeners;
        instance.Apply3D(listeners.ToArray().First(),  _emitter);
        return instance;
    }
}