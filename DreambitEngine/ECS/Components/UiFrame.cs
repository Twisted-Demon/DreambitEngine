using System.IO;
using Dreambit.UI;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class UiFrame : DrawableComponent<UiFrame>
{
    private UiLayout _layout;

    public override void OnCreated()
    {
        var xml = File.ReadAllText("Content/Ui/menu.xml");

        _layout = UiLoader.LoadFromXml(xml);
        Scene.DebugMode = true;
    }

    public override void OnUpdate()
    {
        var screenSize = Window.ScreenSize;
        _layout.Root.Width = UiLength.Pixels(screenSize.X);
        _layout.Root.Height = UiLength.Pixels(screenSize.Y);
        _layout.Root.Arrange(new Rectangle(0, 0, screenSize.X, screenSize.Y));
        _layout.Root.OnUpdate();
    }

    public override void OnDrawUi()
    {
        _layout.Root.Draw();
    }
    

    public override Rectangle Bounds => Scene.MainCamera.Bounds;
}