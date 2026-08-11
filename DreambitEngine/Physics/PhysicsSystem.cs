using System.Collections.Generic;
using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit;

public class PhysicsSystem : Singleton<PhysicsSystem>
{
    private readonly Dictionary<string, List<Collider>> _byTag = [];
    private readonly List<Collider> _candidateList = new(256);

    //scratch buffers to avoid allocs per frame
    private readonly HashSet<Collider> _candidateSet = new(256);
    private readonly List<Collider> _colliders = [];
    private readonly SpatialHash _grid = new(6f);
    private readonly HashSet<Collider> _resultSet = new(256);

    public void RegisterCollider(Collider c)
    {
        if (c?.Bounds == null || !c.IsQueryable)
            return;

        if (!_colliders.Contains(c)) _colliders.Add(c);

        var tags = c.Entity.Tags;

        foreach (var tag in tags)
        {
            if (!_byTag.TryGetValue(tag, out var list)) _byTag[tag] = list = new List<Collider>(64);
            if (!list.Contains(c)) list.Add(c);
        }

        _grid.InsertOrUpdate(c, c.AABB);
    }

    public void DeregisterCollider(Collider collider)
    {
        _colliders.Remove(collider);

        foreach (var tag in collider.Entity.Tags)
        {
            if (!_byTag.TryGetValue(tag, out var list)) continue;

            list.Remove(collider);

            if (list.Count == 0)
                _byTag.Remove(tag);
        }

        _grid.Remove(collider);
    }

    /// <summary>
    ///     This should be called when a collider's transform/shape changes
    /// </summary>
    /// <param name="c"></param>
    public void Touch(Collider c)
    {
        if (c?.Bounds == null || !c.IsQueryable)
            return;

        _grid.InsertOrUpdate(c, c.AABB);
    }

    public void CleanUp()
    {
        _colliders.Clear();
        _byTag.Clear();
        _grid.Clear();
    }

