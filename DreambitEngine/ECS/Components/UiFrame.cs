using System.IO;
using Dreambit.UI;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

[BlueprintType(nameof(UiFrame))]
public class UiFrame : DrawableComponent<UiFrame>
{
    private UiLayout _layout;

    public override void OnCreated()
    {
        var xml = File.ReadAllText("Content/Ui/menu.xml");

        _layout = UiLoader.LoadFromXml(xml);
        Scene.DebugMode = true;

        Window.WindowResized += OnWindowResized;
    }

    private void OnWindowResized(object sender, WindowResizedEventArgs e)
    {
        _layout.Root.InvalidateLayout();
    }

    public override void OnUpdate()
    {
        var screenSize = Window.ScreenSize;

        Scene.UiCamera.SetTargetVerticalResolution(screenSize.Y);
        
        _layout.Root.Width = UiLength.Pixels(screenSize.X);
        _layout.Root.Height = UiLength.Pixels(screenSize.Y);
        _layout.Root.Arrange(new Rectangle(0, 0, screenSize.X, screenSize.Y));
        _layout.Root.Update();
    }

    public override void OnDrawUi()
    {
        _layout.Root.OnDraw();
    }
    
    
    

    public override RectangleF Bounds => Scene.MainCamera.BoundsF;
}