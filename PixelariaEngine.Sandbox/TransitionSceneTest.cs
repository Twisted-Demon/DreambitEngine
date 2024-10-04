using System;
using System.Linq;
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
        _bgEntity.AttachComponent<SpriteComponent>().TexturePath = "SpaceBackground";
        
        _playerEntity = Entity.Create("player");
        _playerEntity.AttachComponent<SpriteComponent>().TexturePath = "b_witch_run";
        _playerEntity.AttachComponent<SandboxController>();

        MainCamera.IsFollowing = true;
        MainCamera.TransformToFollow = _playerEntity.Transform;
    }
}