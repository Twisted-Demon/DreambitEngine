using System;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox.AI.Arthas;

public class ArthasIdle : State<ArthasIdle>
{
    private SpriteDrawer _spriteDrawer;
    private SpriteAnimator _animator;
    private BlackboardVar<SpriteSheetAnimation> _idleAnimation;
    private BlackboardVar<SpriteSheetAnimation> _lickAnimation;
    private BlackboardVar<SpriteSheetAnimation> _sleepAnimation;
    private PolyShapeCollider _wakeUpCollider;

    private Random _random = new Random();
    private BlackboardVar<bool> _isAwake;
    
    public override void OnInitialize()
    {
        _isAwake = Blackboard.CreateVariable<bool>("isAwake", false);
        _idleAnimation = Blackboard.CreateVariable<SpriteSheetAnimation>("idleAnimation");
        _lickAnimation = Blackboard.CreateVariable<SpriteSheetAnimation>("lickAnimation");
        _sleepAnimation = Blackboard.CreateVariable<SpriteSheetAnimation>("sleepAnimation");
        
        _spriteDrawer = FSM.Entity.GetComponent<SpriteDrawer>();
        _animator = FSM.Entity.GetComponent<SpriteAnimator>();
        _wakeUpCollider = FSM.Entity.GetComponentInChildren<PolyShapeCollider>();

        _wakeUpCollider.OnCollisionEnter += OnCollisionEnter;

        _idleAnimation.Value = Resources.LoadAsset<SpriteSheetAnimation>("Animations/arthas_idle");
        _lickAnimation.Value = Resources.LoadAsset<SpriteSheetAnimation>("Animations/arthas_lick");
        _sleepAnimation.Value = Resources.LoadAsset<SpriteSheetAnimation>("Animations/arthas_sleep");
        

        _lickDelay = (float) _random.Next(5, 10);
        _animator.Animation = _sleepAnimation.Value;
        _animator.Play();
    }

    private void OnCollisionEnter(Collider other)
    {
        if (!Entity.CompareTag(other, "player")) return;
        _isAwake.Value = true;
        _animator.Animation = _idleAnimation.Value;
        
        _wakeUpCollider.OnCollisionEnter -= OnCollisionEnter;
        Entity.Destroy(_wakeUpCollider.Entity);
        _wakeUpCollider = null;
    }


    public override void OnExecute()
    {
        if (!_isAwake.Value)
            return;
        
        HandleLickAnimation();
    }
    
    private float _lickDelay = 0;
    private float _elapsedTimeSinceLick;

    private void HandleLickAnimation()
    {
        if (_animator.Animation == _sleepAnimation.Value || _animator.Animation == _lickAnimation.Value)
            return;
        
        _elapsedTimeSinceLick += Time.DeltaTime;
        
        if (_elapsedTimeSinceLick >= _lickDelay)
        {
            _elapsedTimeSinceLick = 0; 
            _lickDelay = (float) _random.Next(5, 10);
            
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