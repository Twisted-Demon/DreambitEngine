using System.Runtime.CompilerServices;
using Dreambit.BriGame.Components.InternalDev;
using Dreambit.ECS;
using LDtk;
using Microsoft.Xna.Framework;

namespace Dreambit.BriGame;

public partial class DevManager : LDtkEntity<DevManager>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this, "dev_manager", ["dev"]);
        entity.AttachComponent<DebugToggleComponent>();

        entity.Transform.Position = Vector3.Zero;
        
    }
}