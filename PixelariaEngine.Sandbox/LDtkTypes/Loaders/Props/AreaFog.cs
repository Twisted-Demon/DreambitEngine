using LDtk;
using PixelariaEngine.Graphics;

namespace PixelariaEngine.Sandbox;

public partial class AreaFog : LDtkEntity<AreaFog>
{
    protected override void SetUp(LDtkLevel level)
    {
        var e = CreateEntity(this, "areaFog", ["areaFog"]);

        var fog = e.AttachComponent<AreaFogComponent>();

        e.Transform.Position = Position.ToVector3();

        fog.PivotType = PivotType.TopLeft;
        fog.Width = (int)Size.X;
        fog.Height = (int)Size.Y;
    }
}