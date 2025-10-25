using Dreambit.ECS;
using Microsoft.Xna.Framework.Input;

namespace Dreambit.BriGame.Components.InternalDev;

public class DebugToggleComponent : Component
{
    public override void OnAddedToEntity()
    {
        Scene.DebugMode = false;
    }

    public override void OnUpdate()
    {
        if (Input.IsKeyPressed(Keys.F8))
            Scene.DebugMode = !Scene.DebugMode;
    }
}