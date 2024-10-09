using System;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox.AI.Arthas;

public class ArthasIdle : State<ArthasIdle>
{
    private SpriteDrawer _spriteDrawer;
    private SpriteAnimator _animator;
    private SpriteSheetAnimation _idleAnimation;
    private SpriteSheetAnimation _lickAnimation;
    private SpriteSheetAnimation _sleepAnimation;
    private PolyShapeCollider _wakeUpCollider;

    private Random _random = new Random();
    private bool _isAwake = false;
    
    public override void OnInitialize()
    {
        _spriteDrawer = FSM.Entity.GetComponent<SpriteDrawer>();
        _animator = FSM.Entity.GetComponent<SpriteAnimator>();
        _wakeUpCollider = FSM.Entity.GetComponentInChildren<PolyShapeCollider>();

        _wakeUpCollider.OnCollisionEnter += OnCollisionEnter;

        _idleAnimation = Resources.Load<SpriteSheetAnimation>("Animations/arthas_idle");
        _lickAnimation = Resources.Load<SpriteSheetAnimation>("Animations/arthas_lick");
        _sleepAnimation = Resources.Load<SpriteSheetAnimation>("Animations/arthas_sleep");

        _lickDelay = (float) _random.Next(5, 10);
        _animator.Animation = _sleepAnimation;
        _animator.Play();
    }

    private void OnCollisionEnter(Collider other)
    {
        if (!Entity.CompareTag(other, "player")) return;
        _isAwake = true;
        _animator.Animation = _idleAnimation;
        
        _wakeUpCollider.OnCollisionEnter -= OnCollisionEnter;
        Entity.Destroy(_wakeUpCollider.Entity);
        _wakeUpCollider = null;
    }


    public override void OnExecute()
    {
        if (!_isAwake)
            return;
        
        HandleLickAnimation();
    }
    
    private float _lickDelay = 0;
    private float _elapsedTimeSinceLick;

    private void HandleLickAnimation()
    {
        if (_animator.Animation == _sleepAnimation || _animator.Animation == _lickAnimation)
            return;
        
        _elapsedTimeSinceLick += Time.DeltaTime;
        
        if (_elapsedTimeSinceLick >= _lickDelay)
        {
            _elapsedTimeSinceLick = 0; 
            _lickDelay = (float) _random.Next(5, 10);
            
            _animator.Animation = _lickAnimation;
            
            var numOfLicks = _random.Next(2, 4);
            for (var i = 0; i < numOfLicks; i++)
            {
                _animator.QueueAnimation(_lickAnimation);
            }
            _animator.QueueAnimation(_idleAnimation);
        }
    }
}