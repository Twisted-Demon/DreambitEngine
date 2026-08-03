# Colliders and triggers

A collider owns a local `Shape2D`; queries transform it by the entity's complete
world transform. Use `BoxCollider` for rectangles and `PolyShapeCollider` for
other polygons.

```csharp
var trigger = entity.AttachComponent<BoxCollider>();
trigger.SetShape(Box2D.CreateRectangle(Vector2.Zero, 24, 24));
trigger.IsTrigger = true;
trigger.InterestedIn.Add("player");
trigger.OnCollisionEnter += other => OpenDoor();
trigger.OnCollisionExit += other => CloseDoor();
```

`OnCollisionStay` fires for every current overlap on each trigger check, including
the entry frame. `IsSilent` disables automatic trigger checks/callbacks.
`IsQueryable = false` prevents broad-phase participation.

Enabled colliders register automatically. Transform position changes refresh
their AABB during physics updates. If custom code changes a shape in place after
registration, make sure its AABB is refreshed; replacing/configuring shapes
before attachment is the safest path.

Collision filters are tags on the other collider's entity. Keep tags stable
after registration because the physics system's tag index is built when a
collider registers.

