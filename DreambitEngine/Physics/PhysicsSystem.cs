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
    private readonly SpatialHash _grid = new(16f);

    public void RegisterCollider(Collider c)
    {
        if (!_colliders.Contains(c)) _colliders.Add(c);

        var tags = c.Entity.Tags;

        foreach (var tag in tags)
        {
            if (!_byTag.TryGetValue(tag, out var list)) _byTag[tag] = list = new List<Collider>(64);
            if (!list.Contains(c)) list.Add(c);
        }

        var poly = c.GetTransformedPolygon();
        _grid.InsertOrUpdate(c, poly.ComputeAABB());
    }

    public void DeregisterCollider(Collider c)
    {
        _colliders.Remove(c);

        foreach (var tag in _byTag.Keys) _byTag[tag].Remove(c);
        _grid.Remove(c);
    }

    /// <summary>
    ///     This should be called when a collider's transform/shape changes
    /// </summary>
    /// <param name="c"></param>
    public void Touch(Collider c)
    {
        var poly = c.GetTransformedPolygon();
        _grid.InsertOrUpdate(c, poly.ComputeAABB());
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


        _candidateSet.Clear();
        var ap = @this.GetTransformedPolygon();
        var aabb = ap.ComputeAABB();

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


    public bool ColliderCastByTag(Collider @this, out CollisionResult result, params string[] tags)
    {
        result = new CollisionResult();
        if (@this == null) return false;

        _candidateSet.Clear();
        var ap = @this.GetTransformedPolygon();
        var aabb = ap.ComputeAABB();

        _candidateSet.Add(@this);
        _grid.QueryAABB(aabb, _candidateSet);

        foreach (var tag in tags)
        {
            if (!_byTag.TryGetValue(tag, out var tagged)) continue;
            foreach (var other in tagged)
            {
                if (!_candidateSet.Contains(other)) continue;
                if (other == null || other == @this) continue;
                if (!other.Enabled || !other.Entity.Enabled) continue;

                var op = other.GetTransformedPolygon();
                if (!ap.Intersects(op)) continue;
                result.Collisions.Add(other);
            }
        }

        return result.Collisions.Count > 0;
    }

    public bool PointCast(Vector2 p, out CollisionResult result)
    {
        result = new CollisionResult();

        _candidateList.Clear();
        _grid.QueryPoint(p, _candidateList);

        _candidateSet.Clear();
        for (var i = 0; i < _candidateList.Count; i++) _candidateSet.Add(_candidateList[i]);

        foreach (var other in _candidateSet)
        {
            if (other == null) continue;
            if (!other.Enabled || !other.Entity.Enabled) continue;

            var poly = other.GetTransformedPolygon();
            if (!poly.ContainsPoint(p)) continue;
            result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    public bool PointCastByTag(Vector2 p, out CollisionResult result, params string[] tags)
    {
        result = new CollisionResult();

        _candidateList.Clear();
        _grid.QueryPoint(p, _candidateList);

        _candidateSet.Clear();
        for (var i = 0; i < _candidateList.Count; i++) _candidateSet.Add(_candidateList[i]);

        foreach (var tag in tags)
        {
            if (!_byTag.TryGetValue(tag, out var tagged)) continue;
            foreach (var other in tagged)
            {
                if (!_candidateSet.Contains(other)) continue;
                if (!other.Enabled || !other.Entity.Enabled) continue;
                var poly = other.GetTransformedPolygon();
                if (poly.ContainsPoint(p)) result.Collisions.Add(other);
            }
        }

        return result.Collisions.Count > 0;
    }

    public bool RayCast(Ray2D ray, out CollisionResult result)
    {
        result = new CollisionResult();

        _candidateList.Clear();
        _grid.QueryRay(ray.Start, ray.End, _candidateList);

        _candidateSet.Clear();
        for (var i = 0; i < _candidateList.Count; i++) _candidateSet.Add(_candidateList[i]);

        foreach (var other in _candidateSet)
        {
            if (other == null) continue;
            if (!other.Enabled || !other.Entity.Enabled) continue;
            var poly = other.GetTransformedPolygon();
            if (!poly.RayIntersects(ray.Start, ray.End, out _)) continue;
            result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    public bool RayCastByTag(Ray2D ray, out CollisionResult result, params string[] tags)
    {
        result = new CollisionResult();
        _candidateList.Clear();
        _grid.QueryRay(ray.Start, ray.End, _candidateList);

        _candidateSet.Clear();
        for (var i = 0; i < _candidateList.Count; i++) _candidateSet.Add(_candidateList[i]);

        foreach (var tag in tags)
        {
            if (!_byTag.TryGetValue(tag, out var tagged)) continue;
            foreach (var other in tagged)
            {
                if (!_candidateSet.Contains(other)) continue;
                if (!other.Enabled || !other.Entity.Enabled) continue;
                var poly = other.GetTransformedPolygon();
                if (poly.RayIntersects(ray.Start, ray.End, out _)) result.Collisions.Add(other);
            }
        }

        return result.Collisions.Count > 0;
    }
}

public readonly struct CollisionResult()
{
    public List<Collider> Collisions { get; } = [];
    public int Count => Collisions.Count;

    public Collider this[int key] => Collisions[key];
}