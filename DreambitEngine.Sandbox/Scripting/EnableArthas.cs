using Dreambit.ECS;
using Dreambit.Scripting;

namespace Dreambit.Sandbox;

public class EnableArthas : ScriptAction
{
    public override void OnUpdate()
    {
        var entity = Entity.FindByName("arthas");

        if (entity != null)
            entity.Enabled = true;

        IsComplete = true;
    }
}