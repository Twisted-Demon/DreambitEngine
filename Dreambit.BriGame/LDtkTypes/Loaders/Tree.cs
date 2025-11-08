

using Dreambit.ECS;
using LDtk;
using Microsoft.Xna.Framework;

namespace Dreambit.BriGame;

public partial class Tree : LDtkEntity<Tree>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this);
        AttachTileSpriteDrawer(this, entity, RenderTiles, Color.White);

        var collider = Entity.CreateChildOf(entity, "foliage_collider", ["foliage"])
            .AttachComponent<BoxCollider>();
        var vec2Pivot = new Vector2(0.5f, -70);
        collider.Bounds = Box2D.CreateRectangle(vec2Pivot, 50, 45);
        collider.IsTrigger = true;
        collider.IsSilent = true;
        collider.InterestedIn = ["player"];
    }
}