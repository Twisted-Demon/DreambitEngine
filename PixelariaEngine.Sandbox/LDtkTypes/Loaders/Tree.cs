using LDtk;

namespace PixelariaEngine.Sandbox;

public partial class Tree : LDtkEntity<Tree>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this);
        AttachTileSpriteDrawer(this, entity, RenderTiles);
    }
}