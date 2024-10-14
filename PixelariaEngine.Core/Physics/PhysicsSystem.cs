using System.Collections.Generic;
using PixelariaEngine.ECS;

namespace PixelariaEngine;

public class PhysicsSystem : Singleton<PhysicsSystem>
{
    private List<Collider> Colliders { get; } = [];
    private Dictionary<string, List<Collider>> CollidersByTag { get; set; } = [];

    public void RegisterCollider(Collider collider)
    {
        if(!Colliders.Contains(collider)) Colliders.Add(collider);

        var tags = collider.Entity.Tags;

        foreach (var tag in tags)
        {
            if (!CollidersByTag.ContainsKey(tag)) 
                CollidersByTag.Add(tag, []);

            if (CollidersByTag[tag].Contains(collider))
                continue;
            
            CollidersByTag[tag].Add(collider);
        }
    }

    public void DeregisterCollider(Collider collider)
    {
        Colliders.Remove(collider);

        foreach (var tag in CollidersByTag.Keys)
        {
            CollidersByTag[tag].Remove(collider);
        }
    }

    public void CleanUp()
    {
        Colliders.Clear();
    }

    public bool PolygonCast(Collider @this, out CollisionResult collisionResult)
    {
        collisionResult = new CollisionResult();
        
        foreach (var other in Colliders)
        {
            if (@this == null) continue;
            if(other == @this) continue;
            if (other == null) continue;
            
            if (!other.Enabled || !other.Entity.Enabled) continue;

            var polygon = @this.GetTransformedPolygon();
            var otherPolygon = other.GetTransformedPolygon();
            
            if(!polygon.Intersects(otherPolygon)) continue;
            
            collisionResult.Collisions.Add(other);
        }
        
        return collisionResult.Collisions.Count > 0;
    }

    public bool PolygonCastByTag(Collider @this, out CollisionResult collisionResult, params string[] tags)
    {
        collisionResult = new CollisionResult();
        
        foreach (var tag in tags)
        {
            if(!CollidersByTag.TryGetValue(tag, out var value)) 
                
                continue;
            foreach (var other in value)
            {
                if (@this == null) continue;
                if(other == @this) continue;
                if (other == null) continue;
            
                if (!other.Enabled || !other.Entity.Enabled) continue;

                var polygon = @this.GetTransformedPolygon();
                var otherPolygon = other.GetTransformedPolygon();
            
                if(!polygon.Intersects(otherPolygon)) continue;
            
                collisionResult.Collisions.Add(other);
            }
        }
        
        return collisionResult.Collisions.Count > 0;
    }

    public bool RayCast(Ray2D ray, out CollisionResult collisionResult)
    {
        collisionResult = new CollisionResult();

        foreach (var other in Colliders)
        {
            if (other == null) continue;
            
            if (!other.Enabled || !other.Entity.Enabled) continue;

            var otherPoly = other.GetTransformedPolygon();
            if (!otherPoly.RayIntersects(ray.Start, ray.End, out _)) continue;
            
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