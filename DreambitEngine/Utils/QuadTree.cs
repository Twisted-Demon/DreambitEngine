using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Dreambit;

public class Quadtree<T>
{
    private const int MaxObjects = 12; // Maximum objects in a node before it splits
    private const int MaxLevels = 8; // Maximum levels to prevent infinite recursion
    private readonly List<(T Entity, Vector2 Position)> _entities; // Entities in this node

    private readonly int _level; // Current level of this node
    private readonly Quadtree<T>[] _nodes; // Child nodes (NE, NW, SW, SE)
    private Rectangle _bounds; // Bounding box of this node

    public Quadtree(int level, Rectangle bounds)
    {
        _level = level;
        _bounds = bounds;
        _entities = new List<(T, Vector2)>();
        _nodes = new Quadtree<T>[4]; // Four child nodes
    }

    /// <summary>
    ///     Clears the quadtree.
    /// </summary>
    public void Clear()
    {
        _entities.Clear();

        // Recursively clear all child nodes
        for (var i = 0; i < 4; i++)
            if (_nodes[i] != null)
            {
                _nodes[i].Clear();
                _nodes[i] = null;
            }
    }

    /// <summary>
    ///     Splits the node into four subnodes.
    /// </summary>
    private void Split()
    {
        var subWidth = _bounds.Width / 2;
        var subHeight = _bounds.Height / 2;
        var x = _bounds.X;
        var y = _bounds.Y;

        // Adjust for odd dimensions to ensure the entire area is covered
        var extraWidth = _bounds.Width % 2;
        var extraHeight = _bounds.Height % 2;

        _nodes[0] = new Quadtree<T>(_level + 1, new Rectangle(x + subWidth, y, subWidth + extraWidth, subHeight)); // NE
        _nodes[1] = new Quadtree<T>(_level + 1, new Rectangle(x, y, subWidth, subHeight)); // NW
        _nodes[2] = new Quadtree<T>(_level + 1,
            new Rectangle(x, y + subHeight, subWidth, subHeight + extraHeight)); // SW
        _nodes[3] = new Quadtree<T>(_level + 1,
            new Rectangle(x + subWidth, y + subHeight, subWidth + extraWidth, subHeight + extraHeight)); // SE
    }

    /// <summary>
    ///     Determines which node the object belongs to.
    /// </summary>
    private int GetIndex(Vector2 position)
    {
        var index = -1;
        var verticalMidpoint = _bounds.X + _bounds.Width / 2;
        var horizontalMidpoint = _bounds.Y + _bounds.Height / 2;

        var topQuadrant = position.Y < horizontalMidpoint;
        var bottomQuadrant = position.Y >= horizontalMidpoint;
        var leftQuadrant = position.X < verticalMidpoint;
        var rightQuadrant = position.X >= verticalMidpoint;

        if (topQuadrant)
        {
            if (rightQuadrant)
                index = 0; // NE
            else if (leftQuadrant)
                index = 1; // NW
        }
        else if (bottomQuadrant)
        {
            if (leftQuadrant)
                index = 2; // SW
            else if (rightQuadrant)
                index = 3; // SE
        }

        return index;
    }

    /// <summary>
    ///     Inserts an object into the quadtree.
    /// </summary>
    public void Insert(T entity, Vector2 position)
    {
        if (_nodes[0] != null)
        {
            var index = GetIndex(position);

            if (index != -1)
            {
                _nodes[index].Insert(entity, position);
                return;
            }
        }

        _entities.Add((entity, position));

        if (_entities.Count > MaxObjects && _level < MaxLevels)
        {
            if (_nodes[0] == null)
                Split();

            var i = 0;
            while (i < _entities.Count)
            {
                var index = GetIndex(_entities[i].Position);
                if (index != -1)
                {
                    var item = _entities[i];
                    _entities.RemoveAt(i);
                    _nodes[index].Insert(item.Entity, item.Position);
                }
                else
                {
                    i++;
                }
            }
        }
    }

    /// <summary>
    ///     Removes an object from the quadtree.
    /// </summary>
    public bool Remove(T entity, Vector2 position)
    {
        var removed = false;

        var index = GetIndex(position);
        if (index != -1 && _nodes[0] != null)
            removed = _nodes[index].Remove(entity, position);
        else
            for (var i = 0; i < _entities.Count; i++)
                if (_entities[i].Entity.Equals(entity) && _entities[i].Position.Equals(position))
                {
                    _entities.RemoveAt(i);
                    removed = true;
                    break;
                }

        if (removed && _nodes[0] != null)
        {
            // Optionally merge nodes if necessary
            var totalObjects = _entities.Count;
            foreach (var node in _nodes)
                if (node != null)
                    totalObjects += node._entities.Count;

            if (totalObjects < MaxObjects)
                // Merge all child nodes into this node
                for (var i = 0; i < 4; i++)
                    if (_nodes[i] != null)
                    {
                        _entities.AddRange(_nodes[i]._entities);
                        _nodes[i] = null;
                    }
        }

        return removed;
    }

    /// <summary>
    ///     Updates the position of an object in the quadtree.
    /// </summary>
    public void Update(T entity, Vector2 oldPosition, Vector2 newPosition)
    {
        Remove(entity, oldPosition);
        Insert(entity, newPosition);
    }

    /// <summary>
    ///     Retrieves all objects that could collide with the given area.
    /// </summary>
    public void Query(Rectangle area, List<T> results)
    {
        if (!_bounds.Intersects(area))
            return;

        foreach (var entity in _entities)
            if (area.Contains(entity.Position))
                results.Add(entity.Entity);

        if (_nodes[0] != null)
            foreach (var node in _nodes)
                node.Query(area, results);
    }

    /// <summary>
    ///     Debugging method to visualize the quadtree.
    /// </summary>
    public void DebugDraw()
    {
        Core.SpriteBatch.DrawHollowRectangle(_bounds, Color.White);

        if (_nodes[0] != null)
            foreach (var node in _nodes)
                node.DebugDraw();
    }
}