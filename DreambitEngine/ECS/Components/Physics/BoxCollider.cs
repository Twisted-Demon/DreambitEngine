using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class BoxCollider : Collider
{
    public override void OnCreated()
    {
        base.OnCreated();
        Bounds ??= Box2D.CreateSquare(Vector2.Zero, 5.0f);
    }

    public void SetShape(Box2D shape)
    {
        Bounds = shape;
    }
}