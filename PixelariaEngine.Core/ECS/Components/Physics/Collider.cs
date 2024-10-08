using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PixelariaEngine.ECS;

public class Collider : Component<Collider>
{
    public Shape Bounds { get; set; }
    public bool isTrigger { get; set; } = false;
    private readonly List<Collider> _otherColliders = [];

    public Action<Collider> OnCollisionEnter;
    public Action<Collider> OnCollisionStay;
    public Action<Collider> OnCollisionExit;


    public override void OnCreated()
    {
        PhysicsSystem.Instance.RegisterCollider(this);
    }

    public override void OnRemovedFromEntity()
    {
        OnCollisionEnter = null;
        OnCollisionStay = null;
        OnCollisionExit = null;
    }

    public override void OnUpdate()
    {
        if(isTrigger) CheckForTriggerCollisions();
    }

    private void CheckForTriggerCollisions()
    {
        PhysicsSystem.Instance.Cast(this, out var collisionResults);
        
        foreach (var other in collisionResults.Collisions)
        {
            //call enter only if we aren't already colliding
            if(!_otherColliders.Contains(other))
                OnCollisionEnter?.Invoke(other);
            
            //call stay even if we are a new collision
            OnCollisionStay?.Invoke(other);
        }
        
        //check for collisions that no longer exist
        foreach (var other in _otherColliders)
        {
            if(!collisionResults.Collisions.Contains(other))
                OnCollisionExit?.Invoke(other);
        }
        
        _otherColliders.Clear();
        _otherColliders.AddRange(collisionResults.Collisions);
        
    }

    public Polygon GetTransformedPolygon()
    {
        return Bounds.TransformPolygon(Transform.GetTransformationMatrix());
    }

    public override void OnDebugDraw()
    {
        var tranformedShape = GetTransformedPolygon();

        for (int i = 0; i < tranformedShape.Vertices.Length; i++)
        {
            var current = tranformedShape.Vertices[i];
            var next = tranformedShape.Vertices[(i + 1) % tranformedShape.Vertices.Length];

            Core.SpriteBatch.DrawLine(current, next, Color.White, 1f);
        }
    }
}