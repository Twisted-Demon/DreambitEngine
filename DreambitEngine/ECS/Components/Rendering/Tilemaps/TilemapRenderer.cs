#nullable enable

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.ECS;

/// <summary>
/// Draws generic Dreambit tilemap data and rejects off-camera tiles using the
/// layer's spatial grid. Importers are responsible only for producing
/// <see cref="TilemapLayerData"/> and loading the corresponding textures.
/// </summary>
[BlueprintType(nameof(TilemapRenderer))]
public sealed class TilemapRenderer : DrawableComponent<TilemapRenderer>
{
    public Texture2D? Texture { get; private set; }
    public TilemapLayerData? Layer { get; private set; }

    [DreambitSerialize]
    public Color Tint { get; set; } = Color.White;

    /// <summary>Number of tiles submitted during the most recent draw.</summary>
    public int LastVisibleTileCount { get; private set; }

    public override RectangleF Bounds
    {
        get
        {
            if (Layer is null || Transform is null)
                return RectangleF.Empty;

            return TransformBounds(Layer.Bounds, Transform.WorldMatrix);
        }
    }

    public TilemapRenderer Configure(Texture2D texture, TilemapLayerData layer)
    {
        Texture = texture ?? throw new ArgumentNullException(nameof(texture));
        Layer = layer ?? throw new ArgumentNullException(nameof(layer));
        return this;
    }

    public TilemapRenderer Configure(TilemapLayerData layer)
    {
        Texture = null;
        Layer = layer ?? throw new ArgumentNullException(nameof(layer));
        return this;
    }

    protected override void OnDraw()
    {
        ArgumentNullException.ThrowIfNull(Layer);

        LastVisibleTileCount = 0;
        var cameraBounds = Scene.MainCamera.BoundsF;
        var worldMatrix = Transform.WorldMatrix;
        var localView = TransformBounds(cameraBounds, Matrix.Invert(worldMatrix));

        if (!Layer.TryGetVisibleCellRange(
                localView,
                out var minimumColumn,
                out var minimumRow,
                out var maximumColumn,
                out var maximumRow))
            return;

        var elapsedMilliseconds = Time.TimeSinceSceneLoaded * 1000f;
        switch (Layer.RenderOrder)
        {
            case TilemapRenderOrder.RightDown:
                for (var row = minimumRow; row <= maximumRow; row++)
                for (var column = minimumColumn; column <= maximumColumn; column++)
                    DrawCell(column, row, cameraBounds, worldMatrix, elapsedMilliseconds);
                break;
            case TilemapRenderOrder.RightUp:
                for (var row = maximumRow; row >= minimumRow; row--)
                for (var column = minimumColumn; column <= maximumColumn; column++)
                    DrawCell(column, row, cameraBounds, worldMatrix, elapsedMilliseconds);
                break;
            case TilemapRenderOrder.LeftDown:
                for (var row = minimumRow; row <= maximumRow; row++)
                for (var column = maximumColumn; column >= minimumColumn; column--)
                    DrawCell(column, row, cameraBounds, worldMatrix, elapsedMilliseconds);
                break;
            case TilemapRenderOrder.LeftUp:
                for (var row = maximumRow; row >= minimumRow; row--)
                for (var column = maximumColumn; column >= minimumColumn; column--)
                    DrawCell(column, row, cameraBounds, worldMatrix, elapsedMilliseconds);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Layer.RenderOrder));
        }
    }

    public override void OnDestroyed()
    {
        Texture = null;
        Layer = null;
        LastVisibleTileCount = 0;
    }

    private void DrawCell(
        int column,
        int row,
        RectangleF cameraBounds,
        Matrix worldMatrix,
        float elapsedMilliseconds)
    {
        foreach (var tile in Layer!.GetTiles(column, row))
        {
            var worldBounds = TransformBounds(tile.Bounds, worldMatrix);
            if (!cameraBounds.Intersects(worldBounds))
                continue;

            var frame = tile.Animation?.GetFrame(elapsedMilliseconds);
            var sourceRectangle = frame?.SourceRectangle ?? tile.SourceRectangle;
            var texture = frame?.Texture ?? tile.Texture ?? Texture
                          ?? throw new InvalidOperationException(
                              "A tilemap tile has no texture and the renderer has no fallback texture.");
            var quarterTurn = MathF.Abs(MathF.Sin(tile.Rotation)) > 0.5f;
            var scale = quarterTurn
                ? new Vector2(
                    tile.Size.Y / sourceRectangle.Width,
                    tile.Size.X / sourceRectangle.Height)
                : new Vector2(
                    tile.Size.X / sourceRectangle.Width,
                    tile.Size.Y / sourceRectangle.Height);
            scale *= Transform.WorldScale2D;

            Core.SpriteBatch.DrawWorldSprite(
                texture,
                Vector2.Transform(tile.Position + tile.Size * 0.5f, worldMatrix),
                sourceRectangle,
                tile.Tint * Tint,
                Transform.WorldRotation2D + tile.Rotation,
                new Vector2(sourceRectangle.Width * 0.5f, sourceRectangle.Height * 0.5f),
                scale,
                tile.Effects);
            LastVisibleTileCount++;
        }
    }

    private static RectangleF TransformBounds(RectangleF bounds, Matrix matrix)
    {
        var topLeft = Vector2.Transform(new Vector2(bounds.Left, bounds.Top), matrix);
        var topRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Top), matrix);
        var bottomLeft = Vector2.Transform(new Vector2(bounds.Left, bounds.Bottom), matrix);
        var bottomRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Bottom), matrix);
        var minimumX = MathF.Min(MathF.Min(topLeft.X, topRight.X), MathF.Min(bottomLeft.X, bottomRight.X));
        var minimumY = MathF.Min(MathF.Min(topLeft.Y, topRight.Y), MathF.Min(bottomLeft.Y, bottomRight.Y));
        var maximumX = MathF.Max(MathF.Max(topLeft.X, topRight.X), MathF.Max(bottomLeft.X, bottomRight.X));
        var maximumY = MathF.Max(MathF.Max(topLeft.Y, topRight.Y), MathF.Max(bottomLeft.Y, bottomRight.Y));
        return new RectangleF(minimumX, minimumY, maximumX - minimumX, maximumY - minimumY);
    }
}
