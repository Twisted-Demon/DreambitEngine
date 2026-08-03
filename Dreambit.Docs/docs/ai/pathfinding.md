# A* pathfinding

Create an initialized grid manager first, then agents:

```csharp
var managers = CreateEntity("managers");
managers.AttachComponent<AStarGrid>().InitializeGrid(collisionLayer);

var follower = CreateEntity("guard", createAt: start)
    .AttachComponent<AStarPathFollower>();
follower.SeekSpeed = 80f;
follower.Seek(target);
```

`AStarPathFollower` requires and attaches `AStarPathfinder` plus `Mover`.
Pathfinder setup finds the `managers` entity during attachment, so initialize the
grid before attaching agents.

The grid maps LDtk value zero to walkable. Change runtime cells through
`SetWalkable(x, y, value)`. World coordinates are divided by `CellSize` to find
cells.

Paths use horizontal, vertical, and diagonal neighbors. Empty paths indicate
blocked/out-of-bounds targets or no route. The current implementation allows
diagonal corner cutting and returns cell-origin world positions; adjust agent
radius and target placement accordingly.

Enable scene debug mode to draw a follower's remaining line segments.

