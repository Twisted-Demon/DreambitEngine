using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.BriGame.Components.Galaga;

[Require(typeof(BulletDrawer), typeof(Mover))]
public class ProjectileComponent : Component<ProjectileComponent>
{
    public BulletDrawer Drawer;
    public Mover Mover;
    
    public Guid OwnerId;

    public Vector3 Direction;
    public float Velocity;

    public Color Tint;
    public int Size = 4;

    public float LifeSpan = 5.0f;
    public float LifeTick = 0.0f;
    
    public HashSet<string> InterestedInTags = [];

    public override void OnAddedToEntity()
    {
        Drawer =  Entity.GetComponent<BulletDrawer>();
        Drawer.Tint = Tint;
        Drawer.Size = Size;
        
        Mover = Entity.GetComponent<Mover>();
    }

    public override void OnUpdate()
    {
        Mover.Velocity = Direction * Velocity;

        if (PhysicsSystem.Instance.PointCastByTag(Transform.WorldPosToVec2, out var results, "enemy"))
        {
            foreach (var other in results.Collisions)
            {
                Entity.Destroy(other.Entity);
            }
            Entity.Destroy(Entity);
        }
        
        LifeTick += Time.DeltaTime;
        if (LifeTick >= LifeSpan)
            Entity.Destroy(Entity);
    }

    public override void OnDestroyed()
    {
        Logger.Trace("Projectile destroyed");
    }

    public static ProjectileComponent Create(Vector3 position, Vector3 direction, float velocity, int size, Color tint, HashSet<string> tags, Guid ownerId)
    {
        var projectile = ECS.Entity.Create("projectile", tags: tags, createAt: position)
            .AttachComponent<ProjectileComponent>();
        
        projectile.Direction = direction;
        projectile.Velocity =  velocity;
        projectile.OwnerId = ownerId;
        projectile.Tint =  tint;
        projectile.Size = size;
        
        return projectile;
    }
}