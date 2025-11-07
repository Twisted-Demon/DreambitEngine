using Dreambit.ECS;
using Microsoft.Xna.Framework.Input;

namespace Dreambit.BriGame.Components.InternalDev;

public class DebugToggleComponent : Component
{
    public override void OnAddedToEntity()
    {
    }

    public override void OnUpdate()
    {
        if (Input.IsKeyPressed(Keys.F8))
            Scene.DebugMode = !Scene.DebugMode;

        if (Input.IsKeyHeld(Keys.Up))
            Scene.MainCamera.Zoom += Time.DeltaTime;
        if (Input.IsKeyHeld(Keys.Down))
            Scene.MainCamera.Zoom -= Time.DeltaTime;
    }
}