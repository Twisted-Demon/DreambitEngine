using Dreambit.ECS;
using Microsoft.Xna.Framework.Audio;

namespace Dreambit.Sandbox;

public class CatCasting : State<CatCasting>
{
    private BlackboardVar<SpriteSheetAnimation> _castingAnimation;
    private SpriteAnimator _animator;

    public override void OnInitialize()
    {
        _animator = Fsm.Entity.GetComponent<SpriteAnimator>();
        _castingAnimation = Fsm.Blackboard.GetVariable<SpriteSheetAnimation>("castingAnimation");
    }

    public override void OnEnter()
    {
        _animator.Animation = _castingAnimation.Value;
        _animator.QueueAnimation(_castingAnimation.Value);
        _animator.Play();
    }
    

    public override void OnExecute()
    {
        if(_animator.IsPlaying == false)
            Fsm.SetNextState<CatIdle>();
    }
}