    public bool ColliderCast(Collider @this, out CollisionResult result)
    {
        result = new CollisionResult();
        if (@this == null) return false;
        if (!IsColliderValid(@this)) return false;


        _candidateSet.Clear();
        var ap = @this.GetTransformedPolygon();
        var aabb = ap.ComputeAabb();

        _candidateSet.Add(@this);
        _grid.QueryAABB(aabb, _candidateSet);

        foreach (var other in _candidateSet)
        {
            if (other == null || other == @this) continue;
            if (!other.Enabled || !other.Entity.Enabled) continue;

            var bp = other.GetTransformedPolygon();
            if (!ap.Intersects(bp)) continue;
            result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    public bool PolygonCast(Polygon2D poly, out CollisionResult result)
    {
        result = new CollisionResult();

        _candidateSet.Clear();
        _resultSet.Clear();

        var aabb = poly.ComputeAabb();

        _grid.QueryAABB(aabb, _candidateSet);

        foreach (var other in _candidateSet)
        {
            if (!IsColliderValid(other)) continue;

            var otherPoly = other.GetTransformedPolygon();
            if (!poly.Intersects(otherPoly)) continue;

            if (_resultSet.Add(other))
                result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    public bool PolygonCastByTag(Polygon2D polygon, out CollisionResult result, IReadOnlyList<string> tags)
    {
        result = new CollisionResult();

        _candidateSet.Clear();

        var aabb = polygon.ComputeAabb();
        _grid.QueryAABB(aabb, _candidateSet);

        foreach (var other in _candidateSet)
        {
            if (!IsColliderValid(other)) continue;

            if (!other.Entity.HasAnyTag(tags)) continue;

            var otherPolygon = other.GetTransformedPolygon();

            if (!polygon.Intersects(otherPolygon)) continue;

            result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    public bool ColliderCastByTag(Collider collider, out CollisionResult result, IReadOnlyList<string> tags)
    {
        result = new CollisionResult();

        if (collider == null) return false;

        _candidateSet.Clear();

        var polygon = collider.GetTransformedPolygon();
        var aabb = polygon.ComputeAabb();

        _grid.QueryAABB(aabb, _candidateSet);

        foreach (var other in _candidateSet)
        {
            if (!IsColliderValid(other)) continue;

            if (!other.Entity.HasAnyTag([.. tags])) continue;

            var otherPolygon = other.GetTransformedPolygon();

            if (!polygon.Intersects(otherPolygon))
                continue;

            result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    public bool PointCast(Vector2 p, out CollisionResult result)
    {
        result = new CollisionResult();

        _candidateList.Clear();
        _grid.QueryPoint(p, _candidateList);

        _candidateSet.Clear();
        _resultSet.Clear();

        for (var i = 0; i < _candidateList.Count; i++)
            _candidateSet.Add(_candidateList[i]);

        foreach (var other in _candidateSet)
        {
            if (other == null) continue;
            if (!other.Enabled || !other.Entity.Enabled) continue;

            var poly = other.GetTransformedPolygon();
            if (!poly.ContainsPoint(p)) continue;

            if (_resultSet.Add(other))
                result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    public bool PointCastByTag(Vector2 point, out CollisionResult result, IReadOnlyList<string> tags)
    {
        result = new CollisionResult();

        _candidateList.Clear();
        _candidateSet.Clear();

        _grid.QueryPoint(point, _candidateList);

        for (var i = 0; i < _candidateList.Count; i++)
            _candidateSet.Add(_candidateList[i]);

        foreach (var other in _candidateSet)
        {
            if (!IsColliderValid(other)) continue;

            if (!other.Entity.HasAnyTag(tags))
                continue;

            var polygon = other.GetTransformedPolygon();

            if (!polygon.ContainsPoint(point))
                continue;

            result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    public bool RayCast(Ray2D ray, out CollisionResult result)
    {
        result = new CollisionResult();

        _candidateList.Clear();
        _grid.QueryRay(ray.Start, ray.End, _candidateList);

        _candidateSet.Clear();
        _resultSet.Clear();

        for (var i = 0; i < _candidateList.Count; i++)
            _candidateSet.Add(_candidateList[i]);

        foreach (var other in _candidateSet)
        {
            if (other == null) continue;
            if (!other.Enabled || !other.Entity.Enabled) continue;

            var poly = other.GetTransformedPolygon();
            if (!poly.RayIntersects(ray.Start, ray.End, out _)) continue;

            if (_resultSet.Add(other))
                result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    public bool RayCastByTag(Ray2D ray, out CollisionResult result, IReadOnlyList<string> tags)
    {
        result = new CollisionResult();

        _candidateList.Clear();
        _candidateSet.Clear();

        _grid.QueryRay(ray.Start, ray.End, _candidateList);

        for (var i = 0; i < _candidateList.Count; i++)
            _candidateSet.Add(_candidateList[i]);

        foreach (var other in _candidateSet)
        {
            if (!IsColliderValid(other)) continue;

            if (!other.Entity.HasAnyTag(tags)) continue;

            var polygon = other.GetTransformedPolygon();

            if (!polygon.RayIntersects(
                    ray.Start,
                    ray.End,
                    out _)) continue;

            result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    public bool CircleCast(Vector2 center, float radius, out CollisionResult result, AABB? aabb = null)
    {
        result = new CollisionResult();

        if (radius <= 0f) return false;

        _candidateList.Clear();
        _candidateSet.Clear();
        _resultSet.Clear();

        if (aabb is null)
            aabb = new AABB
            {
                Min = new Vector2(
                    center.X - radius,
                    center.Y - radius),

                Max = new Vector2(
                    center.X + radius,
                    center.Y + radius)
            };

        _grid.QueryAABB(aabb.Value, _candidateSet);

        foreach (var other in _candidateSet)
        {
            if (other == null) continue;

            if (!IsColliderValid(other)) continue;

            var polygon = other.GetTransformedPolygon();

            if (!polygon.IntersectsCircle(center, radius)) continue;

            if (_resultSet.Add(other))
                result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    public bool CircleCastByTag(Vector2 center, float radius, out CollisionResult result, IReadOnlyList<string> tags)
    {
        result = new CollisionResult();

        if (radius <= 0f) return false;

        _candidateSet.Clear();

        var aabb = new AABB
        {
            Min = new Vector2(
                center.X - radius,
                center.Y - radius),

            Max = new Vector2(
                center.X + radius,
                center.Y + radius)
        };

        _grid.QueryAABB(aabb, _candidateSet);

        foreach (var other in _candidateSet)
        {
            if (!IsColliderValid(other)) continue;

            if (!other.Entity.HasAnyTag(tags)) continue;

            var polygon = other.GetTransformedPolygon();

            if (!polygon.IntersectsCircle(center, radius)) continue;

            result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    private bool IsColliderValid(Collider collider)
    {
        if (collider == null) return false;

        return collider.Bounds != null &&
               collider.IsQueryable &&
               collider.Enabled &&
               collider.Entity?.Enabled == true;
    }
}

public readonly struct CollisionResult()
{
    public List<Collider> Collisions { get; } = [];
    public int Count => Collisions.Count;
    public Collider First => Collisions[0];

    public Collider this[int key] => Collisions[key];
}
