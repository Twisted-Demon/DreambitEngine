using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.ECS;

/// <summary>
/// Draws generic Dreambit tilemap data and rejects off-camera tiles using the
/// layer's spatial grid. Importers are responsible only for producing
/// <see cref="TilemapLayerData"/> and loading the corresponding texture.
/// </summary>
[BlueprintType(nameof(TilemapRenderer))]
public sealed class TilemapRenderer : DrawableComponent<TilemapRenderer>
{
    public Texture2D Texture { get; private set; }
    public TilemapLayerData Layer { get; private set; }

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

    protected override void OnDraw()
    {
        ArgumentNullException.ThrowIfNull(Texture);
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

        for (var row = minimumRow; row <= maximumRow; row++)
        for (var column = minimumColumn; column <= maximumColumn; column++)
        foreach (var tile in Layer.GetTiles(column, row))
        {
            var worldBounds = TransformBounds(tile.Bounds, worldMatrix);
            if (!cameraBounds.Intersects(worldBounds))
                continue;

            var scale = new Vector2(
                tile.Size.X / tile.SourceRectangle.Width,
                tile.Size.Y / tile.SourceRectangle.Height) * Transform.WorldScale2D;
            Core.SpriteBatch.DrawWorldSprite(
                Texture,
                Vector2.Transform(tile.Position, worldMatrix),
                tile.SourceRectangle,
                tile.Tint * Tint,
                Transform.WorldRotation2D,
                Vector2.Zero,
                scale,
                tile.Effects);
            LastVisibleTileCount++;
        }
    }

    public override void OnDestroyed()
    {
        Texture = null;
        Layer = null;
        LastVisibleTileCount = 0;
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
