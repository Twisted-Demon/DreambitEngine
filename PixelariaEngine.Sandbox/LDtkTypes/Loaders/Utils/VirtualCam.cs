using LDtk;

namespace PixelariaEngine.Sandbox;

public partial class VirtualCam : LDtkEntity<VirtualCam>
{
    protected override void SetUp(LDtkLevel level)
    {
        CreateEntity(this, Iid.ToString());
    }
}