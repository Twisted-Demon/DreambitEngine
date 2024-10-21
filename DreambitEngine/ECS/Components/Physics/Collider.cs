using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class Collider : Component
{
    private readonly List<Collider> _otherColliders = [];

    public List<string> InterestedIn = [];

    public Action<Collider> OnCollisionEnter;
    public Action<Collider> OnCollisionExit;
    public Action<Collider> OnCollisionStay;
    public Shape Bounds { get; set; } = null;
    public Polygon TransformedBounds => GetTransformedPolygon();
    public bool IsTrigger { get; set; } = false;


    public override void OnAddedToEntity()
    {
        PhysicsSystem.Instance.RegisterCollider(this);
    }

    public override void OnDestroyed()
    {
        PhysicsSystem.Instance.DeregisterCollider(this);
        OnCollisionEnter = null;
        OnCollisionStay = null;
        OnCollisionExit = null;
    }

    public override void OnRemovedFromEntity()
    {
        PhysicsSystem.Instance.DeregisterCollider(this);
    }

    public override void OnDisabled()
    {
        PhysicsSystem.Instance.DeregisterCollider(this);
    }

    public override void OnEnabled()
    {
        PhysicsSystem.Instance.RegisterCollider(this);
    }

    public override void OnUpdate()
    {
        if (IsTrigger && Bounds != null) CheckForTriggerCollisions();
    }

    private void CheckForTriggerCollisions()
    {
        CollisionResult collisionResults;

        if (InterestedIn.Count == 0)
            PhysicsSystem.Instance.ColliderCast(this, out collisionResults);
        else
            PhysicsSystem.Instance.ColliderCastByTag(this, out collisionResults, InterestedIn.ToArray());

        _otherColliders.RemoveAll(x => x.IsDestroyed);

        foreach (var other in collisionResults.Collisions)
        {
            //call enter only if we aren't already colliding
            if (!_otherColliders.Contains(other))
                OnCollisionEnter?.Invoke(other);

            //call stay even if we are a new collision
            OnCollisionStay?.Invoke(other);
        }

        //check for collisions that no longer exist
        foreach (var other in _otherColliders)
            if (!collisionResults.Collisions.Contains(other))
                OnCollisionExit?.Invoke(other);

        _otherColliders.Clear();
        _otherColliders.AddRange(collisionResults.Collisions);
    }

    public Polygon GetTransformedPolygon()
    {
        return Bounds.TransformPolygon(Transform);
    }

    public Polygon GetTransformedPolyWithDesiredPos(Vector3 desiredPos)
    {
        return Bounds.TransformWithDesiredPos(Transform, desiredPos);
    }

    public override void OnDebugDraw()
    {
        Core.SpriteBatch.DrawPolygon(TransformedBounds.Vertices, Color.White);
    }
}