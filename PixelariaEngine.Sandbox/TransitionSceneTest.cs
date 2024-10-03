using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox;

public class TransitionSceneTest : Scene
{
    private Entity _player;
    private SpriteComponent _playerSprite;
    protected override void OnInitialize()
    {
        _player = CreateEntity("Player");
        _playerSprite = _player.AttachComponent<SpriteComponent>();
        _playerSprite.TexturePath = "SpaceBackground";
    }

    protected override void OnUpdate()
    {
        if(Input.IsKeyPressed(Keys.I))
            Core.Instance.SetNextScene(new TransitionSceneTest());

        if (Input.IsKeyPressed(Keys.H))
            _player.DetachComponent<SpriteComponent>();
        
        if(Input.IsKeyPressed(Keys.Escape))
            Core.Instance.Exit();
    }
    
}