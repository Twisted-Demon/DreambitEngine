using Dreambit.ECS;
using LDtk;
using Microsoft.Xna.Framework;

namespace Dreambit.Sandbox;

public partial class Tree4x5 : LDtkEntity<Tree4x5>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this);
        AttachTileSpriteDrawer(this, entity, RenderTiles, Color.White);
        
        var colliderEntity = Entity.CreateChildOf(entity, "foliage_collider", ["folliage_collider"]);
        
        if (!CanFade) return;
        var collider = colliderEntity.AttachComponent<BoxCollider>();
        var vec2Pivot = new Vector2(0.5f, -70);
        collider.Bounds = Box.CreateRectangle(vec2Pivot, 50, 55);
        collider.InterestedIn = ["player"];

        colliderEntity.AttachComponent<AlphaDimmer>();
    }
}