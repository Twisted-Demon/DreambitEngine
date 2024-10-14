using LDtk;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox;

public partial class Managers : LDtkEntity<Managers>
{
    protected override void SetUp(LDtkLevel level)
    {
        var collisionGrid = LDtkManager.Instance.CurrentLevel.GetIntGrid("CollisionGrid");
        var entity = CreateEntity(this);
        entity.AttachComponent<AStarGrid>().InitializeGrid(collisionGrid);
        entity.Name = "managers";
    }
}