using LDtk;
using Microsoft.Xna.Framework;

namespace Dreambit.Sandbox;

public partial class Prop2x1 : LDtkEntity<Prop2x1>
{
    protected override void SetUp(LDtkLevel level)
    {
        var e = CreateEntity(this, tags: ["prop"]);
        AttachTileSpriteDrawer(this, e, RenderTiles, Color.White);
    }
}