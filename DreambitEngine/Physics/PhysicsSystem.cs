using System.Collections.Generic;
using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit;

public class PhysicsSystem : Singleton<PhysicsSystem>
{
    private readonly List<Collider> _candidateList =
        new(256);

    private readonly HashSet<Collider> _candidateSet =
        new(256);

    private readonly SpatialHash _grid =
        new(6f);

    #region Registration

    public void RegisterCollider(
        Collider collider)
    {
        if (collider?.Bounds == null ||
            !collider.IsQueryable)
        {
            return;
        }

        _grid.InsertOrUpdate(
            collider,
            collider.AABB);
    }

    public void DeregisterCollider(
        Collider collider)
    {
        if (collider == null)
            return;

        _grid.Remove(collider);
    }

    /// <summary>
    /// Called when a registered collider's world bounds may have changed.
    /// </summary>
    public void Touch(
        Collider collider)
    {
        if (collider?.Bounds == null ||
            !collider.IsQueryable)
        {
            return;
        }

        _grid.InsertOrUpdate(
            collider,
            collider.AABB);
    }

    public void CleanUp()
    {
        _candidateList.Clear();
        _candidateSet.Clear();
        _grid.Clear();
    }

    #endregion

    #region Collider Cast

    public bool ColliderCast(
        Collider collider,
        out CollisionResult result)
    {
        result = new CollisionResult();

        if (!IsColliderValid(collider))
            return false;

        _candidateSet.Clear();

        var polygon =
            collider.GetTransformedPolygon();

        _grid.QueryAABB(
            collider.AABB,
            _candidateSet);

        foreach (var other in _candidateSet)
        {
            if (ReferenceEquals(other, collider))
                continue;

            if (!IsColliderValid(other))
                continue;

            var otherPolygon =
                other.GetTransformedPolygon();

            if (!polygon.Intersects(otherPolygon))
                continue;

            result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    public bool ColliderCastByTag(
        Collider collider,
        out CollisionResult result,
        IReadOnlyList<string> tags)
    {
        result = new CollisionResult();

        if (!IsColliderValid(collider))
            return false;

        _candidateSet.Clear();

        var polygon =
            collider.GetTransformedPolygon();

        _grid.QueryAABB(
            collider.AABB,
            _candidateSet);

        foreach (var other in _candidateSet)
        {
            if (ReferenceEquals(other, collider))
                continue;

            if (!IsColliderValid(other))
                continue;

            if (!other.Entity.HasAnyTag(tags))
                continue;

            var otherPolygon =
                other.GetTransformedPolygon();

            if (!polygon.Intersects(otherPolygon))
                continue;

            result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    /// <summary>
    /// Boolean-only collider query for hot paths such as RigidBody2D.
    /// Does not allocate a CollisionResult or result List.
    /// </summary>
    internal bool ColliderCastAny(
        Collider collider)
    {
        if (!IsColliderValid(collider))
            return false;

        _candidateSet.Clear();

        var polygon =
            collider.GetTransformedPolygon();

        _grid.QueryAABB(
            collider.AABB,
            _candidateSet);

        foreach (var other in _candidateSet)
        {
            if (ReferenceEquals(other, collider))
                continue;

            if (!IsColliderValid(other))
                continue;

            var otherPolygon =
                other.GetTransformedPolygon();

            if (polygon.Intersects(otherPolygon))
                return true;
        }

        return false;
    }

    /// <summary>
    /// HashSet-specialized tagged boolean cast used by RigidBody2D.
    /// Avoids converting InterestedTags to a temporary array.
    /// </summary>
    internal bool ColliderCastAnyByTag(
        Collider collider,
        HashSet<string> tags)
    {
        if (!IsColliderValid(collider))
            return false;

        _candidateSet.Clear();

        var polygon =
            collider.GetTransformedPolygon();

        _grid.QueryAABB(
            collider.AABB,
            _candidateSet);

        foreach (var other in _candidateSet)
        {
            if (ReferenceEquals(other, collider))
                continue;

            if (!IsColliderValid(other))
                continue;

            if (!HasAnyTag(
                    other.Entity,
                    tags))
            {
                continue;
            }

            var otherPolygon =
                other.GetTransformedPolygon();

            if (polygon.Intersects(otherPolygon))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Fills reusable trigger storage directly.
    /// The destination set is cleared before use.
    /// </summary>
    internal bool CollectColliderOverlaps(
        Collider collider,
        HashSet<Collider> destination)
    {
        destination.Clear();

        if (!IsColliderValid(collider))
            return false;

        _candidateSet.Clear();

        var polygon =
            collider.GetTransformedPolygon();

        _grid.QueryAABB(
            collider.AABB,
            _candidateSet);

        foreach (var other in _candidateSet)
        {
            if (ReferenceEquals(other, collider))
                continue;

            if (!IsColliderValid(other))
                continue;

            var otherPolygon =
                other.GetTransformedPolygon();

            if (!polygon.Intersects(otherPolygon))
                continue;

            destination.Add(other);
        }

        return destination.Count > 0;
    }

    /// <summary>
    /// Tagged reusable trigger query.
    /// </summary>
    internal bool CollectColliderOverlapsByTag(
        Collider collider,
        HashSet<Collider> destination,
        IReadOnlyList<string> tags)
    {
        destination.Clear();

        if (!IsColliderValid(collider))
            return false;

        _candidateSet.Clear();

        var polygon =
            collider.GetTransformedPolygon();

        _grid.QueryAABB(
            collider.AABB,
            _candidateSet);

        foreach (var other in _candidateSet)
        {
            if (ReferenceEquals(other, collider))
                continue;

            if (!IsColliderValid(other))
                continue;

            if (!other.Entity.HasAnyTag(tags))
                continue;

            var otherPolygon =
                other.GetTransformedPolygon();

            if (!polygon.Intersects(otherPolygon))
                continue;

            destination.Add(other);
        }

        return destination.Count > 0;
    }

    #endregion

    #region Polygon Cast

    public bool PolygonCast(
        Polygon2D polygon,
        out CollisionResult result)
    {
        result = new CollisionResult();

        _candidateSet.Clear();

        var aabb =
            polygon.ComputeAabb();

        _grid.QueryAABB(
            aabb,
            _candidateSet);

        foreach (var other in _candidateSet)
        {
            if (!IsColliderValid(other))
                continue;

            var otherPolygon =
                other.GetTransformedPolygon();

            if (!polygon.Intersects(otherPolygon))
                continue;

            result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    public bool PolygonCastByTag(
        Polygon2D polygon,
        out CollisionResult result,
        IReadOnlyList<string> tags)
    {
        result = new CollisionResult();

        _candidateSet.Clear();

        var aabb =
            polygon.ComputeAabb();

        _grid.QueryAABB(
            aabb,
            _candidateSet);

        foreach (var other in _candidateSet)
        {
            if (!IsColliderValid(other))
                continue;

            if (!other.Entity.HasAnyTag(tags))
                continue;

            var otherPolygon =
                other.GetTransformedPolygon();

            if (!polygon.Intersects(otherPolygon))
                continue;

            result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    #endregion

    #region Point Cast

    public bool PointCast(
        Vector2 point,
        out CollisionResult result)
    {
        result = new CollisionResult();

        _candidateList.Clear();

        _grid.QueryPoint(
            point,
            _candidateList);

        /*
         * QueryPoint touches exactly one grid cell.
         * A collider only occurs once in a given cell, so there is no need
         * to copy the list through a HashSet first.
         */
        for (var index = 0;
             index < _candidateList.Count;
             index++)
        {
            var other =
                _candidateList[index];

            if (!IsColliderValid(other))
                continue;

            var polygon =
                other.GetTransformedPolygon();

            if (!polygon.ContainsPoint(point))
                continue;

            result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    public bool PointCastByTag(
        Vector2 point,
        out CollisionResult result,
        IReadOnlyList<string> tags)
    {
        result = new CollisionResult();

        _candidateList.Clear();

        _grid.QueryPoint(
            point,
            _candidateList);

        for (var index = 0;
             index < _candidateList.Count;
             index++)
        {
            var other =
                _candidateList[index];

            if (!IsColliderValid(other))
                continue;

            if (!other.Entity.HasAnyTag(tags))
                continue;

            var polygon =
                other.GetTransformedPolygon();

            if (!polygon.ContainsPoint(point))
                continue;

            result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    #endregion

    #region Ray Cast

    public bool RayCast(
        Ray2D ray,
        out CollisionResult result)
    {
        result = new CollisionResult();

        _candidateList.Clear();

        _grid.QueryRay(
            ray.Start,
            ray.End,
            _candidateList);

        /*
         * A ray visits many cells, so the same collider can occur multiple
         * times. Deduplicate once here.
         */
        _candidateSet.Clear();

        for (var index = 0;
             index < _candidateList.Count;
             index++)
        {
            _candidateSet.Add(
                _candidateList[index]);
        }

        foreach (var other in _candidateSet)
        {
            if (!IsColliderValid(other))
                continue;

            var polygon =
                other.GetTransformedPolygon();

            if (!polygon.RayIntersects(
                    ray.Start,
                    ray.End,
                    out _))
            {
                continue;
            }

            result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    public bool RayCastByTag(
        Ray2D ray,
        out CollisionResult result,
        IReadOnlyList<string> tags)
    {
        result = new CollisionResult();

        _candidateList.Clear();

        _grid.QueryRay(
            ray.Start,
            ray.End,
            _candidateList);

        _candidateSet.Clear();

        for (var index = 0;
             index < _candidateList.Count;
             index++)
        {
            _candidateSet.Add(
                _candidateList[index]);
        }

        foreach (var other in _candidateSet)
        {
            if (!IsColliderValid(other))
                continue;

            if (!other.Entity.HasAnyTag(tags))
                continue;

            var polygon =
                other.GetTransformedPolygon();

            if (!polygon.RayIntersects(
                    ray.Start,
                    ray.End,
                    out _))
            {
                continue;
            }

            result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    #endregion

    #region Circle Cast

    public bool CircleCast(
        Vector2 center,
        float radius,
        out CollisionResult result,
        AABB? aabb = null)
    {
        result = new CollisionResult();

        if (radius <= 0f)
            return false;

        _candidateSet.Clear();

        aabb ??=
            new AABB
            {
                Min = new Vector2(
                    center.X - radius,
                    center.Y - radius),

                Max = new Vector2(
                    center.X + radius,
                    center.Y + radius)
            };

        _grid.QueryAABB(
            aabb.Value,
            _candidateSet);

        foreach (var other in _candidateSet)
        {
            if (!IsColliderValid(other))
                continue;

            var polygon =
                other.GetTransformedPolygon();

            if (!polygon.IntersectsCircle(
                    center,
                    radius))
            {
                continue;
            }

            result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    public bool CircleCastByTag(
        Vector2 center,
        float radius,
        out CollisionResult result,
        IReadOnlyList<string> tags)
    {
        result = new CollisionResult();

        if (radius <= 0f)
            return false;

        _candidateSet.Clear();

        var aabb =
            new AABB
            {
                Min = new Vector2(
                    center.X - radius,
                    center.Y - radius),

                Max = new Vector2(
                    center.X + radius,
                    center.Y + radius)
            };

        _grid.QueryAABB(
            aabb,
            _candidateSet);

        foreach (var other in _candidateSet)
        {
            if (!IsColliderValid(other))
                continue;

            if (!other.Entity.HasAnyTag(tags))
                continue;

            var polygon =
                other.GetTransformedPolygon();

            if (!polygon.IntersectsCircle(
                    center,
                    radius))
            {
                continue;
            }

            result.Collisions.Add(other);
        }

        return result.Collisions.Count > 0;
    }

    #endregion

    #region Helpers

    private static bool IsColliderValid(
        Collider collider)
    {
        return collider != null &&
               collider.Bounds != null &&
               collider.IsQueryable &&
               collider.Enabled &&
               collider.Entity?.Enabled == true;
    }

    private static bool HasAnyTag(
        Entity entity,
        HashSet<string> tags)
    {
        if (entity == null ||
            tags == null ||
            tags.Count == 0)
        {
            return false;
        }

        /*
         * Iterate the requested tags because InterestedTags is normally tiny.
         * HashSet.Contains on Entity.Tags remains O(1) average.
         */
        foreach (var tag in tags)
        {
            if (entity.Tags.Contains(tag))
                return true;
        }

        return false;
    }

    #endregion
}

public readonly struct CollisionResult()
{
    public List<Collider> Collisions { get; } = [];

    public int Count =>
        Collisions.Count;

    public Collider First =>
        Collisions[0];

    public Collider this[int key] =>
        Collisions[key];
}