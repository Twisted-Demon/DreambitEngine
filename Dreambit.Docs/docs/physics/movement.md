# Movement and collision response

Choose the simplest model that matches your game.

## Unrestricted movement

Use transform helpers or `Mover`:

```csharp
Transform.TranslateWorld2D(velocity * Time.DeltaTime);
```

## Roll back on overlap

`RigidBody2D` stores the old position, applies velocity, casts its configured
collider, and restores the old position on any match. It is useful for a simple
Pong-like response but does not slide.

## Custom response

For control over the hit, snapshot, move, query, then respond:

```csharp
var oldPosition = Transform.WorldPosition;
Transform.TranslateWorld2D(velocity * Time.DeltaTime);

if (PhysicsSystem.Instance.ColliderCastByTag(
        _collider, out var result, ["solid"]))
{
    Transform.WorldPosition = oldPosition;
    velocity.X *= -1;
}
```

For sliding, test X and Y movement separately or resolve with a minimum
translation vector from polygon intersection. For fast projectiles, cast the
swept segment between old and desired positions so thin colliders cannot be
skipped.

`TileMover` is an occupancy check against the A* grid, not polygon collision.
