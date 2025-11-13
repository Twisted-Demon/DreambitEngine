using Microsoft.Xna.Framework;

namespace Dreambit;

public class InputActionContext
{
    public readonly string Name;
    public readonly float Time;
    public readonly float V1;
    public readonly Vector2 V2;

    public InputActionContext(string name, float time, float v1, Vector2 v2)
    {
        Name = name;
        Time = time;
        V1 = v1;
        V2 = v2;
    }
}

public enum InputActionType
{
    Button,
    Value1D,
    Value2D
}

public enum Axis1D
{
    MouseX,
    MouseY,
    Scroll
}

public enum InputActionPhase
{
    Waiting,
    Started,
    Performed,
    Canceled
}