using Microsoft.Xna.Framework;

namespace PixelariaEngine;

public static class Vector3Extensions
{
    public static Vector2 ToVector2(this Vector3 vector)
    {
        return new Vector2(vector.X, vector.Y);
    }
}