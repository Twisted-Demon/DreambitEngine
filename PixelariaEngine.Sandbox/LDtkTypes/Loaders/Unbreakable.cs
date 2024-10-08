using LDtk;
using Microsoft.Xna.Framework;

namespace PixelariaEngine.Sandbox;

public partial class Unbreakable : LDtkEntity<Unbreakable>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this);
        AttachTileSpriteDrawer(this, entity, RenderTiles, Color.White);
    }
}