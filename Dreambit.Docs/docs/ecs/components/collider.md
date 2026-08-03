# Collider

`Collider` is the base polygon-collision component. Most games use
[`BoxCollider`](box-collider.md) or [`PolyShapeCollider`](poly-shape-collider.md).

Important options are `IsTrigger`, `IsSilent`, `IsQueryable`, `Bounds`, and the
`InterestedIn` tag list. Queryable colliders register with the physics spatial
hash when attached and update their entry on physics ticks.

```csharp
collider.IsTrigger = true;
collider.InterestedIn.Add("player");
collider.OnCollisionEnter += other => OnPlayerEntered(other.Entity);
collider.OnCollisionExit += other => OnPlayerExited(other.Entity);
```

Trigger callbacks run from the component's normal update. `IsSilent` suppresses
these automatic callbacks without removing the collider from queries.

Call `ColliderCast(out var hits)` or `ColliderCastByTags(out var hits, "enemy")`
for explicit overlaps. Read [Colliders and triggers](../../physics/colliders.md)
for the full workflow.

