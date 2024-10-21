using Dreambit.ECS;
using LDtk;

namespace Dreambit.Sandbox;

public partial class Managers : LDtkEntity<Managers>
{
    protected override void SetUp(LDtkLevel level)
    {
        var collisionGrid = LDtkManager.Instance.CurrentLevel.GetIntGrid("CollisionGrid");
        var entity = CreateEntity(this);
        entity.AlwaysUpdate = true;
        entity.AttachComponent<AStarGrid>().InitializeGrid(collisionGrid);
        entity.Name = "managers";
    }
}