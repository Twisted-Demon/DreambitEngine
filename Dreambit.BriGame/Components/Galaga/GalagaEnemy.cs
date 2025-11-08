using System.Threading.Tasks.Dataflow;
using Dreambit.BriGame.Components.Galaga;
using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.BriGame.Components.Galaga;

[Require(typeof(SpriteDrawer), typeof(Mover), typeof(BoxCollider))]
public class GalagaEnemy : Component
{
    private FSM _fsm;
    private EnemyBlackboard Blackboard => _fsm.GetBlackBoard<EnemyBlackboard>();
    private BoxCollider _collider;

    public override void OnCreated()
    {
        SetUpFsm();
        SetUpSpriteSheets();
    }

    public override void OnAddedToEntity()
    {
        _fsm.GetBlackBoard<EnemyBlackboard>().GalagaPlayer =
            ECS.Entity.FindByName("player").GetComponent<GalagaPlayer>();
        _collider = Entity.GetComponent<BoxCollider>();
        _collider.SetShape(Box2D.CreateSquare(Vector2.Zero, 7));
        _collider.IsTrigger = true;
        _collider.IsSilent = true;
    }

    public void SetFormationSlot(FormationSlot slot)
    {
        slot.SetEnemy(this);
        Blackboard.FormationSlot = slot;
    }

    public void DetachFromFormationSlot()
    {
        Blackboard.FormationSlot.DetachEnemy();
        Blackboard.FormationSlot = null;
    }

    private void SetUpFsm()
    {
        _fsm = Entity.AttachComponent<FSM>();
        _fsm.SetBlackboard(new EnemyBlackboard());
        
        _fsm.Register([
            typeof(EnemyFlyIn),
            typeof(EnemyFlyToFormationSlot)
        ]);

        _fsm.SetDefaultState<EnemyFlyIn>();

        var bb = _fsm.GetBlackBoard<EnemyBlackboard>();
        _fsm.AddTransition<EnemyFlyIn, EnemyFlyToFormationSlot>(fsm => bb.ArrivedToFlyInPoint);
    }

    private void SetUpSpriteSheets()
    {
        var bb = _fsm.GetBlackBoard<EnemyBlackboard>();
        
        bb.BossHealthySpriteSheet = SpriteSheet.Create(8, 1, "Textures/Galaga/boss_healthy");
        bb.BossHurtSpriteSheet = SpriteSheet.Create(8, 1, "Textures/Galaga/boss_hurt");
        bb.BeeSpriteSheet = SpriteSheet.Create(8, 1, "Textures/Galaga/bee");
        bb.ButterflySpriteSheet = SpriteSheet.Create(8, 1, "Textures/Galaga/butterfly");
    }
}

public enum EnemyType
{
    Boss,
    Butterfly,
    Bee
}