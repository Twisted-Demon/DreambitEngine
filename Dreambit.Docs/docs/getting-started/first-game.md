# Your first game

This scene creates a controllable square. It shows the engine's normal flow:
create entities in `OnInitialize`, attach components, and write gameplay in a
custom component.

```csharp
using Dreambit;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public sealed class MainScene : Scene<MainScene>
{
    protected override void OnInitialize()
    {
        Window.SetSize(1280, 720);
        BackgroundColor = new Color(18, 20, 28);

        MainCamera.SetTargetVerticalResolution(720f);
        MainCamera.ForcePosition(new Vector3(640, 360, 0));

        CreateEntity("player", tags: ["player"],
                createAt: new Vector3(640, 360, 0))
            .AttachComponent<PlayerController>();
    }
}

[Require(typeof(RectDrawer))]
public sealed class PlayerController : Component
{
    [FromRequired] private RectDrawer _drawer;

    public override void OnCreated()
    {
        _drawer.Width = 32;
        _drawer.Height = 32;
        _drawer.Color = Color.CornflowerBlue;
    }

    public override void OnUpdate()
    {
        var direction = Vector2.Zero;
        if (Input.IsKeyHeld(Keys.A)) direction.X--;
        if (Input.IsKeyHeld(Keys.D)) direction.X++;
        if (Input.IsKeyHeld(Keys.W)) direction.Y--;
        if (Input.IsKeyHeld(Keys.S)) direction.Y++;

        if (direction != Vector2.Zero)
            direction.Normalize();

        Transform.TranslateWorld2D(direction * 240f * Time.DeltaTime);
    }
}
```

`[Require]` makes sure `RectDrawer` exists before `PlayerController` is created.
`[FromRequired]` fills the field, so the component does not need a manual lookup.

## Run it

Schedule `MainScene` before `game.Run()` as shown on the installation page. You
should see a blue square that moves with WASD.

## Next steps

- Replace the square with a [SpriteDrawer](../ecs/components/sprite-drawer.md).
- Add a [BoxCollider](../ecs/components/box-collider.md).
- Move input into an [input action map](../input/actions.md).
- Add a HUD with a [UiFrame](../ecs/components/ui-frame.md).

