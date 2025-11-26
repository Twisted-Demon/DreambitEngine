using Dreambit.ECS;

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
    }

    private void InitializeSettings()
    {
        Window.SetSize(WindowWidth, WindowHeight);

        MainCamera.PixelsPerUnit = 1;
        MainCamera.SetTargetVerticalResolution(WindowHeight);

        UICamera.PixelsPerUnit = 1;
        UICamera.SetTargetVerticalResolution(WindowHeight);
    }
}