using Dreambit.ECS;

namespace Dreambit.Scripting;

public class SetAnimationScript : ScriptAction
{
    private readonly Logger<SetAnimationScript> _logger = new();
    
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

        _animator.SetAnimation(_animationName);
        IsComplete = true;
        
        if (_animator.Animation is null)
            _logger.Warn("Animation {0} Found", _animationName);
    }
    
    
}