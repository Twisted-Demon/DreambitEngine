using System;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox;

public class CatIdle : State<CatIdle>
{
    private SpriteDrawer _spriteDrawer;
    private SpriteAnimator _animator;
    private BlackboardVar<SpriteSheetAnimation> _idleAnimation;
    private BlackboardVar<SpriteSheetAnimation> _lickAnimation;
    private BlackboardVar<SpriteSheetAnimation> _sleepAnimation;
    private BlackboardVar<Entity> _player;
    private BlackboardVar<float> _followDistance;
    
    private readonly Random _random = new ();

    
    public override void OnInitialize()
    {
        _idleAnimation = Fsm.Blackboard.GetVariable<SpriteSheetAnimation>("idleAnimation");
        _lickAnimation = Fsm.Blackboard.GetVariable<SpriteSheetAnimation>("lickAnimation");
        _sleepAnimation = Fsm.Blackboard.GetVariable<SpriteSheetAnimation>("sleepAnimation");
        _player = Fsm.Blackboard.GetVariable<Entity>("player");
        _followDistance = Fsm.Blackboard.GetVariable<float>("followDistance");
        
        _spriteDrawer = Fsm.Entity.GetComponent<SpriteDrawer>();
        _animator = Fsm.Entity.GetComponent<SpriteAnimator>();
        
        _animator.Animation = _idleAnimation.Value;

        _lickDelay = _random.Next(5, 10);
        
        _animator.Play();
    }

    public override void OnEnter()
    {
        _animator.ClearAnimationQueue();
        _animator.Animation = _idleAnimation.Value;
        
    }
    
    public override void OnExecute()
    {
        CheckForPlayer();
        
        HandleLickAnimation();
        CheckPlayerDistance();
    }

    private void CheckForPlayer()
    {
        if(_player.Value == null)
            _player.Value = Scene.GetEntity("player");
        
        if(_player.Value == null)
            Logger.Warn("Could not find player");
    }

    private void CheckPlayerDistance()
    {
        if (_player.Value == null) return;

        var playerPosition = _player.Value.Transform.WorldPosition;

        var direction = playerPosition - Transform.WorldPosition;
        
        var distance = direction.Length();
        
        if(distance > _followDistance.Value)
            Fsm.SetNextState<CatFollow>();
    }
    
    private float _lickDelay;
    private float _elapsedTimeSinceLick;

    private void HandleLickAnimation()
    {
        if (_animator.Animation == _sleepAnimation.Value || _animator.Animation == _lickAnimation.Value)
            return;
        
        _elapsedTimeSinceLick += Time.DeltaTime;
        
        if (_elapsedTimeSinceLick >= _lickDelay)
        {
            _elapsedTimeSinceLick = 0; 
            _lickDelay = _random.Next(5, 10);
            
            _animator.Animation = _lickAnimation.Value;
            
            var numOfLicks = _random.Next(2, 4);
            for (var i = 0; i < numOfLicks; i++)
            {
                _animator.QueueAnimation(_lickAnimation.Value);
            }
            _animator.QueueAnimation(_idleAnimation.Value);
        }
    }
}