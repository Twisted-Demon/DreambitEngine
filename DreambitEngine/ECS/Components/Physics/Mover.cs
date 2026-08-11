using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

[BlueprintType(nameof(Mover))]
public class Mover : Component
{
    private readonly Logger<Mover> _logger = new();
    public Vector2 Velocity;

    public override void OnUpdate()
    {
        Transform.Translate2D(Velocity * Time.DeltaTime);
    }

    /// <summary>
    ///     Moves the entity towards the target position
    ///     returns true if it has arrived.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="velocity"></param>
    /// <returns></returns>
    public void Move(Vector2 direction, float velocity)
    {
        Velocity = direction * velocity;
    }
}
