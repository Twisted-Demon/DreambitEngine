using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PixelariaEngine.ECS;
using PixelariaEngine.Graphics;

namespace PixelariaEngine.Sandbox;

public class TransitionSceneTest : Scene
{
    private Entity _bgEntity;
    private Entity _playerEntity;
    
    protected override void OnInitialize()
    {
        _bgEntity = CreateEntity("bg");
        _bgEntity.AttachComponent<SpriteComponent>().TexturePath = "SpaceBackground";
        
        _playerEntity = CreateEntity("player");
        _playerEntity.AttachComponent<SpriteComponent>().TexturePath = "b_witch_run";

        _playerEntity.Transform.Position.Y = -25;
        
        Camera2D.TransformToFollow = _playerEntity.Transform;
        Camera2D.IsFollowing = true;
        Camera2D.CameraFollowBehavior = CameraFollowBehavior.Lerp;
    }

    protected override void OnUpdate()
    {
        var dir = Vector2.Zero;
        
        dir.X += Keyboard.GetState().IsKeyDown(Keys.D) ? 1 : 0;
        dir.X -= Keyboard.GetState().IsKeyDown(Keys.A) ? 1 : 0;
        
        dir.Y += Keyboard.GetState().IsKeyDown(Keys.S) ? 1 : 0;
        dir.Y -= Keyboard.GetState().IsKeyDown(Keys.W) ? 1 : 0;
        
        if(dir != Vector2.Zero)
            dir.Normalize();

        _playerEntity.Transform.Position += (dir * Time.DeltaTime * 75f);

        _playerEntity.Transform.Rotation += Time.DeltaTime;
        
        if(Input.IsKeyPressed(Keys.Escape))
            Core.Instance.Exit();
    }
    
}