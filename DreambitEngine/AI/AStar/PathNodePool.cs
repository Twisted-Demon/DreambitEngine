using System.Collections.Generic;

namespace Dreambit;

public class PathNodePool
{
    private readonly Stack<PathNode> _pool;

    public PathNodePool(int initialCapacity)
    {
        _pool = new Stack<PathNode>(initialCapacity);

        for (var i = 0; i < initialCapacity; i++)
            _pool.Push(new PathNode(0, 0, true));
    }


    public PathNode GetNode(int x, int y, bool isWalkable)
    {
        PathNode node;
        if (_pool.Count > 0)
        {
            node = _pool.Pop();
            node.Initialize(x, y, isWalkable);
        }
        else
        {
            node = new PathNode(x, y, isWalkable);
        }

        return node;
    }

    public void Return(PathNode node)
    {
        node.Reset();
        _pool.Push(node);
    }
}