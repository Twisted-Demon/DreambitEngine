using System.Collections.Generic;
using System.Linq;
using LDtk;
using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox;

public partial class Wall : LDtkEntity<Wall>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this);
        if (Vertices is null) return;

        var bounds = CreatePolyCollider(entity, Vertices!, Position);
    }
}