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
        var bg = Entity.Create("bg");
        var bgSprite = bg.AttachComponent<SpriteDrawer>();
        bgSprite.SpriteSheetPath = "SpriteSheets/SpaceBackground";
        
        _playerEntity = Entity.Create("player");
        var sprite = _playerEntity.AttachComponent<SpriteDrawer>();
        sprite.SpriteSheetPath = "SpriteSheets/b_witch_run";
        sprite.CurrentFrameIndex = 2;
        _playerEntity.AttachComponent<SandboxController>();

        _playerEntity.AttachComponent<FrameSerializer>();
        MainCamera.IsFollowing = true;
        MainCamera.TransformToFollow = _playerEntity.Transform;
    }

    protected override void OnUpdate()
    {
        Scene.SetNextScene(new TransitionSceneTest().AddRenderer<DefaultRenderer>());
    }
}