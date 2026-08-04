using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit;

public readonly struct CellKey(int x, int y) : IEquatable<CellKey>
{
    public readonly int X = x, Y = y;

    public bool Equals(CellKey other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object o)
    {
        return o is CellKey k && Equals(k);
    }

    public override int GetHashCode()
    {
        return unchecked((X * 73856093) ^ (Y * 19349663));
    }
}

public sealed class SpatialHash
{
    private readonly float _cellSize;

    private readonly Dictionary<CellKey, List<Collider>> _cells = new(1024);
    private readonly Dictionary<Collider, CellRange> _colliderCells = new(256);
    private readonly float _invCell;

    public SpatialHash(float cellSize)
    {
        _cellSize = MathF.Max(1f, cellSize);
        _invCell = 1f / _cellSize;
    }

    public void Clear()
    {
        _cells.Clear();
        _colliderCells.Clear();
    }

    public void Remove(Collider collider)
    {
        if (!_colliderCells.Remove(
                collider,
                out var previousRange))
            return;

        RemoveFromCells(collider, previousRange);
    }

    private void RemoveFromCells(Collider collider, CellRange range)
    {
        for (var y = range.MinY; y <= range.MaxY; y++)
        for (var x = range.MinX; x <= range.MaxX; x++)
        {
            var key = new CellKey(x, y);

            if (!_cells.TryGetValue(key, out var list))
                continue;

            var index = list.IndexOf(collider);

            if (index < 0)
                continue;

            // Swap-remove avoids shifting every later element.
            var lastIndex = list.Count - 1;

            list[index] = list[lastIndex];
            list.RemoveAt(lastIndex);

            // Do not retain thousands of permanently empty cells.
            if (list.Count == 0)
                _cells.Remove(key);
        }
    }

    public void InsertOrUpdate(Collider collider, AABB aabb)
    {
        var newRange = new CellRange(
            WorldToCell(aabb.Min.X),
            WorldToCell(aabb.Min.Y),
            WorldToCell(aabb.Max.X),
            WorldToCell(aabb.Max.Y));

        if (_colliderCells.TryGetValue(collider, out var previousRange))
        {
            if (previousRange == newRange)
                return;

            RemoveFromCells(collider, previousRange);
        }

        for (var y = newRange.MinY; y <= newRange.MaxY; y++)
        for (var x = newRange.MinX; x <= newRange.MaxX; x++)
        {
            var key = new CellKey(x, y);

            if (!_cells.TryGetValue(key, out var list))
            {
                list = new List<Collider>(4);
                _cells.Add(key, list);
            }

            list.Add(collider);
        }

        _colliderCells[collider] = newRange;
    }

    public void QueryAABB(AABB aabb, HashSet<Collider> outSet)
    {
        var minX = WorldToCell(aabb.Min.X);
        var minY = WorldToCell(aabb.Min.Y);
        var maxX = WorldToCell(aabb.Max.X);
        var maxY = WorldToCell(aabb.Max.Y);

        for (var y = minY; y <= maxY; y++)
        for (var x = minX; x <= maxX; x++)
        {
            var key = new CellKey(x, y);
            if (_cells.TryGetValue(key, out var list))
                for (var i = 0; i < list.Count; i++)
                    outSet.Add(list[i]);
        }
    }

    public void QueryPoint(Vector2 p, List<Collider> outList)
    {
        var key = new CellKey(WorldToCell(p.X), WorldToCell(p.Y));
        if (_cells.TryGetValue(key, out var list))
            outList.AddRange(list);
    }

    // Fast voxel traversal for rays (2D DDA)
    public void QueryRay(Vector2 start, Vector2 end, List<Collider> outList, float maxStep = 4096f)
    {
        var dir = end - start;
        var x = WorldToCell(start.X);
        var y = WorldToCell(start.Y);
        var targetX = WorldToCell(end.X);
        var targetY = WorldToCell(end.Y);

        var stepX = Math.Sign(dir.X);
        var stepY = Math.Sign(dir.Y);

        float tMaxX, tMaxY;
        var tDeltaX = dir.X == 0 ? float.PositiveInfinity : MathF.Abs(_cellSize / dir.X);
        var tDeltaY = dir.Y == 0 ? float.PositiveInfinity : MathF.Abs(_cellSize / dir.Y);

        var cellBorderX = (x + (stepX > 0 ? 1 : 0)) * _cellSize;
        var cellBorderY = (y + (stepY > 0 ? 1 : 0)) * _cellSize;
        tMaxX = dir.X == 0 ? float.PositiveInfinity : (cellBorderX - start.X) / dir.X;
        tMaxY = dir.Y == 0 ? float.PositiveInfinity : (cellBorderY - start.Y) / dir.Y;

        var steps = 0;
        while (steps++ < maxStep)
        {
            var key = new CellKey(x, y);
            if (_cells.TryGetValue(key, out var list))
                outList.AddRange(list);

            if (x == targetX && y == targetY) break;

            if (tMaxX < tMaxY)
            {
                x += stepX;
                tMaxX += tDeltaX;
            }
            else
            {
                y += stepY;
                tMaxY += tDeltaY;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int WorldToCell(float v)
    {
        return (int)MathF.Floor(v * _invCell);
    }

    private readonly record struct CellRange(
        int MinX,
        int MinY,
        int MaxX,
        int MaxY);
}