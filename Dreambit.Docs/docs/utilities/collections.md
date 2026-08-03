# Collections and spatial helpers

Dreambit includes a few focused data structures:

- `PriorityQueue<T>` is a min-heap for `IComparable<T>` values with `Enqueue`,
  `Dequeue`, `Contains`, and `Count`. A* uses it for open nodes.
- `Quadtree<T>` stores values by 2D point and supports insert, remove, update,
  rectangular query, clearing, and debug drawing.
- `SpatialHash` is the physics broad phase. Most game code should use
  `PhysicsSystem` queries rather than manipulating it directly.

```csharp
var queue = new PriorityQueue<MyPriorityItem>(64);
queue.Enqueue(item);
var next = queue.Dequeue();

var tree = new Quadtree<Entity>(0, worldBounds);
tree.Insert(entity, position);
tree.Query(searchArea, results);
```

The quadtree indexes the position supplied by your code; call `Update` whenever
that position changes. Query results are appended to the provided list, so clear
it before reuse when you need only the latest query.
