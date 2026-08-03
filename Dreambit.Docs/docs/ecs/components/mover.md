# Mover

`Mover` translates an entity by `Velocity * Time.DeltaTime` each frame:

```csharp
var mover = entity.AttachComponent<Mover>();
mover.Velocity = new Vector3(100, 0, 0);
```

`MoveTo(target, speed)` advances toward a point and returns true on arrival:

```csharp
if (mover.MoveTo(destination, 160f))
    mover.Velocity = Vector3.Zero;
```

`Mover` performs no collision checks. Add collision behavior separately or use
`TileMover` for A* grid walkability.

!!! warning "Parented entities"
    `MoveTo` measures from world position but writes local `Transform.Position`.
    Use it on unparented entities, or implement movement with world transform
    helpers when the entity has a transformed parent.

