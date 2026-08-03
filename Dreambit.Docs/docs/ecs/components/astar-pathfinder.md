# AStarPathfinder

`AStarPathfinder` computes an eight-direction path over the scene's `AStarGrid`.
It expects the grid on an entity named `managers` and initializes its node pool
when attached.

```csharp
var pathfinder = agent.AttachComponent<AStarPathfinder>();
Queue<Node> path = pathfinder.FindPath(
    agent.Transform.WorldPosition2D,
    target,
    skipFirst: true);
```

Returned `Node.X` and `Node.Y` values are world positions at cell origins, not
grid indices. An empty queue means the request was out of bounds, the target was
blocked, or no path was found.

The search allows diagonal moves and does not currently prevent diagonal corner
cutting. Account for that in collision geometry or implement a stricter neighbor
rule for agents that must not pass between touching obstacles.

