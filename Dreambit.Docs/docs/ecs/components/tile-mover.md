# TileMover

`TileMover` applies velocity only when the destination remains walkable in the
scene's `AStarGrid`.

```csharp
var mover = entity.AttachComponent<TileMover>();
mover.Velocity = new Vector3(80, 0, 0);
```

Like the A* components, it expects an initialized `AStarGrid` on an entity named
`managers`. Each frame it checks the desired destination and either moves the
full step or does not move.

This is grid occupancy, not polygon collision or swept collision. Fast movement
can skip narrow blocked areas, and blocked diagonal motion does not slide. Use a
smaller speed/step or a custom controller when those behaviors matter.
