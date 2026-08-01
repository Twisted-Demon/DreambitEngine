using System;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

[BlueprintType(nameof(TileMover))]
public class TileMover : Component
{
    private readonly Logger<TileMover> _logger = new();
    private AStarGrid _astarGrid;

    public Vector3 Velocity;

    public override void OnAddedToEntity()
    {
        _astarGrid = Entity.FindByName("managers").GetComponent<AStarGrid>();
    }

    public override void OnUpdate()
    {
        if (_astarGrid == null)
        {
            throw new ArgumentNullException(nameof(_astarGrid));
        }

        var desiredMovement = Velocity * Time.DeltaTime;

        if (_astarGrid.IsWalkable(Transform.WorldPosition2D + desiredMovement.ToVector2()))
            Transform.Position += desiredMovement;
    }
}