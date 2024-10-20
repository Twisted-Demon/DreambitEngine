using Microsoft.Xna.Framework;

namespace PixelariaEngine.ECS;

public class BoxCollider : Collider
{
    public override void OnCreated()
    {
        base.OnCreated();
        Bounds = Box.CreateSquare(Vector2.Zero, 5f);
    }

    public void SetShape(Box shape)
    {
        Bounds = shape;
    }
}