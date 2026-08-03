# CircleDrawer

`CircleDrawer` draws a world-space circle outline.

```csharp
var circle = entity.AttachComponent<CircleDrawer>();
circle.Radius = 48f;
circle.Segments = 48;
circle.LineThickness = 2f;
circle.Color = Color.Gold;
```

More segments produce a smoother circle at a higher draw cost. Scale segment
count to the on-screen radius. `LineThickness` is expressed in the draw helper's
world-space treatment and should be checked at your camera's pixels-per-unit.

The circle is visual only; physics circle queries are available through
`PhysicsSystem.CircleCast`, but there is no circle-collider component in the
current engine.

