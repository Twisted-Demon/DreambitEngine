using System;
using Dreambit.ECS;
using Dreambit.Sandbox.Drawable;
using Microsoft.Xna.Framework.Audio;

namespace Dreambit.Sandbox;

public class ArthasSpellHandler : Component
{
    private InputActions _inputActions;
    private FSM _fsm;
    private SoundEffect _gongEffect;
    private Random _random = new();

    public float Cooldown = 1.0f;
    private float _cooldownTimer = 30f;
    private ScreenFog _screenFog;
    
    public override void OnAddedToEntity()
    {
        _inputActions = Entity.FindByName("player")?.GetComponent<InputActions>();
        _fsm = Entity.GetComponent<FSM>();
        _gongEffect = Resources.LoadAsset<SoundEffect>("Sounds/bellGong");
        _screenFog = Entity.FindByName("fog")?.GetComponent<ScreenFog>();

        if (_inputActions == null)
            return;

        _inputActions.ArthasSpellAction = OnArthasSpellAction;
    }

    public override void OnUpdate()
    {
        _cooldownTimer += Time.DeltaTime;
    }

    public override void OnRemovedFromEntity()
    {
        if(_inputActions != null)
            _inputActions.ArthasSpellAction = null;
    }

    private void OnArthasSpellAction()
    {
        if (_fsm.CurrentState is not (CatIdle or CatFollow)) return;
        
        if (_cooldownTimer < Cooldown) return;

        _cooldownTimer = 0;
        var bell = HolyBell.CreateInstance(Transform.WorldPosition);
        _fsm.SetNextState<CatCasting>();
        var animator = bell.GetComponent<SpriteAnimator>();
        animator.RegisterEvent("spell_sound", OnSpellSound);
    }

    private void OnSpellSound()
    {
        var gongInstance = _gongEffect.CreateInstance();
        gongInstance.Volume = 0.25f;
        var pitch = _random.Next(-0.75f, 0.25f);
        gongInstance.Pitch = pitch;
        gongInstance.Play();
        _screenFog.DimFog();
    }
}