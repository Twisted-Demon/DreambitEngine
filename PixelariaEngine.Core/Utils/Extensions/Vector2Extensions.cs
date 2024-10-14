using Microsoft.Xna.Framework;

namespace PixelariaEngine;

public static class Vector2Extensions
{
    public static Vector3 ToVector3(this Vector2 vector)
    {
        return new Vector3(vector.X, vector.Y, 0);
    }
}