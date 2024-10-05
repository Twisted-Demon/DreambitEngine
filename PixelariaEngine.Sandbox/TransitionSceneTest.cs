using System;
using System.Linq;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelariaEngine.ECS;
using PixelariaEngine.Graphics;
using PixelariaEngine.Sandbox.Components;

namespace PixelariaEngine.Sandbox;

public class TransitionSceneTest : Scene
{
    private Entity _bgEntity;
    private Entity _playerEntity;
    
    protected override void OnInitialize()
    {
        _bgEntity = Entity.Create("bg");
        var bgSprite = _bgEntity.AttachComponent<SpriteDrawer>();
        bgSprite.SpriteSheetPath = "SpriteSheets/SpaceBackground";
        
        _playerEntity = Entity.Create("player");
        _playerEntity.AttachComponent<SandboxController>();
        var animator = _playerEntity.AttachComponent<AnimatedSprite>();
        animator.AnimationPath = "Animations/witch_run";
        animator.Play();
        
        MainCamera.IsFollowing = true;
        MainCamera.TransformToFollow = _playerEntity.Transform;
    }

    protected override void OnUpdate()
    {
        //Scene.SetNextScene(new TransitionSceneTest().AddRenderer<DefaultRenderer>());
    }
}