# BoxCollider

`BoxCollider` is a `Collider` whose local shape is normally a rectangle. Its
default is a square centered at zero with a half-extent of 5.

```csharp
var collider = entity.AttachComponent<BoxCollider>();
collider.SetShape(Box2D.CreateRectangle(
    center: Vector2.Zero,
    halfWidth: 8,
    halfHeight: 12));
```

The shape is local to the entity and is transformed by world position, rotation,
and scale during collision tests. Use half sizes with `CreateRectangle`, not
full width and height.

For JSON blueprints, `Bounds` can be supplied as polygon points. Keep vertices in
consistent counter-clockwise order; polygon cleanup normalizes supported input.

