using Microsoft.Xna.Framework;

namespace Dreambit;

public class InputActionContext
{
    public readonly string Name;
    public readonly float Time;
    public readonly float Value1D;
    public readonly Vector2 Value2D;

    public InputActionContext(string name, float time, float value1D, Vector2 value2D)
    {
        Name = name;
        Time = time;
        Value1D = value1D;
        Value2D = value2D;
    }
}