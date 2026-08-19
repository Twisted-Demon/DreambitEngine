using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

/// <summary>
///     Physics collider component. Can act as a trigger, participate in spatial queries,
///     and raise collision callbacks (enter/stay/exit). Rendering of bounds is available in debug.
/// </summary>
[BlueprintType(nameof(Collider))]
public class Collider : Component
{
    #region Debug

    /// <summary>Draws the collider outline in editor-hosted scene and blueprint views.</summary>
    public override void OnEditorDrawGizmos(IEditorGizmoContext context)
    {
        //DrawEditorOutline(context, new Color(82, 235, 140, 150), 1.5f);
    }

    /// <summary>Emphasizes the collider when its entity is selected in the editor.</summary>
    public override void OnEditorDrawGizmosSelected(IEditorGizmoContext context)
    {
        DrawEditorOutline(
            context,
            new Color(82, 235, 140, 150),
            1.5f);
    }

    /// <summary>Renders polygon outline for debugging purposes.</summary>
    public override void OnDebugDraw()
    {
        Core.SpriteBatch.DrawPolygon(
            WorldPolygon2D.Vertices,
            Color.White,
            Scene.Instance.MainCamera.WorldUnitsPerScreenPixel);
    }

    private void DrawEditorOutline(
        IEditorGizmoContext context,
        Color color,
        float thickness)
    {
        var vertices = WorldPolygon2D.Vertices;

        if (vertices is null || vertices.Length < 2)
            return;

        for (var index = 0; index < vertices.Length; index++)
        {
            context.Line(
                vertices[index],
                vertices[(index + 1) % vertices.Length],
                color,
                thickness);
        }
    }

    #endregion

    #region Trigger Collision Checks

    /// <summary>
    ///     Performs trigger overlap checks and dispatches Enter/Exit/Stay events.
    ///     Uses tag filtering if <see cref="InterestedIn" /> is populated.
    /// </summary>
    private void CheckForTriggerCollisions()
    {
        // Fill the reusable overlap set directly.
        // This avoids allocating a CollisionResult + List every trigger update.
        if (InterestedIn.Count == 0)
        {
            PhysicsSystem.Instance.CollectColliderOverlaps(
                this,
                _overlapsCurr);
        }
        else
        {
            PhysicsSystem.Instance.CollectColliderOverlapsByTag(
                this,
                _overlapsCurr,
                InterestedIn);
        }

        // Enter = curr \ prev
        foreach (var collider in _overlapsCurr)
        {
            if (!_overlapsPrev.Contains(collider))
                OnCollisionEnter?.Invoke(collider);
        }

        // Exit = prev \ curr
        foreach (var collider in _overlapsPrev)
        {
            if (!_overlapsCurr.Contains(collider))
                OnCollisionExit?.Invoke(collider);
        }

        // Stay = everything currently overlapping.
        foreach (var collider in _overlapsCurr)
            OnCollisionStay?.Invoke(collider);

        // Swap conceptually by copying into the already-allocated set.
        _overlapsPrev.Clear();

        foreach (var collider in _overlapsCurr)
            _overlapsPrev.Add(collider);
    }

    #endregion

    #region Collision Casting Checks

    public virtual void ColliderCast(out CollisionResult hits)
    {
        PhysicsSystem.Instance.ColliderCast(
            this,
            out hits);
    }

    public virtual void ColliderCastByTags(
        out CollisionResult hits,
        params string[] tags)
    {
        PhysicsSystem.Instance.ColliderCastByTag(
            this,
            out hits,
            tags);
    }

    #endregion

    #region Broadphase / Spatial Hash Participation

    /// <summary>
    ///     Notifies the physics system when the collider's transform/shape changes.
    ///
    ///     World polygon storage is cached and reused. Static colliders therefore
    ///     do not allocate a new vertex array every physics step.
    /// </summary>
    internal void RefreshSpatialHash()
    {
        if (!_isAttached ||
            !Enabled ||
            !IsQueryable ||
            Bounds == null)
        {
            DeregisterFromPhysics();
            return;
        }

        var previousAabb = AABB;

        var geometryChanged =
            EnsureWorldGeometry();

        /*
         * Preserve the existing contract that SetAabb() is evaluated during
         * collider maintenance, including for future Collider subclasses that
         * override it.
         *
         * If geometry changed, EnsureWorldGeometry already called SetAabb().
         */
        if (!geometryChanged)
            SetAabb();

        var aabbChanged =
            !AabbEquals(previousAabb, AABB);

        if (!_isRegistered)
        {
            PhysicsSystem.Instance.RegisterCollider(this);
            _isRegistered = true;
            return;
        }

        /*
         * SpatialHash.InsertOrUpdate() already early-outs when the occupied
         * CellRange did not change, so touching here is cheap even if the
         * polygon rotated inside the same cells.
         */
        if (geometryChanged || aabbChanged)
            PhysicsSystem.Instance.Touch(this);
    }

