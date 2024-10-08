using LDtk;
using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;
using PixelariaEngine.Sandbox.AI.Arthas;

namespace PixelariaEngine.Sandbox;

public partial class Arthas : LDtkEntity<Arthas>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this);
        //AttachTileSpriteDrawer(this, entity, RenderTiles, Color.White);
        var animator = entity.AttachComponent<SpriteAnimator>();

        var sprite = entity.GetComponent<SpriteDrawer>();
        sprite.IsHorizontalFlip = IsFlipped;

        var fsm = entity.AttachComponent<FSM>();
        
        fsm.RegisterStates([
            typeof(ArthasIdle)
        ]);
        
        fsm.SetInitialState<ArthasIdle>();
    }
}