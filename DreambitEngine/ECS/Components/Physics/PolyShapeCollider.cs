namespace Dreambit.ECS;

[BlueprintType($"Dreambit.{nameof(PolyShapeCollider)}")]
public class PolyShapeCollider : Collider
{
    public void SetShape(PolyShape2D shape2D)
    {
        Bounds = shape2D;
    }
}