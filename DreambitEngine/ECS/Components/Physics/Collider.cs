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
        DrawEditorOutline(context, new Color(82, 235, 140, 150), 1.5f);
    }

    /// <summary>Emphasizes the collider when its entity is selected in the editor.</summary>
    public override void OnEditorDrawGizmosSelected(IEditorGizmoContext context)
    {
        DrawEditorOutline(context, new Color(82, 235, 140, 150), 1.5f);
    }

    /// <summary>Renders polygon outline for debugging purposes.</summary>
    public override void OnDebugDraw()
    {
        Core.SpriteBatch.DrawPolygon(WorldPolygon2D.Vertices, Color.White,
            Scene.Instance.MainCamera.WorldUnitsPerScreenPixel);
    }

    private void DrawEditorOutline(IEditorGizmoContext context, Color color, float thickness)
    {
        var vertices = WorldPolygon2D.Vertices;
        if (vertices is null || vertices.Length < 2)
            return;

        for (var index = 0; index < vertices.Length; index++)
            context.Line(vertices[index], vertices[(index + 1) % vertices.Length], color, thickness);
    }

    #endregion

    #region Trigger Collision Checks

    /// <summary>
    ///     Performs trigger overlap checks and dispatches Enter/Exit/Stay events.
    ///     Uses tag filtering if <see cref="InterestedIn" /> is populated.
    /// </summary>
    private void CheckForTriggerCollisions()
    {
        CollisionResult hits;

        if (InterestedIn.Count == 0)
            PhysicsSystem.Instance.ColliderCast(this, out hits);
        else
            PhysicsSystem.Instance.ColliderCastByTag(this, out hits, InterestedIn);

        // Build current-frame overlap set
        _overlapsCurr.Clear();
        for (var i = 0; i < hits.Collisions.Count; i++)
            _overlapsCurr.Add(hits.Collisions[i]);

        // Enter = curr \ prev
        foreach (var c in _overlapsCurr)
            if (!_overlapsPrev.Contains(c))
                OnCollisionEnter?.Invoke(c);

        // Exit = prev \ curr
        foreach (var c in _overlapsPrev)
            if (!_overlapsCurr.Contains(c))
                OnCollisionExit?.Invoke(c);

        // Stay = curr ∩ prev  (here: fire for all curr each pass)
        foreach (var c in _overlapsCurr)
            OnCollisionStay?.Invoke(c);

        // Prepare for next frame
        _overlapsPrev.Clear();
        foreach (var c in _overlapsCurr)
            _overlapsPrev.Add(c);
    }

    #endregion

    #region Collision Casting Checks

    public virtual void ColliderCast(out CollisionResult hits)
    {
        PhysicsSystem.Instance.ColliderCast(this, out hits);
    }

    public virtual void ColliderCastByTags(out CollisionResult hits, params string[] tags)
    {
        PhysicsSystem.Instance.ColliderCastByTag(this, out hits, tags);
    }

    #endregion

    #region Broadphase / Spatial Hash Participation

    /// <summary>
    ///     Notifies the physics system when the collider's position changes,
    ///     allowing broadphase structures (e.g., spatial hash) to stay current.
    /// </summary>
    internal void RefreshSpatialHash()
    {
        if (!_isAttached || !Enabled || !IsQueryable || Bounds == null)
        {
            DeregisterFromPhysics();
            return;
        }

        var previousAabb = AABB;
        SetAabb();

        if (!_isRegistered)
        {
            PhysicsSystem.Instance.RegisterCollider(this);
            _isRegistered = true;
            return;
        }

        if (!AabbEquals(previousAabb, AABB))
            PhysicsSystem.Instance.Touch(this);
    }

    //this is to be overridden by circle collider and capsule collider
    protected virtual void SetAabb()
    {
        AABB = WorldPolygon2D.ComputeAabb();
    }

    #endregion

    #region Flags & Configuration

    /// <summary>When true, collider acts as a trigger (no physical response, events only).</summary>
    [DreambitSerialize]
    public bool IsTrigger { get; set; } = false;

    /// <summary>When true, suppresses trigger event generation.</summary>
    [DreambitSerialize]
    public bool IsSilent { get; set; } = false;

    /// <summary>When false, collider is ignored by spatial queries / broadphase.</summary>
    [DreambitSerialize]
    public bool IsQueryable
    {
        get => _isQueryable;
        set
        {
            if (_isQueryable == value) return;
            _isQueryable = value;
            RefreshSpatialHash();
        }
    }

    /// <summary>Optional filter: limit trigger checks to these tags. Empty = all.</summary>
    [DreambitSerialize] public List<string> InterestedIn = [];

    #endregion

    #region Events / Callbacks

    /// <summary>Raised when this trigger begins overlapping another collider.</summary>
    public Action<Collider> OnCollisionEnter;

    /// <summary>Raised while this trigger stays overlapping another collider (fired each check).</summary>
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
            if (ReferenceEquals(_bounds, value)) return;
            _bounds = value;
            RefreshSpatialHash();
        }
    }

    public AABB AABB { get; set; }

    /// <summary>World-space polygon computed from <see cref="Bounds" /> and current transform.</summary>
    public Polygon2D WorldPolygon2D => GetTransformedPolygon();

    #endregion

    #region Internal State

    private Shape2D _bounds;
    private bool _isAttached;
    private bool _isQueryable = true;
    private bool _isRegistered;

    // Sets used to detect enter/exit vs. stay across frames
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
    }

    /// <summary>Deregister when removed from entity.</summary>
    public override void OnRemovedFromEntity()
    {
        _isAttached = false;
        DeregisterFromPhysics();
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
        if (IsTrigger && Bounds != null && !IsSilent) CheckForTriggerCollisions();
    }

    /// <summary>Physics-step update; maintains spatial hash participation.</summary>
    public override void OnPhysicsUpdate()
    {
        RefreshSpatialHash();
    }

    #endregion

    #region Helpers

    /// <summary>Returns world-space polygon transformed from current <see cref="Bounds" />.</summary>
    public Polygon2D GetTransformedPolygon()
    {
        if (Bounds == null)
            return default;

        return Bounds.TransformPolygon(Transform);
    }

    /// <summary>
    ///     Returns world-space polygon transformed as if the collider were at <paramref name="desiredPos" />.
    /// </summary>
    public Polygon2D GetTransformedPolyWithDesiredPos(Vector3 desiredPos)
    {
        if (Bounds == null)
            return default;

        return Bounds.TransformWithDesiredPos(Transform, desiredPos);
    }

    private void DeregisterFromPhysics()
    {
        if (!_isRegistered) return;
        PhysicsSystem.Instance.DeregisterCollider(this);
        _isRegistered = false;
    }

    private static bool AabbEquals(AABB left, AABB right)
    {
        return left.Min == right.Min && left.Max == right.Max;
    }

    #endregion
}
