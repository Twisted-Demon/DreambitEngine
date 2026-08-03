# Physics queries

All queries return a Boolean and populate `CollisionResult`, whose `Collisions`
list contains matching colliders.

```csharp
if (PhysicsSystem.Instance.RayCastByTag(
        new Ray2D(origin, end),
        out var hits,
        ["enemy"]))
{
    foreach (var hit in hits.Collisions)
        Damage(hit.Entity);
}
```

Available query pairs are:

| All colliders | Tag filtered |
| --- | --- |
| `ColliderCast` | `ColliderCastByTag` |
| `PolygonCast` | `PolygonCastByTag` |
| `PointCast` | `PointCastByTag` |
| `RayCast` | `RayCastByTag` |
| `CircleCast` | `CircleCastByTag` |

Ray endpoints are world positions and form a finite segment. Circle casts test a
world-space center/radius against polygon colliders. Polygon casts expect an
already world-space `Polygon2D`.

Results are overlap sets, not ordered hit distances. If you need the nearest ray
hit, calculate intersections/distances for returned candidates and sort them.

