using System;

namespace PixelariaEngine;

public class PathNode : IComparable<PathNode>
{
    public float GCost; // Cost from the start node
    public float HCost; // Heuristic cost to target node

    public PathNode(int x, int y, bool isWalkable)
    {
        Initialize(x, y, isWalkable);
    }

    public int X { get; internal set; }
    public int Y { get; internal set; }
    public bool IsWalkable { get; internal set; }
    public float FCost => GCost + HCost; // Total Cost
    public PathNode Parent { get; set; } // Parent node in path

    public int CompareTo(PathNode other)
    {
        var compare = FCost.CompareTo(other.FCost);

        if (compare == 0)
            compare = HCost.CompareTo(other.HCost);

        return compare;
    }

    public void Initialize(int x, int y, bool isWalkable)
    {
        X = x;
        Y = y;
        IsWalkable = isWalkable;
        Reset();
    }

    public void Reset()
    {
        GCost = float.MaxValue;
        HCost = 0;
        Parent = null;
    }

    public override bool Equals(object obj)
    {
        if (obj is not PathNode)
            return false;

        var other = (PathNode)obj;

        return X == other.X && Y == other.Y;
    }

    public override int GetHashCode()
    {
        unchecked // Allow arithmetic overflow without exceptions
        {
            var hash = 17;
            hash = hash * 31 + X;
            hash = hash * 31 + Y;
            return hash;
        }
    }
}