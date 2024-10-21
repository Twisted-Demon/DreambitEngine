using System.Collections.Generic;
using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.Sandbox;

public class TileWallMover : Component
{
    private readonly Logger<TileWallMover> _logger = new();
    
    public Vector3 Velocity;
    public AStarGrid AstarGrid;
    public string[] InterestedTags;
    public Collider Collider;

    public override void OnAddedToEntity()
    {
    }

    public override void OnUpdate()
    {
        var desiredMovement = Velocity * Time.DeltaTime;

        if (desiredMovement.Length() == 0)
            return;

        if (AstarGrid != null)
        {
            if (!AstarGrid.IsWalkable(Transform.WorldPosToVec2 + desiredMovement.ToVector2()))
            {
                desiredMovement = Vector3.Zero;
            }
        }

        if (Collider != null)
        {
            var thisPolygon = Collider.GetTransformedPolyWithDesiredPos(desiredMovement);

            if (PhysicsSystem.Instance.PolygonCastByTag(thisPolygon, out var _, InterestedTags))
            {
                desiredMovement = Vector3.Zero;
            }
        }
        
        Transform.Position += desiredMovement;
    }

    public override void OnDebugDraw()
    {
    }
}