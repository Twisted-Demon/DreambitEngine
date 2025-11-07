using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.Examples.Pong;

public class PongScene : Scene
{
    private float _paddleMargin = 32f;
    
    protected override void OnInitialize()
    {
        MainCamera.ForcePosition(new Vector3(PongSettings.GameWidth * 0.5f, PongSettings.GameHeight * 0.5f, 0));
        
        AmbientLight.Color = Color.White;
        SetUpController();
        SetUpPaddles();
        SetUpBall();
        SetUpScoreKeeper();
    }

    private void SetUpController()
    {
        var controller = CreateEntity("player_controller", tags: ["controller"])
            .AttachComponent<PongController>();
    }

    private void SetUpPaddles()
    {
        Vector2 leftPaddleSpawn;
        leftPaddleSpawn.X = _paddleMargin;
        leftPaddleSpawn.Y = (float)PongSettings.GameHeight * 0.5f;

        Vector2 rightPaddleSpawn;
        rightPaddleSpawn.X = (float)PongSettings.GameWidth - _paddleMargin;
        rightPaddleSpawn.Y = (float)PongSettings.GameHeight * 0.5f;

        var playerOne = Entity.Create("player_one_paddle", tags: ["paddle"], createAt: leftPaddleSpawn.ToVector3())
            .AttachComponent<PongPaddle>();
        playerOne.PlayerNumber = PlayerNumber.One;

        var playerTwo = Entity.Create("player_two_paddle", tags: ["paddle"], createAt: rightPaddleSpawn.ToVector3())
            .AttachComponent<PongPaddle>();
        playerTwo.PlayerNumber = PlayerNumber.Two;
    }

    private void SetUpBall()
    {
        Vector2 position;
        position.X = PongSettings.GameWidth * 0.5f;
        position.Y = PongSettings.GameHeight * 0.5f;
        
        var paddle = CreateEntity("pong-ball", tags:["ball"], createAt: position.ToVector3())
            .AttachComponent<PongBall>();
    }

    private void SetUpScoreKeeper()
    {
        var score = CreateEntity("score").AttachComponent<PongScoreKeeper>();
    }
}