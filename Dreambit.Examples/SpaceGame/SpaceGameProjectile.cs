using System.Collections.Generic;
using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.Examples.SpaceGame;

[Require(typeof(Mover))]
public class SpaceGameProjectile : Component<SpaceGameProjectile>
{
    public float Velocity;
    public Vector3 Direction;

    private Mover _mover;
    private Collider _collider;

    private string[] _interestedIn;

    public override void OnCreated()
    {
        _mover = Entity.GetComponent<Mover>();
    }

    public override void OnUpdate()
    {
        _mover.Velocity = Direction * Velocity;
    }

    public override void OnPhysicsUpdate()
    {
        if (_collider is null) return;

        var collided = CheckForCollision(out var result);

        if (!collided) return;
        foreach (var other in result.Collisions)
        {
            Entity.Destroy(other.Entity);
        }

        Entity.Destroy(Entity);
    }

    public void SetCollider(Collider collider)
    {
        _collider = collider;
    }

    public void SetInterestedTags(string[] tags)
    {
        _interestedIn = tags;
    }
    

    private bool CheckForCollision(out CollisionResult result)
    {
        return _interestedIn is null ? 
            PhysicsSystem.Instance.ColliderCast(_collider, out result) : 
            PhysicsSystem.Instance.ColliderCastByTag(_collider, out result, _interestedIn);
    }

    public static SpaceGameProjectile CreatePlayerBeam(Entity player)
    {
        var projectile = ECS.Entity.Create("player_beam", createAt: player.Transform.Position)
            .AttachComponent<SpaceGameProjectile>();

        var drawer = projectile.Entity.AttachComponent<SpriteDrawer>();
        drawer.SetSprite(SpriteSheet.Create(1, 1, "SpaceGame/Textures/Projectiles/player_beam").Frames[0]);
        drawer.DrawLayer = -1;

        projectile.Velocity = 200.0f;
        projectile.Direction = Vector3.Down;
        projectile.SetInterestedTags(["enemy"]);

        Vector2[] polyPoints =
        [
            new Vector2(-4, -3),
            new Vector2(5, -3),
            new Vector2(5, 5),
            new Vector2(-4, 5)
        ];

        var collider = projectile.Entity.AttachComponent<PolyShapeCollider>();
        collider.SetShape(PolyShape2D.Create(polyPoints));
        projectile.SetCollider(collider);
        
        return projectile;
    }

    public static SpaceGameProjectile CreateAlanProjectile(Entity alan)
    {
        var projectile = ECS.Entity.Create("alan_projectile", createAt: alan.Transform.Position)
            .AttachComponent<SpaceGameProjectile>();

        var anim = projectile.Entity.AttachComponent<SpriteAnimator>();
        anim.SetAnimation("SpaceGame/Animations/alan_projectile");
        anim.Play();

        var drawer = projectile.Entity.GetComponent<SpriteDrawer>();
        drawer.DrawLayer = -1;
        
        projectile.Velocity = 80.0f;
        projectile.Direction = Vector3.Up;
        projectile.SetInterestedTags(["player"]);
        
        Vector2[] polyPoints =
        [
            new Vector2(-2, -2),
            new Vector2(3, -2),
            new Vector2(3, 3),
            new Vector2(-2, 3)
        ];
        
        var collider = projectile.Entity.AttachComponent<PolyShapeCollider>();
        collider.SetShape(PolyShape2D.Create(polyPoints));
        projectile.SetCollider(collider);
        
        return projectile;
    }
}