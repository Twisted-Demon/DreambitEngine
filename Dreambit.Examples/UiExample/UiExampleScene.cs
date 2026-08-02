using Dreambit.ECS;
using Dreambit.Examples.Particles;
using Dreambit.Examples.Pong;
using Dreambit.Examples.SpaceGame;
using Dreambit.UI;

namespace Dreambit.Examples.UiExample;

public class UiExampleScene : Scene<UiExampleScene>
{
    private const int WindowWidth = 1280;
    private const int WindowHeight = 720;

    protected override void OnInitialize()
    {
        InitializeSettings();

        var menu = CreateEntity("menu")
            .AttachComponent<UiFrame>();

        menu.LayoutPath = "Ui/menu.xml";

        menu.Layout
            .GetRequired<UiButton>("play-particles-button")
            .Clicked += OnPlayParticlesClicked;

        menu.Layout
            .GetRequired<UiButton>("play-pong-button")
            .Clicked += OnPlayPongClicked;

        menu.Layout
            .GetRequired<UiButton>("play-spacegame-button")
            .Clicked += OnPlaySpaceGameClicked;
    }


    private void InitializeSettings()
    {
        Window.SetSize(WindowWidth, WindowHeight);

        MainCamera.PixelsPerUnit = 1;
        MainCamera.SetTargetVerticalResolution(WindowHeight);

        UiCamera.PixelsPerUnit = 1;
        UiCamera.SetTargetVerticalResolution(WindowHeight);
    }

    private void OnPlayParticlesClicked(UiButton button)
    {
        Scene.SetNextScene<ParticlesScene>();
    }
    
    private void OnPlayPongClicked(UiButton button)
    {
        Scene.SetNextScene<PongScene>();
    }
    
    private void OnPlaySpaceGameClicked(UiButton button)
    {
        Scene.SetNextScene<SpaceGameScene>();
    }
}
