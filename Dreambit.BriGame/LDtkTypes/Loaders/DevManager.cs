using Dreambit.BriGame.Components.InternalDev;
using LDtk;

namespace Dreambit.BriGame;

public partial class DevManager : LDtkEntity<DevManager>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this, "dev_manager", ["dev"]);
        entity.AttachComponent<DebugToggleComponent>();
    }
}