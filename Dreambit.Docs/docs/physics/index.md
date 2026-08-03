# Physics

Dreambit physics is a lightweight 2D polygon collision and spatial-query system.
It provides a spatial-hash broad phase, polygon intersection, triggers,
point/ray/circle/polygon/collider casts, and simple movement components.

It is not a force-based rigid-body simulation: there is no mass, torque,
friction, restitution, joints, or automatic penetration solver. Gameplay code
chooses the collision response.

Typical setup:

```csharp
var wall = CreateEntity("wall", tags: ["solid"], createAt: position);
wall.AttachComponent<BoxCollider>()
    .SetShape(Box2D.CreateRectangle(Vector2.Zero, 32, 8));
```

Read [Colliders and triggers](colliders.md), then choose explicit
[queries](queries.md) or a movement pattern from [Movement](movement.md).

