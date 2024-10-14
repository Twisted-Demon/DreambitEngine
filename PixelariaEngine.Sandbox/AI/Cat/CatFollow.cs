using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox;

public class CatFollow : State<CatFollow>
{
    private BlackboardVar<float> _followSpeed;
    private BlackboardVar<SpriteSheetAnimation> _runAnimation;
    private BlackboardVar<string> _catType;
    private SpriteDrawer _spriteDrawer;
    private Mover _mover;
    private SpriteAnimator _spriteAnimator;
    private PathFollower _pathFollower;
    private CatFollowPoints _followPoints;

    public override void OnInitialize()
    {
        _followSpeed = Fsm.Blackboard.GetVariable<float>("followSpeed");
        _runAnimation = Fsm.Blackboard.GetVariable<SpriteSheetAnimation>("runAnimation");
        _catType = Fsm.Blackboard.GetVariable<string>("catType");

        _mover = Fsm.Entity.GetComponent<Mover>();
        _spriteAnimator = Fsm.Entity.GetComponent<SpriteAnimator>();
        _spriteDrawer = Fsm.Entity.GetComponent<SpriteDrawer>();
        _pathFollower = Fsm.Entity.GetComponent<PathFollower>();
        
        _pathFollower = Fsm.Entity.GetComponent<PathFollower>();
        _pathFollower.SeekSpeed = _followSpeed.Value;
    }

    public override void OnEnter()
    {
        _spriteAnimator.ClearAnimationQueue();
        _spriteAnimator.Animation = _runAnimation.Value;
        _followPoints = Entity.FindByName("player").GetComponent<CatFollowPoints>();
        
        if(_followPoints != null)
            GenPathToDestination();
    }
    

    public override void OnEnd()
    {
        _pathFollower.Stop();
    }

    public override void OnExecute()
    {
        if (_followPoints == null) return;
        
        FollowPlayer();
        HandleAnimation();

        if (!_pathFollower.IsSeeking)
        {
            Fsm.SetNextState<CatIdle>();
            _mover.Velocity = Vector3.Zero;
        }
    }

    private void GenPathToDestination()
    {
        var destination = GetFollowPoint();
        
        _pathFollower.Seek(destination);
    }

    private Vector2 GetFollowPoint()
    {
        var destination = Vector2.Zero;

        switch (_catType.Value)
        {
            case "Arthas":
                destination = _followPoints.ArthasPos.ToVector2();
                break;
            case "Darion":
                destination = _followPoints.DarionPos.ToVector2();
                break;
        }

        return destination;
    }
    

    private float _pathElapsedTime;
    private void FollowPlayer()
    {
        _pathElapsedTime += Time.DeltaTime;

        if (!(_pathElapsedTime >= 0.25f)) return;
        _pathElapsedTime -= 0.1f;
        GenPathToDestination();
    }

    private void HandleAnimation()
    {
        var xVelocity = _mover.Velocity.X;

        _spriteDrawer.IsHorizontalFlip = xVelocity switch
        {
            > 0 => false,
            < 0 => true,
            _ => _spriteDrawer.IsHorizontalFlip
        };
    }
}