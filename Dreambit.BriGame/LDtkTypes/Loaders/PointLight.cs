using Dreambit.ECS;
using LDtk;

namespace Dreambit.BriGame;

public partial class PointLight : LDtkEntity<PointLight>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this, tags: ["light"]);

        var pointLight = entity.AttachComponent<PointLight2D>();

        pointLight.Intensity = Intensity;
        pointLight.Radius = Radius;
        pointLight.Color = Color;
    }
}