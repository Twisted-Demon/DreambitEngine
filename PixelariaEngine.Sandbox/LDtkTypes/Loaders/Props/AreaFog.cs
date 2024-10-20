using LDtk;
using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;
using PixelariaEngine.Graphics;

namespace PixelariaEngine.Sandbox;

public partial class AreaFog : LDtkEntity<AreaFog>
{
    protected override void SetUp(LDtkLevel level)
    {
        var e = CreateEntity(this, Iid.ToString(), ["areaFog"]);

        var fog = e.AttachComponent<AreaFogComponent>();

        e.Transform.Position = Position.ToVector3();

        fog.PivotType = PivotType.TopLeft;
        fog.Width = (int)Size.X;
        fog.Height = (int)Size.Y;
    }
}