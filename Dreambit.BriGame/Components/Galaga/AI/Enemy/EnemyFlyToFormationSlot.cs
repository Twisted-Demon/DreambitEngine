using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.BriGame.Components.Galaga;

public class EnemyFlyToFormationSlot : State<EnemyFlyToFormationSlot>
{
    private EnemyBlackboard _bb;
    private SpriteDrawer _drawer;
    private Mover _mover;

    public override void OnInitialize()
    {
        _bb = Fsm.GetBlackBoard<EnemyBlackboard>();
        _drawer = Fsm.Entity.GetComponent<SpriteDrawer>();
        _mover = Fsm.Entity.GetComponent<Mover>();
    }
    
    public override void OnEnter()
    {
        if (_bb.EnemyType == EnemyType.Bee)
        {
            _drawer.SpriteSheet = _bb.BeeSpriteSheet;
            _drawer.SetFrame(0);
        }
    }

    public override void OnExecute()
    {
        var slot = _bb.FormationSlot;
        var slotPos = FormationManager.GetSlotPosition(slot).ToVector3();

        _mover.MoveTo(slotPos, _bb.FlySpeed);
        HandleRotation(slotPos);
    }
    
    private void HandleRotation(Vector3 destination)
    {
        var angle = Mathf.AngleBetweenVectors(Fsm.Transform.Position.ToVector2(), destination.ToVector2());

        Fsm.Transform.Rotation.Z = -angle;
    }
}