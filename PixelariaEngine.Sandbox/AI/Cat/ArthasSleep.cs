using PixelariaEngine.ECS;
using PixelariaEngine.Sandbox.Drawable;
using PixelariaEngine.Sandbox.Utils;

namespace PixelariaEngine.Sandbox;

public class ArthasSleep : State<ArthasSleep>
{
    private InteractionListener _interactionListener;
    private FloatingIcon _floatingIcon;
    private BlackboardVar<SpriteSheetAnimation> _sleepAnimation;
    private SpriteAnimator _spriteAnimator;
    public override void OnInitialize()
    {
        _interactionListener = Fsm.Entity.AttachComponent<InteractionListener>();
        _floatingIcon = Fsm.Entity.GetComponentInChildren<FloatingIcon>();
        _spriteAnimator = Fsm.Entity.GetComponent<SpriteAnimator>();

        _floatingIcon.Icon = KeyboardIcon.E;
        _floatingIcon.Entity.Enabled = true;
        
        _interactionListener.OnPlayerInteract += OnPlayerInteracted;
        _interactionListener.InteractionRange = 35f;

        _sleepAnimation = Fsm.Blackboard.GetVariable<SpriteSheetAnimation>("sleepAnimation");
    }

    public override void OnEnter()
    {
        _spriteAnimator.Animation = _sleepAnimation.Value;
    }

    public override void OnExecute()
    {
        
    }

    public void OnPlayerInteracted()
    {
        _floatingIcon.Entity.Enabled = false;
        Fsm.SetNextState<CatIdle>();
    }
}