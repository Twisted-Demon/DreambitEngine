using Microsoft.Xna.Framework;

namespace PixelariaEngine.ECS;

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
            _logger.Warn("No A* grid found");
            return;
        }

        var desiredMovement = Velocity * Time.DeltaTime;

        if (_astarGrid.IsWalkable(Transform.WorldPosToVec2 + desiredMovement.ToVector2()))
            Transform.Position += desiredMovement;
    }
}