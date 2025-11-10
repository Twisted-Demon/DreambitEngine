using System;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Dreambit.Examples.Pong;

public class PongController : Component<PongController>
{
    private PongPaddle _playerOnePaddle;
    private PongPaddle _playerTwoPaddle;

    private Vector2 _leftInputDir;
    private Vector2 _rightInputDir;

    private float _paddleSpeed = 288.0f;

    public override void OnAddedToEntity()
    {
        _playerOnePaddle = Entity.FindByName("player_one_paddle").GetComponent<PongPaddle>();
        _playerTwoPaddle = Entity.FindByName("player_two_paddle").GetComponent<PongPaddle>();
    }

    public override void OnUpdate()
    {
        HandleInput();
        HandleMovement();
    }

    private void HandleInput()
    {
        //Reset input and check what we are pressing
        
        _leftInputDir = Vector2.Zero;
        _leftInputDir.Y += Input.IsKeyHeld(Keys.W) ? -1 : 0;
        _leftInputDir.Y += Input.IsKeyHeld(Keys.S) ? 1 : 0;
        
        _rightInputDir = Vector2.Zero;
        _rightInputDir.Y += Input.IsKeyHeld(Keys.Up) ? -1 : 0;
        _rightInputDir.Y += Input.IsKeyHeld(Keys.Down) ? 1 : 0;

        if (Input.IsKeyPressed(Keys.Space))
            Scene.SetNextScene(new PongScene());
    }
    
    private void HandleMovement()
    {
        MovePaddle(_playerOnePaddle.Entity, _leftInputDir.Y);
        MovePaddle(_playerTwoPaddle.Entity, _rightInputDir.Y);
    }

    private void MovePaddle(Entity paddle, float inputY)
    {
        var pos = paddle.Transform.Position;
        pos.Y += inputY * _paddleSpeed * Time.DeltaTime;

        // Clamp so the never goes above/below allowed range
        const int minY = 0;
        const int maxY = PongSettings.GameHeight;
        pos.Y = Mathf.Clamp(pos.Y, minY, maxY);

        paddle.Transform.Position = pos;
    }
}