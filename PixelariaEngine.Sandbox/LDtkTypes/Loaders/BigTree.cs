using LDtk;

namespace PixelariaEngine.Sandbox;

public partial class BigTree : LDtkEntity<BigTree>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this);
        AttachTileSpriteDrawer(this, entity, RenderTiles);
    }
}