    /// <summary>
    ///     Calculates the current broadphase bounds.
    ///     Override for specialized collider types when appropriate.
    /// </summary>
    protected virtual void SetAabb()
    {
        if (_cachedWorldPolygon.Vertices is not { Length: > 0 })
        {
            AABB = default;
            return;
        }

        AABB = _cachedWorldPolygon.ComputeAabb();
    }

    /// <summary>
    /// Ensures the reusable world-space polygon matches the current Bounds and Transform.
    /// Returns true only when the cached geometry was rebuilt.
    /// </summary>
    private bool EnsureWorldGeometry()
    {
        if (Bounds == null)
        {
            _cachedWorldPolygon = default;
            _cachedBounds = null;
            _hasCachedWorldGeometry = false;
            _worldGeometryDirty = true;
            AABB = default;

            return false;
        }

        var localVertices = Bounds.GetVertices();

        if (localVertices is not { Length: >= 3 })
        {
            _cachedWorldPolygon = default;
            _cachedBounds = Bounds;
            _hasCachedWorldGeometry = false;
            _worldGeometryDirty = true;
            AABB = default;

            return false;
        }

        /*
         * WorldMatrix is deliberately evaluated once per collider check,
         * not once per polygon vertex.
         */
        var worldMatrix =
            Transform.WorldMatrix;

        /*
         * GetVertices() currently exposes the backing array publicly.
         * The hash keeps caching compatible with callers that modify those
         * vertices in-place rather than assigning a new Bounds object.
         */
        var shapeHash =
            ComputeShapeHash(localVertices);

        var needsRebuild =
            !_hasCachedWorldGeometry ||
            _worldGeometryDirty ||
            !ReferenceEquals(_cachedBounds, Bounds) ||
            !_cachedWorldMatrix.Equals(worldMatrix) ||
            _cachedShapeHash != shapeHash;

        if (!needsRebuild)
            return false;

        var worldVertices =
            _cachedWorldPolygon.Vertices;

        if (worldVertices == null ||
            worldVertices.Length != localVertices.Length)
        {
            worldVertices =
                new Vector2[localVertices.Length];

            _cachedWorldPolygon =
                new Polygon2D
                {
                    Vertices = worldVertices
                };
        }

        /*
         * Reuse the same world-space vertex array.
         *
         * This is the important allocation fix:
         * moving colliders update their existing vertices instead of creating
         * a new Vector2[] on every transform.
         */
        for (var index = 0;
             index < localVertices.Length;
             index++)
        {
            var point =
                new Vector3(
                    localVertices[index],
                    0f);

            var transformed =
                Vector3.Transform(
                    point,
                    worldMatrix);

            worldVertices[index] =
                new Vector2(
                    transformed.X,
                    transformed.Y);
        }

        _cachedBounds = Bounds;
        _cachedWorldMatrix = worldMatrix;
        _cachedShapeHash = shapeHash;
        _hasCachedWorldGeometry = true;
        _worldGeometryDirty = false;

        try
        {
            /*
             * Metadata has already been committed above so an overridden
             * SetAabb() can safely access WorldPolygon2D without causing
             * recursive geometry rebuilding.
             */
            SetAabb();
        }
        catch
        {
            _worldGeometryDirty = true;
            throw;
        }

        return true;
    }

    private void InvalidateWorldGeometry()
    {
        _worldGeometryDirty = true;
    }

    private static int ComputeShapeHash(
        Vector2[] vertices)
    {
        unchecked
        {
            var hash = 17;

            hash =
                hash * 31 +
                vertices.Length;

            for (var index = 0;
                 index < vertices.Length;
                 index++)
            {
                hash =
                    hash * 31 +
                    BitConverter.SingleToInt32Bits(
                        vertices[index].X);

                hash =
                    hash * 31 +
                    BitConverter.SingleToInt32Bits(
                        vertices[index].Y);
            }

            return hash;
        }
    }

    #endregion

    #region Flags & Configuration

    /// <summary>When true, collider acts as a trigger (no physical response, events only).</summary>
    [DreambitSerialize]
    public bool IsTrigger { get; set; }

    /// <summary>When true, suppresses trigger event generation.</summary>
    [DreambitSerialize]
    public bool IsSilent { get; set; }

    /// <summary>When false, collider is ignored by spatial queries / broadphase.</summary>
    [DreambitSerialize]
    public bool IsQueryable
    {
        get => _isQueryable;

        set
        {
            if (_isQueryable == value)
                return;

            _isQueryable = value;
            RefreshSpatialHash();
        }
    }

