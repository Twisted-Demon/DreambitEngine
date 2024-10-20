using System.Collections.Generic;
using LDtk;
using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;
using PixelariaEngine.Sandbox.Drawable;
using PixelariaEngine.Sandbox.Utils;

namespace PixelariaEngine.Sandbox;

public partial class Darion : LDtkEntity<Darion>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this);
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
            typeof(ArthasSleep),
            typeof(CatIdle),
            typeof(CatFollow)
        ]);
        fsm.SetDefaultState<ArthasSleep>();
    }
    
    private void SetUpSprite(Entity entity)
    {
        var sprite = entity.GetComponent<SpriteDrawer>();
        sprite.IsHorizontalFlip = IsFlipped;
    }
}