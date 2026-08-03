# PolyShapeCollider

`PolyShapeCollider` accepts an arbitrary polygon shape:

```csharp
var shape = PolyShape2D.Create([
    new Vector2(-10, 8),
    new Vector2(0, -12),
    new Vector2(10, 8)
]);

entity.AttachComponent<PolyShapeCollider>().SetShape(shape);
```

Points are local to the entity. The collider uses the same query, trigger, tag,
and lifecycle behavior as `Collider`.

Convex shapes take the direct SAT path. General/concave polygons use the broader
polygon intersection path and are more expensive. Prefer a small set of convex
colliders for frequently moving gameplay objects.
