using System;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox.AI.Arthas;

public class ArthasIdle : State<ArthasIdle>
{
    private SpriteDrawer _spriteDrawer;
    private AnimatedSprite _animator;
    private SpriteSheetAnimation _idleAnimation;
    private SpriteSheetAnimation _lickAnimation;

    private Random _random = new Random();
    
    public override void OnInitialize()
    {
        _spriteDrawer = FSM.Entity.GetComponent<SpriteDrawer>();
        _animator = FSM.Entity.GetComponent<AnimatedSprite>();

        _idleAnimation = Resources.Load<SpriteSheetAnimation>("Animations/arthas_idle");
        _lickAnimation = Resources.Load<SpriteSheetAnimation>("Animations/arthas_lick");

        _lickDelay = (float) _random.Next(5, 10);
        _animator.SpriteSheetAnimation = _idleAnimation;
    }

    public override void OnExecute()
    {
        HandleAnimation();
    }
    
    private float _lickDelay = 0;
    private float _elapsedTimeSinceLick;

    private void HandleAnimation()
    {
        if (_animator.SpriteSheetAnimation.AssetName == "Animations/arthas_lick")
            return;
        
        _elapsedTimeSinceLick += Time.DeltaTime;
        
        if (_elapsedTimeSinceLick >= _lickDelay)
        {
            _elapsedTimeSinceLick = 0; 
            _lickDelay = (float) _random.Next(5, 10);
            
            _animator.SpriteSheetAnimation = _lickAnimation;
            
            var numOfLicks = _random.Next(3, 5);
            for (var i = 0; i < numOfLicks; i++)
            {
                _animator.QueueAnimation(_lickAnimation);
            }
            _animator.QueueAnimation(_idleAnimation);
        }
    }
}