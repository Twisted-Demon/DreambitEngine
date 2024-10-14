using LDtk;
using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox;

public partial class Tree4x6 : LDtkEntity<Tree4x6>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this);
        AttachTileSpriteDrawer(this, entity, RenderTiles, Color.White);
        
        var colliderEntity = Entity.CreateChildOf(entity, "foliage_collider", ["folliage_collider"]);

        if (!CanFade) return;
        var collider = colliderEntity.AttachComponent<BoxCollider>();
        var vec2Pivot = new Vector2(0.5f, -90);
        collider.Bounds = Box.CreateRectangle(vec2Pivot, 50, 70);
        collider.InterestedIn = ["player"];

        colliderEntity.AttachComponent<AlphaDimmer>();
    }
}