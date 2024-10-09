using LDtk;
using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;
using PixelariaEngine.Sandbox.Components;

namespace PixelariaEngine.Sandbox;

public partial class PlayerStart : LDtkEntity<PlayerStart>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this);
        entity.Name = "player";
        var colliderEntity = Entity.CreateChildOf(entity, "player_collider", ["player"]);
        
        var collider = colliderEntity.AttachComponent<BoxCollider>();
        collider.SetShape(Box.CreateRectangle(new Vector2(0, -5), 5, 10));
        collider.IsTrigger = true;
        
        entity.AttachComponent<SandboxController>();
        
    }
}