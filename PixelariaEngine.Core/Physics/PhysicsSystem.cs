using System.Collections.Generic;
using PixelariaEngine.ECS;

namespace PixelariaEngine;

public class PhysicsSystem : Singleton<PhysicsSystem>
{
    public List<Collider> Colliders { get; } = [];

    public void RegisterCollider(Collider collider)
    {
        if(!Colliders.Contains(collider)) Colliders.Add(collider);
    }

    public bool Cast(Collider collider, out CollisionResult collisionResult)
    {
        collisionResult = new CollisionResult();
        
        foreach (var other in Colliders)
        {
            if(other == collider ) continue;

            var polygon = collider.GetTransformedPolygon();
            var otherPolygon = other.GetTransformedPolygon();
            
            if(!polygon.Intersects(otherPolygon)) continue;
            
            collisionResult.Collisions.Add(other);
        }
        
        return collisionResult.Collisions.Count > 0;
    }
}

public readonly struct CollisionResult()
{
    public List<Collider> Collisions { get; } = [];
    public int Count => Collisions.Count;

    public Collider this[int key] => Collisions[key];
}