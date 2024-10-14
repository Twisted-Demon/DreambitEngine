using System;
using Microsoft.Xna.Framework;

namespace PixelariaEngine.ECS;

public class Mover : Component
{
    public Vector3 Velocity;

    public override void OnUpdate()
    {
        Transform.Position += Velocity * Time.DeltaTime;
    }
}