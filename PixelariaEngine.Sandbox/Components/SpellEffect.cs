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

    public override void OnAddedToEntity()
    {
        _animator = Entity.GetComponent<SpriteAnimator>();
        _spellAnimation = Resources.LoadAsset<SpriteSheetAnimation>(SpellAnimationPath);
        _drawer = Entity.GetComponent<SpriteDrawer>();
        _drawer.DrawLayer = 4;
        
        _animator.Animation = _spellAnimation;
        _animator.Play();
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