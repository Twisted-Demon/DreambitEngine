using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.BriGame.Components.Galaga;
public class EnemyFlyIn : State<EnemyFlyIn>
{
    private EnemyBlackboard _bb;
    private SpriteDrawer _drawer;
    private Mover _mover;

    private readonly Vector3 _destination = new (112, 160, 0);

    public override void OnInitialize()
    {
        _bb = Fsm.Blackboard as EnemyBlackboard;
        _drawer = Fsm.Entity.GetComponent<SpriteDrawer>();
        _mover = Fsm.Entity.GetComponent<Mover>();
    }

    public override void OnEnter()
    {
        if (_bb.EnemyType == EnemyType.Bee)
        {
            _drawer.SpriteSheet = _bb.BeeSpriteSheet;
            _drawer.SetFrame(7);
        }
    }

    public override void OnExecute()
    {
        if(_mover.MoveTo(_destination, _bb.FlySpeed))
        {
            _bb.ArrivedToFlyInPoint = true;
        }
        else
        {
            HandleRotation(_destination);
        }
    }

    public override bool Reason()
    {
        return !_bb.ArrivedToFlyInPoint;
    }

    private void HandleRotation(Vector3 destination)
    {
        var angle = Mathf.AngleBetweenVectors(Fsm.Transform.Position.ToVector2(), destination.ToVector2());

        Fsm.Transform.Rotation.Z = angle;
    }
}