using Dreambit.ECS;
using LDtk;

namespace Dreambit.BriGame;

public partial class AmbientLight : LDtkEntity<AmbientLight>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this, tags: ["light"]);

        var ambientLight = entity.AttachComponent<AmbientLight2D>();
        ambientLight.Color = Color;
    }
}