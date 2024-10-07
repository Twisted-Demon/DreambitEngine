using LDtk;
using PixelariaEngine.Sandbox.Components;

namespace PixelariaEngine.Sandbox;

public partial class PlayerStart : LDtkEntity<PlayerStart>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this);
        entity.AttachComponent<SandboxController>();
    }
}