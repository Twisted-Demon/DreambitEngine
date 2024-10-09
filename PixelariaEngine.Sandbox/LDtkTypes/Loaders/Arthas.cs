using System.Collections.Generic;
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
        
        SetUpWakeUpBounds(entity);
        SetUpSprite(entity);
        SetUpFSM(entity);
        
    }

    private void SetUpWakeUpBounds(Entity entity)
    {
        var wakeUpEntity = Entity.CreateChildOf(entity, "WakeUpBounds");
        var bounds = wakeUpEntity.AttachComponent<PolyShapeCollider>();
        bounds.IsTrigger = true;

        List<Vector2> pointsToUse = [];
        
        if (WakeUpTriggerEntityRef == null)
            return;

        var scene = Core.Instance.CurrentScene as LDtkScene;
        var triggerRef = scene!.Level.GetEntityRef<CustomTrigger>(WakeUpTriggerEntityRef);
        
        foreach (var point in triggerRef.Bounds)
        {
            var entityPosition = entity.Transform.WorldPosToVec2;
            pointsToUse.Add(point.ToVector2() - entityPosition);
        }

        if (pointsToUse.Count < 3)
            return;

        var poly = PolyShape.Create(pointsToUse.ToArray());
        bounds.SetShape(poly);
    }

    private void SetUpFSM(Entity entity)
    {
        var fsm = entity.AttachComponent<FSM>();
        
        fsm.RegisterStates([
            typeof(ArthasIdle)
        ]);
        fsm.SetInitialState<ArthasIdle>();
    }

    private void SetUpSprite(Entity entity)
    {
        var sprite = entity.GetComponent<SpriteDrawer>();
        sprite.IsHorizontalFlip = IsFlipped;
    }
}