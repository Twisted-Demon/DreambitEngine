using Microsoft.Xna.Framework;

namespace PixelariaEngine.ECS;

public struct Transform
{
    public Vector2 Position = new(0, 0);
    public float Rotation = 0;
    public Vector2 Scale = new(1, 1);

    public Transform()
    {
    }
}