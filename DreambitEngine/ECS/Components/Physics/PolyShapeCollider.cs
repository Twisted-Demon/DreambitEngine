namespace Dreambit.ECS;

public class PolyShapeCollider : Collider
{
    public void SetShape(PolyShape2D shape2D)
    {
        Bounds = shape2D;
    }
}