using Microsoft.Xna.Framework.Audio;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox;

[Require(typeof(SpriteAnimator), typeof(SpriteDrawer))]
public class SpellEffect : Component
{
    private readonly Logger<SpellEffect> _logger = new();
    
    public string SpellAnimationPath;
    
    private SpriteDrawer _drawer;
    private SpriteAnimator _animator;
    private SpriteSheetAnimation _spellAnimation;

    public string SoundEffectPath = string.Empty;
    private SoundEffect _soundEffect;

    public override void OnAddedToEntity()
    {
        _animator = Entity.GetComponent<SpriteAnimator>();
        _spellAnimation = Resources.LoadAsset<SpriteSheetAnimation>(SpellAnimationPath);
        _drawer = Entity.GetComponent<SpriteDrawer>();
        _drawer.DrawLayer = 4;
        
        _animator.Animation = _spellAnimation;
        _animator.Play();

        if (SoundEffectPath != string.Empty)
            _soundEffect = Resources.LoadAsset<SoundEffect>(SoundEffectPath);

        if (_soundEffect == null) return;

        var soundInstance = _soundEffect.CreateInstance();

        soundInstance.Volume = 0.5f;
        soundInstance.Play();
    }

    public override void OnUpdate()
    {
        if (!_animator.IsPlaying)
        {
            Entity.Destroy(Entity);
        }
    }

    public override void OnDestroyed()
    {
        _logger.Debug("Effect Faded");
    }
}