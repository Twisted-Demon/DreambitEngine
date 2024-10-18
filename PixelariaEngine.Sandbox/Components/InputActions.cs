using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PixelariaEngine.ECS;

namespace PixelariaEngine.Sandbox;

public class InputActions : Component
{
    public Action<Vector2> PlayerMovementAction;

    public Action ArthasSpellAction;

    public Action DarionSpellAction;

    public override void OnUpdate()
    {
        CheckForArthasSpellInput();
        CheckForDarionSpellInput();
    }

    public void CheckForArthasSpellInput()
    {
        if(Input.IsKeyPressed(Keys.A))
            ArthasSpellAction?.Invoke();
    }

    public void CheckForDarionSpellInput()
    {
        if(Input.IsKeyPressed(Keys.S))
            DarionSpellAction?.Invoke();
    }
}