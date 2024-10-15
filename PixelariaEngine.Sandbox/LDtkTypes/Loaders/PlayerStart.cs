using LDtk;
using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;
using PixelariaEngine.Sandbox.Player;

namespace PixelariaEngine.Sandbox;

public partial class PlayerStart : LDtkEntity<PlayerStart>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this, tags: ["player"]);
        entity.Name = "player";
        Core.Instance.CurrentScene.MainCamera.ForcePosition(entity.Transform.WorldPosition);
        
        CreateCollider(entity);
        
        entity.AttachComponent<SandboxController>();
        entity.AttachComponent<CatFollowPoints>();

        SetUpFsm(entity);

    }

    private void CreateCollider(Entity parent)
    {
        var colliderEntity = Entity.CreateChildOf(parent, "player_collider", ["player"]);
        var collider = colliderEntity.AttachComponent<BoxCollider>();
        collider.SetShape(Box.CreateRectangle(new Vector2(0, -5), 5, 10));
        collider.IsTrigger = true;
    }

    private void SetUpFsm(Entity entity)
    {
        var fsm = entity.AttachComponent<FSM>();
        
        fsm.RegisterStates([
            typeof(PlayerFree),
            typeof(InDialogue)
        ]);
        
        fsm.SetInitialState<PlayerFree>();
    }
}