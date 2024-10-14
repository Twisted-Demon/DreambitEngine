using LDtk;
using Microsoft.Xna.Framework;

namespace PixelariaEngine.Sandbox;

public partial class StoneWall : LDtkEntity<StoneWall>
{
    protected override void SetUp(LDtkLevel level)
    {
        var e = CreateEntity(this, tags: ["Wall"]);
        AttachTileSpriteDrawer(this, e, RenderTiles,Color.White);
    }
}