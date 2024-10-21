using System.Collections.Generic;
using Dreambit.ECS;
using Dreambit.Sandbox.Drawable;
using Dreambit.Sandbox.Player;
using Dreambit.Sandbox.Utils;
using LDtk;
using Microsoft.Xna.Framework;

namespace Dreambit.Sandbox;

public partial class Darion : LDtkEntity<Darion>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this, "darion");
        //AttachTileSpriteDrawer(this, entity, RenderTiles, Color.White);
        entity.AttachComponent<SpriteAnimator>();

        entity.AttachComponent<PathFollower>();

        
        SetUpSprite(entity);
        
        var iconEntity = Entity.CreateChildOf(entity, "floatingIcon", ["floatingIcon"]);
        var iconComponent = iconEntity.AttachComponent<FloatingIcon>();

        iconComponent.Offset = new Vector2(0, 15);

        iconComponent.Icon = KeyboardIcon.E;
        iconEntity.Enabled = false;
        
        SetUpFSM(entity);
        entity.Enabled = false;
    }

    private void SetUpFSM(Entity entity)
    {
        var fsm = entity.AttachComponent<FSM>();
        
        fsm.Blackboard.CreateVariable("followDistance", 35.0f);
        fsm.Blackboard.CreateVariable("followSpeed", 70.0f);
        fsm.Blackboard.CreateVariable<Entity>("player");
        fsm.Blackboard.CreateVariable("catType", "Darion");
        var idleAnimation = fsm.Blackboard.CreateVariable<SpriteSheetAnimation>("idleAnimation");
        var lickAnimation = fsm.Blackboard.CreateVariable<SpriteSheetAnimation>("lickAnimation");
        var sleepAnimation = fsm.Blackboard.CreateVariable<SpriteSheetAnimation>("sleepAnimation");
        var runAnimation = fsm.Blackboard.CreateVariable<SpriteSheetAnimation>("runAnimation");
        
        idleAnimation.Value = Resources.LoadAsset<SpriteSheetAnimation>("Animations/darion_idle");
        lickAnimation.Value = Resources.LoadAsset<SpriteSheetAnimation>("Animations/darion_lick");
        sleepAnimation.Value = Resources.LoadAsset<SpriteSheetAnimation>("Animations/darion_sleep");
        runAnimation.Value = Resources.LoadAsset<SpriteSheetAnimation>("Animations/darion_run");
        
        fsm.Blackboard.CreateVariable("isAwake", false);
        
        fsm.RegisterStates([
            typeof(CatIdle),
            typeof(CatFollow),
            typeof(InDialogue)
        ]);
        fsm.SetDefaultState<CatIdle>();
        fsm.SetNextState<InDialogue>();
    }
    
    private void SetUpSprite(Entity entity)
    {
        var sprite = entity.GetComponent<SpriteDrawer>();
        sprite.IsHorizontalFlip = IsFlipped;
    }
}