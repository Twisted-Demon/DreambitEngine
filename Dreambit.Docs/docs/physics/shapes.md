# Physics shapes

`Shape2D` wraps a polygon and supplies intersection and transform helpers.
Concrete creation helpers include:

```csharp
var square = Box2D.CreateSquare(Vector2.Zero, halfExtent: 8);
var box = Box2D.CreateRectangle(Vector2.Zero, halfWidth: 16, halfHeight: 8);
var triangle = PolyShape2D.Create([
    new Vector2(-8, 8), new Vector2(0, -8), new Vector2(8, 8)
]);
```

`Polygon2D` supports winding cleanup, centroid/edges, point containment, circle
and ray intersection, SAT/general polygon intersection, transform helpers, and
splitting. Keep vertex arrays non-null with at least three useful points.

Box dimensions are half extents. Shape points are local when assigned to a
collider; direct physics queries require world-space polygons.

`AABB` is the broad-phase envelope with `Min`, `Max`, `Intersects`, and
`ContainsPoint`. It does not replace narrow polygon tests.

