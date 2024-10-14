using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace PixelariaEngine.ECS;

public class AStarPathfinder: Component
{
    private AStarGrid _grid;
    private PathNode[,] _pathNodes;
    private PathNodePool _pathNodePool;
    private PriorityQueue<PathNode> _openList;
    private HashSet<PathNode> _closedList;
    private int _gridWidth;
    private int _gridHeight;
    
    public override void OnAddedToEntity()
    {
        _grid = Scene.GetEntity("managers").GetComponent<AStarGrid>();
        
        _gridWidth = _grid.Width;
        _gridHeight = _grid.Height;
        _pathNodes = new PathNode[_gridWidth, _gridHeight];
        _pathNodePool = new PathNodePool(_gridWidth * _gridHeight);
        InitializePathNodes();
        _openList = new PriorityQueue<PathNode>(_gridWidth * _gridHeight);
        _closedList = [];
        
    }

    private void InitializePathNodes()
    {
        for (var x = 0; x < _gridWidth; x++)
        {
            for (var y = 0; y < _gridHeight; y++)
            {
                var gridNode = _grid.GetNode(x, y);
                var pathNode = _pathNodePool.GetNode(x, y, gridNode.IsWalkable);
                _pathNodes[x, y] = pathNode;
            }
        }
    }

    public Queue<Node> FindPath(Vector2 startingWorldPosition, Vector2 targetWorldPosition, bool skipFirst = true)
    {
        var startX = (int)startingWorldPosition.X / _grid.CellSize;
        var startY = (int)startingWorldPosition.Y / _grid.CellSize;
        var targetX = (int)targetWorldPosition.X / _grid.CellSize;
        var targetY = (int)targetWorldPosition.Y / _grid.CellSize;

        //if start or target is out of bounds
        if (!_grid.IsInBounds(startX, startY) || !_grid.IsInBounds(targetX, targetY))
        {
            return [];
        }
        
        var startNode = _pathNodes[startX, startY];
        var targetNode = _pathNodes[targetX, targetY];

        //both are unwalkable
        if (!targetNode.IsWalkable)
            return [];

        //reset data structures
        ResetPathNodes();
        _openList = new PriorityQueue<PathNode>(_gridWidth * _gridHeight);
        _closedList.Clear();

        startNode.GCost = 0;
        startNode.HCost = CalculateHeuristic(startNode, targetNode);
        startNode.Parent = null;
        
        _openList.Enqueue(startNode);

        while (_openList.Count > 0)
        {
            var currentNode = _openList.Dequeue();

            if (currentNode.Equals(targetNode))
            {
                var path = RetracePath(targetNode);
                var pathQueue = new Queue<Node>(path.Count);

                // skip the first
                for (var i = 0; i < path.Count; i++)
                {
                    if (skipFirst && i == 0)
                        continue;
                    
                    pathQueue.Enqueue(path[i]);
                }

                return pathQueue;
            }

            _closedList.Add(currentNode);

            foreach (var neighbor in GetNeighbors(currentNode))
            {
                if (_closedList.Contains(neighbor) || !neighbor.IsWalkable)
                    continue;
                
                var tentativeGCost = currentNode.GCost + CalculateDistance(currentNode, neighbor);

                if (!(tentativeGCost < neighbor.GCost)) continue;
                
                neighbor.Parent = currentNode;
                neighbor.GCost = tentativeGCost;
                neighbor.HCost = CalculateHeuristic(neighbor, targetNode);
                    
                if(!_openList.Contains(neighbor))
                    _openList.Enqueue(neighbor);
            }
        }

        return []; // no path found
    }
    
    private void ResetPathNodes()
    {
        for (var x = 0; x < _gridWidth; x++)
        {
            for (var y = 0; y < _gridHeight; y++)
            {
                var node = _pathNodes[x, y];
                node.Reset();
                node.IsWalkable = _grid.GetNode(x, y).IsWalkable;
            }
        }
    }

    private List<Node> RetracePath(PathNode endNode)
    {
        var path = new List<Node>();
        var currentNode = endNode;

        while (currentNode != null)
        {
            path.Add(new Node(currentNode.X * _grid.CellSize, currentNode.Y * _grid.CellSize, currentNode.IsWalkable));
            currentNode = currentNode.Parent;
        }

        path.Reverse();
        return path;
    }

    private float CalculateHeuristic(PathNode a, PathNode b)
    {
        var dx = Math.Abs(a.X - b.X);
        var dy = Math.Abs(a.Y - b.Y);
        var D = 1f;
        var D2 = 1.4142f;

        return D * (dx + dy) + (D2 - 2 * D) * Math.Min(dx, dy);
    }
    
    private float CalculateDistance(PathNode a, PathNode b)
    {
        // Assuming movement cost between adjacent nodes is constant
        var dx = Math.Abs(a.X - b.X);
        var dy = Math.Abs(a.Y - b.Y);

        if (dx == 1 && dy == 1)
            return 1.4142f; // Diagonal movement
        
        return 1f; // Horizontal or vertical movement
    }
    
    private IEnumerable<PathNode> GetNeighbors(PathNode node)
    {
        var neighbors = new List<PathNode>(8);

        for (var xOffset = -1; xOffset <= 1; xOffset++)
        {
            var x = node.X + xOffset;
            if (x < 0 || x >= _gridWidth)
                continue;

            for (var yOffset = -1; yOffset <= 1; yOffset++)
            {
                var y = node.Y + yOffset;
                if (y < 0 || y >= _gridHeight)
                    continue;

                if (xOffset == 0 && yOffset == 0)
                    continue; // Skip the current node

                neighbors.Add(_pathNodes[x, y]);
            }
        }

        return neighbors;
    }
    
}