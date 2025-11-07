using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

/// <summary>
///     Physics collider component. Can act as a trigger, participate in spatial queries,
///     and raise collision callbacks (enter/stay/exit). Rendering of bounds is available in debug.
/// </summary>
public class Collider : Component
{
    
    #region Flags & Configuration

    /// <summary>When true, collider acts as a trigger (no physical response, events only).</summary>
    public bool IsTrigger { get; set; } = false;

    /// <summary>When true, suppresses trigger event generation.</summary>
    public bool IsSilent { get; set; } = false;

    /// <summary>When false, collider is ignored by spatial queries / broadphase.</summary>
    public bool IsQueryable { get; set; } = true;

    /// <summary>Optional filter: limit trigger checks to these tags. Empty = all.</summary>
    public List<string> InterestedIn = [];

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
    public Shape Bounds { get; set; } = null;

    /// <summary>World-space polygon computed from <see cref="Bounds" /> and current transform.</summary>
    public Polygon WorldPolygon => GetTransformedPolygon();

    #endregion
    
    #region Debug

    /// <summary>Renders polygon outline for debugging purposes.</summary>
    public override void OnDebugDraw()
    {
        Core.SpriteBatch.DrawPolygon(WorldPolygon.Vertices, Color.White);
    }

    #endregion

    #region Internal State

    private Vector3 _lastPosition = Vector3.Zero;
    private Vector3 _currentPosition = Vector3.Zero;

    // Sets used to detect enter/exit vs. stay across frames
    private readonly HashSet<Collider> _overlapsPrev = [];
    private readonly HashSet<Collider> _overlapsCurr = [];

    #endregion

    #region Lifecycle Overrides

    /// <summary>Registers this collider with the physics system.</summary>
    public override void OnAddedToEntity()
    {
        PhysicsSystem.Instance.RegisterCollider(this);
    }

    /// <summary>Ensures deregistration and clears callbacks on destruction.</summary>
    public override void OnDestroyed()
    {
        PhysicsSystem.Instance.DeregisterCollider(this);
        OnCollisionEnter = null;
        OnCollisionStay = null;
        OnCollisionExit = null;
    }

    /// <summary>Deregister when removed from entity.</summary>
    public override void OnRemovedFromEntity()
    {
        PhysicsSystem.Instance.DeregisterCollider(this);
    }

    /// <summary>Deregister while disabled.</summary>
    public override void OnDisabled()
    {
        PhysicsSystem.Instance.DeregisterCollider(this);
    }

    /// <summary>Re-register when enabled.</summary>
    public override void OnEnabled()
    {
        PhysicsSystem.Instance.RegisterCollider(this);
    }

    /// <summary>Per-frame update; drives trigger collision checks when enabled.</summary>
    public override void OnUpdate()
    {
        if (IsTrigger && Bounds != null && !IsSilent) CheckForTriggerCollisions();
    }

    /// <summary>Physics-step update; maintains spatial hash participation.</summary>
    public override void OnPhysicsUpdate()
    {
        UpdateInSpatialHash();
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
            PhysicsSystem.Instance.ColliderCastByTag(this, out hits, InterestedIn.ToArray());

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
    
    #region Broadphase / Spatial Hash Participation

    /// <summary>
    ///     Notifies the physics system when the collider's position changes,
    ///     allowing broadphase structures (e.g., spatial hash) to stay current.
    /// </summary>
    private void UpdateInSpatialHash()
    {
        if (!IsQueryable) return;

        _currentPosition = Transform.Position;

        if (_lastPosition != _currentPosition)
            PhysicsSystem.Instance.Touch(this);

        _lastPosition = _currentPosition;
    }

    #endregion

    #region Helpers

    /// <summary>Returns world-space polygon transformed from current <see cref="Bounds" />.</summary>
    public Polygon GetTransformedPolygon()
    {
        return Bounds.TransformPolygon(Transform);
    }

    /// <summary>
    ///     Returns world-space polygon transformed as if the collider were at <paramref name="desiredPos" />.
    /// </summary>
    public Polygon GetTransformedPolyWithDesiredPos(Vector3 desiredPos)
    {
        return Bounds.TransformWithDesiredPos(Transform, desiredPos);
    }

    #endregion
}