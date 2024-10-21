using System.Globalization;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class Mover : Component
{
    private readonly Logger<Mover> _logger = new();
    public Vector3 Velocity;

    public override void OnUpdate()
    {
        Transform.Position += Velocity * Time.DeltaTime;
    }

    /// <summary>
    /// Moves the entity towards the target position
    /// returns true if it has arrived.
    /// </summary>
    /// <param name="targetPosition"></param>
    /// <param name="velocity"></param>
    /// <returns></returns>
    public bool MoveTo(Vector3 targetPosition, float velocity)
    {
        var position = Transform.WorldPosition;
        var direction = targetPosition - position;
        var distance = direction.Length();
        
        if(direction.Length() != 0)
            direction.Normalize();
        
        var adjustedSpeed = velocity * Time.DeltaTime;
        
        if (adjustedSpeed >= distance)
        {
            Transform.Position = targetPosition;
            return true;
        }

        Transform.Position += direction * adjustedSpeed;
        return false;
    }
}