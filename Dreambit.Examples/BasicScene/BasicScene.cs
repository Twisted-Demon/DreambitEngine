using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Dreambit.Examples;

public class BasicScene : Scene
{
    protected override void OnInitialize()
    {
        Window.SetSize(800, 600);
        BackgroundColor = new Color(18, 20, 28);

        MainCamera.PixelsPerUnit = 1;
        MainCamera.SetTargetVerticalResolution(600);
        MainCamera.ForcePosition(Vector3.Zero);

        CreateEntity("player", tags: ["player"],
                createAt: Vector3.Zero)
            .AttachComponent<BasicPlayerController>();
    }
}

[Require(typeof(RectDrawer))]
public sealed class BasicPlayerController : Component
{
    [FromRequired] private RectDrawer _drawer;

    public override void OnCreated()
    {
        _drawer.Width = 16;
        _drawer.Height = 16;
        _drawer.Color = Color.Beige;
    }

    public override void OnUpdate()
    {
        var direction = Vector2.Zero;

        if (Input.IsKeyHeld(Keys.A)) direction.X--;
        if (Input.IsKeyHeld(Keys.D)) direction.X++;
        if (Input.IsKeyHeld(Keys.W)) direction.Y--;
        if (Input.IsKeyHeld(Keys.S)) direction.Y++;
        
        if(direction != Vector2.Zero)
            direction.Normalize();

        Transform.TranslateWorld2D(direction * 240f * Time.DeltaTime);
    }
}