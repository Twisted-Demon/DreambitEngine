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
    private readonly Dictionary<CellKey, List<Collider>> _cells = new(1024);
    private readonly float _cellSize;
    private readonly Dictionary<Collider, CellKey[]> _colliderCells = new(256);
    private readonly float _invCell;

    public SpatialHash(float cellSize)
    {
        _cellSize = MathF.Max(1f, cellSize);
        _invCell = 1f / _cellSize;
    }

    public void Clear()
    {
        foreach (var kv in _cells) kv.Value.Clear();
        _colliderCells.Clear();
    }

    public void Remove(Collider c)
    {
        if (!_colliderCells.TryGetValue(c, out var prev)) return;
        for (var i = 0; i < prev.Length; i++)
            if (_cells.TryGetValue(prev[i], out var list))
            {
                // swap-remove to avoid O(n) remove
                var idx = list.IndexOf(c);
                if (idx >= 0)
                {
                    var last = list.Count - 1;
                    list[idx] = list[last];
                    list.RemoveAt(last);
                }
            }

        _colliderCells.Remove(c);
    }

    public void InsertOrUpdate(Collider c, AABB aabb)
    {
        // compute overlapped cells
        var minX = WorldToCell(aabb.Min.X);
        var minY = WorldToCell(aabb.Min.Y);
        var maxX = WorldToCell(aabb.Max.X);
        var maxY = WorldToCell(aabb.Max.Y);

        // remove from previous cells (if any)
        Remove(c);

        // count cells and store
        var count = (maxX - minX + 1) * (maxY - minY + 1);
        var cells = new CellKey[count];
        var k = 0;
        for (var y = minY; y <= maxY; y++)
        for (var x = minX; x <= maxX; x++)
        {
            var key = new CellKey(x, y);
            if (!_cells.TryGetValue(key, out var list))
                _cells[key] = list = new List<Collider>(4);
            list.Add(c);
            cells[k++] = key;
        }

        _colliderCells[c] = cells;
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
}