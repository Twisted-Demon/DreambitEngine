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

    public bool MoveTo(Vector3 targetPosition, float velocity)
    {
        var direction = targetPosition - Transform.WorldPosition;
        var distance = direction.Length();
        
        if (distance < float.Epsilon)
        {
            Transform.Position = targetPosition; // Snap to the target to avoid floating-point imprecision
            return true;
        }
        
        direction.Normalize();

        var intendedMovement = velocity * Time.DeltaTime;

        
        if (intendedMovement >= distance)
        {
            Transform.Position = targetPosition;
            return true;
        }

        Transform.Position += direction * intendedMovement;
        return false;

    }
}