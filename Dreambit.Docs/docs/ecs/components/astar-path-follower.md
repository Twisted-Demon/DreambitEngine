# AStarPathFollower

`AStarPathFollower` requests a path and drives a required `Mover` along it.
Attaching it also ensures `AStarPathfinder` and `Mover` exist.

```csharp
var follower = agent.AttachComponent<AStarPathFollower>();
follower.SeekSpeed = 90f;
follower.Seek(targetWorldPosition);
```

`Pause` stops motion but retains the remaining queue. `Stop` clears the path.
`IsSeeking` and `PathLength` expose current state. With scene debug drawing
enabled, `OnDebugDraw` draws remaining path segments.

This component inherits the pathfinder's `managers` entity convention. Call
`Seek` only after that grid has been initialized and after the follower's entity
has completed attachment.

