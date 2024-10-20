namespace PixelariaEngine.ECS;

public class PolyShapeCollider : Collider
{
    public void SetShape(PolyShape shape)
    {
        Bounds = shape;
    }
}