    /// <summary>Optional filter: limit trigger checks to these tags. Empty = all.</summary>
    [DreambitSerialize]
    public List<string> InterestedIn = [];

    #endregion

    #region Events / Callbacks

    /// <summary>Raised when this trigger begins overlapping another collider.</summary>
    public Action<Collider> OnCollisionEnter;

    /// <summary>Raised while this trigger stays overlapping another collider.</summary>
    public Action<Collider> OnCollisionStay;

    /// <summary>Raised when this trigger stops overlapping another collider.</summary>
    public Action<Collider> OnCollisionExit;

    #endregion

    #region Bounds & Shape

    /// <summary>Local-space shape used for collision/trigger checks.</summary>
    [DreambitSerialize]
    public Shape2D Bounds
    {
        get => _bounds;

        set
        {
            if (ReferenceEquals(_bounds, value))
                return;

            _bounds = value;

            InvalidateWorldGeometry();
            RefreshSpatialHash();
        }
    }

    public AABB AABB { get; set; }

    /// <summary>
    /// World-space polygon computed from Bounds and the current transform.
    /// The returned polygon references reusable collider-owned storage.
    /// </summary>
    public Polygon2D WorldPolygon2D =>
        GetTransformedPolygon();

    #endregion

    #region Internal State

    private Shape2D _bounds;

    private bool _isAttached;
    private bool _isQueryable = true;
    private bool _isRegistered;

    /*
     * Cached world geometry.
     *
     * Vector storage is reused across physics steps. The polygon is rebuilt
     * only when the transform or authored shape actually changes.
     */
    private Polygon2D _cachedWorldPolygon;
    private Matrix _cachedWorldMatrix;
    private Shape2D _cachedBounds;
    private int _cachedShapeHash;
    private bool _hasCachedWorldGeometry;
    private bool _worldGeometryDirty = true;

    // Reused sets for trigger enter/stay/exit detection.
    private readonly HashSet<Collider> _overlapsPrev = [];
    private readonly HashSet<Collider> _overlapsCurr = [];

    #endregion

    #region Lifecycle Overrides

    /// <summary>Registers this collider with the physics system.</summary>
    public override void OnAddedToEntity()
    {
        _isAttached = true;

        RefreshSpatialHash();

        Transform.CaptureLastWorldPosition();
    }

    /// <summary>Ensures deregistration and clears callbacks on destruction.</summary>
    public override void OnDestroyed()
    {
        _isAttached = false;

        DeregisterFromPhysics();

        OnCollisionEnter = null;
        OnCollisionStay = null;
        OnCollisionExit = null;

        _overlapsPrev.Clear();
        _overlapsCurr.Clear();

        InvalidateWorldGeometry();
    }

    /// <summary>Deregister when removed from entity.</summary>
    public override void OnRemovedFromEntity()
    {
        _isAttached = false;

        DeregisterFromPhysics();

        _overlapsPrev.Clear();
        _overlapsCurr.Clear();
    }

    /// <summary>Deregister while disabled.</summary>
    public override void OnDisabled()
    {
        DeregisterFromPhysics();
    }

    /// <summary>Re-register when enabled.</summary>
    public override void OnEnabled()
    {
        RefreshSpatialHash();

        Transform.CaptureLastWorldPosition();
    }

    /// <summary>Per-frame update; drives trigger collision checks when enabled.</summary>
    public override void OnUpdate()
    {
        if (IsTrigger &&
            Bounds != null &&
            !IsSilent)
        {
            CheckForTriggerCollisions();
        }
    }

    /// <summary>Physics-step update; maintains spatial hash participation.</summary>
    public override void OnPhysicsUpdate()
    {
        RefreshSpatialHash();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Returns the collider's current world polygon using reusable cached storage.
    /// </summary>
    public Polygon2D GetTransformedPolygon()
    {
        if (Bounds == null)
            return default;

        EnsureWorldGeometry();

        return _cachedWorldPolygon;
    }

    /// <summary>
    /// Returns world-space polygon transformed as if the collider were at desiredPos.
    /// Intended for speculative queries; unlike the regular world polygon this creates
    /// temporary polygon storage.
    /// </summary>
    public Polygon2D GetTransformedPolyWithDesiredPos(
        Vector3 desiredPos)
    {
        if (Bounds == null)
            return default;

        return Bounds.TransformWithDesiredPos(
            Transform,
            desiredPos);
    }

    private void DeregisterFromPhysics()
    {
        if (!_isRegistered)
            return;

        PhysicsSystem.Instance.DeregisterCollider(this);
        _isRegistered = false;
    }

    private static bool AabbEquals(
        AABB left,
        AABB right)
    {
        return left.Min == right.Min &&
               left.Max == right.Max;
    }

    #endregion
}