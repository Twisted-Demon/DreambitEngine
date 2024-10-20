using PixelariaEngine.ECS;
using PixelariaEngine.Scripting;

namespace PixelariaEngine.Sandbox;

public class EnableArthas : Script
{
    public override void OnUpdate()
    {
        var entity = Entity.FindByName("arthas");

        if (entity != null)
            entity.Enabled = true;

        IsComplete = true;
    }
}