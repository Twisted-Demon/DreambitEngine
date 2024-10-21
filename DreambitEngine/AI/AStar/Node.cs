using Microsoft.Xna.Framework;

namespace Dreambit;

public struct Node(int x, int y, bool isWalkable)
{
    public int X { get; } = x;
    public int Y { get; } = y;
    public bool IsWalkable { get; } = isWalkable;

    public Vector2 ToVec2()
    {
        return new Vector2(X, Y);
    }
}