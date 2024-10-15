using System.Collections.Generic;
using LDtk;
using Microsoft.Xna.Framework;
using PixelariaEngine.ECS;
using PixelariaEngine.Sandbox.Drawable;
using PixelariaEngine.Sandbox.Utils;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace PixelariaEngine.Sandbox;

public partial class Arthas : LDtkEntity<Arthas>
{
    protected override void SetUp(LDtkLevel level)
    {
        var entity = CreateEntity(this);
        //AttachTileSpriteDrawer(this, entity, RenderTiles, Color.White);
        entity.AttachComponent<SpriteAnimator>();

        entity.AttachComponent<PathFollower>();
        
        SetUpSprite(entity);

        var lightEntity = Entity.CreateChildOf(entity, "light", ["light"]);
        var light = lightEntity.AttachComponent<PointLight2D>();
        
        light.Color = new Vector3(253/(float)255, 251/(float)255, 251/(float)211);
        light.Intensity = 0.75f;
        light.Radius = 125.0f;
        
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
        fsm.Blackboard.CreateVariable("followSpeed", 75.0f);
        fsm.Blackboard.CreateVariable<Entity>("player");
        fsm.Blackboard.CreateVariable("catType", "Arthas");
        var idleAnimation = fsm.Blackboard.CreateVariable<SpriteSheetAnimation>("idleAnimation");
        var lickAnimation = fsm.Blackboard.CreateVariable<SpriteSheetAnimation>("lickAnimation");
        var sleepAnimation = fsm.Blackboard.CreateVariable<SpriteSheetAnimation>("sleepAnimation");
        var runAnimation = fsm.Blackboard.CreateVariable<SpriteSheetAnimation>("runAnimation");
        
        idleAnimation.Value = Resources.LoadAsset<SpriteSheetAnimation>("Animations/arthas_idle");
        lickAnimation.Value = Resources.LoadAsset<SpriteSheetAnimation>("Animations/arthas_lick");
        sleepAnimation.Value = Resources.LoadAsset<SpriteSheetAnimation>("Animations/arthas_sleep");
        runAnimation.Value = Resources.LoadAsset<SpriteSheetAnimation>("Animations/arthas_run");
        
        fsm.Blackboard.CreateVariable("isAwake", false);
        
        fsm.RegisterStates([
            typeof(ArthasSleep),
            typeof(CatIdle),
            typeof(CatFollow)
        ]);
        fsm.SetInitialState<ArthasSleep>();
    }

    private void SetUpSprite(Entity entity)
    {
        var sprite = entity.GetComponent<SpriteDrawer>();
        sprite.IsHorizontalFlip = IsFlipped;
    }
}