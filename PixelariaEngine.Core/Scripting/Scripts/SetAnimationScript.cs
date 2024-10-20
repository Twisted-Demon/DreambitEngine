using PixelariaEngine.ECS;

namespace PixelariaEngine.Scripting;

public class SetAnimationScript : Script
{
    private readonly Logger<SetAnimationScript> _logger = new();
    
    private SpriteSheetAnimation _animation;
    private SpriteAnimator _animator;
    private string _entityName;
    private string _animationName;

    public SetAnimationScript(string entity, string animation)
    {
        _animationName = animation;
        _entityName = entity;
    }

    public override void OnStart()
    {
        _animation = Resources.LoadAsset<SpriteSheetAnimation>(_animationName);
        
        var entity = Entity.FindByName(_entityName);
        _animator = entity.GetComponent<SpriteAnimator>();
    }

    public override void OnUpdate()
    {
        if (_animator == null)
        {
            _logger.Warn("No Animator Found");
            IsComplete = true;
            return;
        }
        
        if (_animation == null)
        {
            _logger.Warn("Animation {0} Found", _animationName);
            IsComplete = true;
            return;
        }
        
        _animator.Animation = _animation;
        IsComplete = true;
    }
    
    
}