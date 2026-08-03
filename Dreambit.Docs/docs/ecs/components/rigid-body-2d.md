# RigidBody2D

`RigidBody2D` applies a velocity each update and restores the previous position
when its configured collider overlaps another collider.

```csharp
var body = entity.AttachComponent<RigidBody2D>();
var collider = entity.AttachComponent<BoxCollider>();
body.SetCollider(collider);
body.SetInterestedTags("wall", "obstacle");
body.Velocity = new Vector2(120, 0);
```

You must call `SetCollider`; the component does not discover one automatically.
With no interested tags it collides against every queryable collider. This is a
simple all-or-nothing response: it does not apply mass, forces, restitution,
sliding, or penetration resolution.

Use it for uncomplicated blocked movement. For character sliding or projectiles
that react to a particular hit, perform a cast and implement the response in a
custom component. See [Movement and collision response](../../physics/movement.md).

