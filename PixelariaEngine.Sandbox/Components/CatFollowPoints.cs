using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox;

public class CatFollowPoints : Component
{
    // the desired direction of where we want arthas / darien to sit
    // relative to the player
    private Vector3 _arthasPositionDir = Vector3.Zero;
    private Vector3 _darionPositionDir = Vector3.Zero;

    public float Offset = 15.0f; // set 10px away from player
    
    // the actual position, after accounting for collisions
    public Vector3 ArthasPos;
    public Vector3 DarionPos;

    public override void OnCreated()
    {
        // Arthas to sit on the right of the player
        _arthasPositionDir = new Vector3(1, 0, 0);
        
        // Darion to sit on the left of the player
        _darionPositionDir = new Vector3(-1, 0, 0);
    }

    public override void OnUpdate()
    {
        // Check if intended spot for Darion is availble
        var playerPos = Transform.WorldPosition;
        var intendedDarionPos = playerPos + _darionPositionDir * Offset;
        
        var ray = new Ray2D(playerPos.ToVector2(), intendedDarionPos.ToVector2());

        if (PhysicsSystem.Instance.RayCastByTag(ray, out _, "wall"))
        {
            intendedDarionPos = playerPos;
        }
        
        DarionPos = intendedDarionPos;
        
        var intendedArthasPos = playerPos + _arthasPositionDir * Offset;
        
        ray = new Ray2D(playerPos.ToVector2(), intendedArthasPos.ToVector2());
        
        if (PhysicsSystem.Instance.RayCastByTag(ray, out _, "wall"))
        {
            intendedArthasPos = playerPos;
        }
        
        ArthasPos = intendedArthasPos;
    }
}