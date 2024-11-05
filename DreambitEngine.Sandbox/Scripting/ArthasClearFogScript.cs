using System;
using Dreambit.ECS;
using Dreambit.Scripting;
using Microsoft.Xna.Framework.Audio;

namespace Dreambit.Sandbox;

public class ArthasClearFogScript : ScriptAction
{
    private Entity _arthas;
    private SoundEffect _gongEffect;
    private readonly Random _random = new();
    private readonly string[] _fogsToClear;
    private SpriteSheetAnimation _castingAnimation;
    private SpriteSheetAnimation _idleAnimation;
    
    public ArthasClearFogScript(string[] fogsToClear)
    {
        _fogsToClear = fogsToClear;
    }

    public override void OnStart()
    {
        _arthas = Entity.FindByName("arthas");
        _gongEffect = Resources.LoadAsset<SoundEffect>("Sounds/bellGong");
        _castingAnimation = Resources.LoadAsset<SpriteSheetAnimation>("Animations/arthas_cast");
        _idleAnimation = Resources.LoadAsset<SpriteSheetAnimation>("Animations/arthas_idle");
    }

    public override void OnUpdate()
    {
        var bell = HolyBell.CreateInstance(_arthas.Transform.WorldPosition);
        var animator = bell.GetComponent<SpriteAnimator>();
        animator.RegisterEvent("spell_sound", OnSpellSound);

        var arthasAnimator = _arthas.GetComponent<SpriteAnimator>();
        arthasAnimator.Animation = _castingAnimation;
        arthasAnimator.QueueAnimation(_idleAnimation);
        
        IsComplete = true;
    }
    
    private void OnSpellSound()
    {
        var gongInstance = _gongEffect.CreateInstance();
        gongInstance.Volume = 0.25f;
        var pitch = _random.Next(-0.75f, 0.25f);
        gongInstance.Pitch = pitch;
        gongInstance.Play();

        foreach (var fog in _fogsToClear)
        {
            var iid = Guid.Parse(fog);
            var e = Entity.FindById(iid);
            e.Enabled = false;
        }
    }
}