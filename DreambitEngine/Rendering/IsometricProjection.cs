using Microsoft.Xna.Framework;

namespace Dreambit;

public static class IsometricProjection
{
    public const float TileWidth = 2f;
    public const float TileHeight = 1f;

    public const float ElevationHeight = 1f;

    private const float HalfTileWidth = TileWidth * 0.5f;
    private const float HalfTileHeight = TileHeight * 0.5f;

    public static Vector2 WorldToRender(Vector2 worldPosition)
    {
        return new Vector2(
            (worldPosition.X - worldPosition.Y) *
            HalfTileWidth,

            (worldPosition.X + worldPosition.Y) *
            HalfTileHeight);
    }

    public static Vector2 WorldToRender(
        Vector3 worldPosition)
    {
        var groundPosition =
            WorldToRender(
                new Vector2(
                    worldPosition.X,
                    worldPosition.Y));

        groundPosition.Y -=
            worldPosition.Z * ElevationHeight;

        return groundPosition;
    }

    public static Vector2 RenderToWorld(
        Vector2 renderPosition,
        float worldZ = 0f)
    {
        var projectedY =
            renderPosition.Y +
            worldZ * ElevationHeight;

        var x =
        (
            renderPosition.X / HalfTileWidth +
            projectedY / HalfTileHeight
        ) * 0.5f;

        var y =
        (
            projectedY / HalfTileHeight -
            renderPosition.X / HalfTileWidth
        ) * 0.5f;

        return new Vector2(x, y);
    }

    public static Vector2 WorldVectorToRender(
        Vector2 worldVector)
    {
        return new Vector2(
            (worldVector.X - worldVector.Y) *
            HalfTileWidth,

            (worldVector.X + worldVector.Y) *
            HalfTileHeight);
    }

    public static float WorldDirectionToRenderRotation(
        Vector2 worldDirection)
    {
        if (worldDirection.LengthSquared() <=
            float.Epsilon)
            return 0f;

        var renderDirection =
            WorldVectorToRender(worldDirection);

        return Mathf.Atan2(
            renderDirection.Y,
            renderDirection.X);
    }

    public static float GetSortDepth(
        Vector3 worldPosition)
    {
        return worldPosition.X +
               worldPosition.Y;
    }
}
