using LDtk;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelariaEngine.ECS;
using PixelariaEngine.Sandbox.Components;

namespace PixelariaEngine.Sandbox;

public partial class BigTree : LDtkEntity<BigTree>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this);
        AttachTileSpriteDrawer(this, entity, RenderTiles, Color.White);
        
        var colliderEntity = Entity.CreateChildOf(entity, "foliage_collider", ["foliage_collider"]);
        var collider = colliderEntity.AttachComponent<BoxCollider>();
        var vec2Pivot = new Vector2(0, -75);
        collider.Bounds = Box.CreateRectangle(vec2Pivot, 54, 50);
        
        colliderEntity.AttachComponent<FoliageDimmer>();
    }
}