using LDtk;
using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox;

public partial class Tree3x4 : LDtkEntity<Tree3x4>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this);
        AttachTileSpriteDrawer(this, entity, RenderTiles, Color.White);
        
        var colliderEntity = Entity.CreateChildOf(entity, "foliage_collider", ["folliage_collider"]);

        if (!CanFade) return;
        var collider = colliderEntity.AttachComponent<BoxCollider>();
        var vec2Pivot = new Vector2(0.5f, -70);
        collider.Bounds = Box.CreateRectangle(vec2Pivot, 50, 45);
        collider.InterestedIn = ["player"];

        colliderEntity.AttachComponent<AlphaDimmer>();
    }
}