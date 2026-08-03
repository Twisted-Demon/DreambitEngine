# AStarGrid

`AStarGrid` stores walkable cells for Dreambit's A* components. Its normal input
is an LDtk IntGrid layer:

```csharp
var grid = CreateEntity("managers")
    .AttachComponent<AStarGrid>()
    .InitializeGrid(collisionIntGrid);
```

IntGrid value `0` becomes walkable; nonzero values become blocked. The component
exposes `Width`, `Height`, `CellSize`, `GetNode`, `IsInBounds`, `IsWalkable`, and
`SetWalkable` for dynamic changes.

The current pathfinder looks up this component on an entity named `managers`, so
use that exact name unless you also replace the pathfinder implementation.

