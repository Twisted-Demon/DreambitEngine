using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.Examples.Pong;

public class PongScoreKeeper : Component
{
    private readonly float _margin = 1.0f;


    public Canvas Canvas;
    public UIText PlayerOneScoreText;
    public UIText PlayerTwoScoreText;

    public override void OnCreated()
    {
        Scene.UICamera.SetTargetVerticalResolution(720);
        
        Canvas = Entity.AttachComponent<Canvas>();

        PlayerOneScoreText = Canvas.CreateUIElement<UIText>();
        PlayerTwoScoreText = Canvas.CreateUIElement<UIText>();

        PlayerOneScoreText.FontPath = "Fonts/monogram";
        PlayerTwoScoreText.FontPath = "Fonts/monogram";

        PlayerOneScoreText.FontSize = 62.0f;
        PlayerTwoScoreText.FontSize = 62.0f;

        PlayerOneScoreText.VAlignment = VerticalAlignment.Top;
        PlayerOneScoreText.HAlignment   = HorizontalAlignment.Left;
        
        PlayerTwoScoreText.VAlignment = VerticalAlignment.Top;
        PlayerTwoScoreText.HAlignment   = HorizontalAlignment.Right;
        
        PlayerOneScoreText.Text = PongSettings.PlayerOneScore.ToString();
        PlayerTwoScoreText.Text = PongSettings.PlayerTwoScore.ToString();

        PlayerOneScoreText.Entity.Transform.Position = new Vector3(-(50 - _margin), -(50 - _margin), 0);
        PlayerTwoScoreText.Entity.Transform.Position = new Vector3((50 - _margin), -(50 - _margin), 0);
    }

    public override void OnUpdate()
    {
        PlayerOneScoreText.Text = PongSettings.PlayerOneScore.ToString();
        PlayerTwoScoreText.Text = PongSettings.PlayerTwoScore.ToString();
    }
}