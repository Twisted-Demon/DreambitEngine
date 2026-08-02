using Dreambit.ECS;
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

        menu.Layout
            .GetRequired<UiButton>("play-button")
            .Clicked += OnPlayClicked;
    }

    private void InitializeSettings()
    {
        Window.SetSize(WindowWidth, WindowHeight);

        MainCamera.PixelsPerUnit = 1;
        MainCamera.SetTargetVerticalResolution(WindowHeight);

        UiCamera.PixelsPerUnit = 1;
        UiCamera.SetTargetVerticalResolution(WindowHeight);
    }

    private void OnPlayClicked(UiButton button)
    {
        Logger.Info("Play clicked");
    }
}
