using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.ECS;

[BlueprintType(nameof(SpriteDrawer))]
public class SpriteDrawer :
    DrawableComponent<SpriteDrawer>
{
    [DreambitSerialize]
    public Color Tint { get; internal set; } =
        Color.White;

    [DreambitSerialize]
    public float Opacity { get; internal set; } =
        1.0f;

    /// <summary>
    /// The custom pivot in pixels relative to the sprite's source rectangle.
    /// It is converted to world space using the sprite draw scale.
    /// </summary>
    [DreambitSerialize]
    public Vector2 Pivot { get; internal set; } =
        Vector2.Zero;

    [DreambitSerialize]
    public PivotType PivotType { get; internal set; } =
        PivotType.Center;

    [DreambitSerialize("SpritePath")]
    public Sprite Sprite { get; set; }

    /// <summary>
    /// Legacy source compatibility. Blueprints serialize <see cref="Sprite"/> as
    /// an asset reference and never write this path property.
    /// </summary>
    [Obsolete("Use Sprite. SpritePath is accepted only as a legacy blueprint name.")]
    public string SpritePath
    {
        get => Sprite?.AssetName ?? string.Empty;
        set
        {
            Sprite =
                string.IsNullOrWhiteSpace(value)
                    ? null
                    : Resources.LoadAsset<Sprite>(value);
        }
    }

    [DreambitSerialize]
    public bool FlipX { get; set; }

    public override RectangleF Bounds
    {
        get
        {
            Vector2 position;

            if (Sprite is null)
            {
                position =
                    GetDrawPosition();

                return new RectangleF(
                    position.X,
                    position.Y,
                    1f,
                    1f);
            }

            var worldOrigin =
                GetWorldOriginToUse();

            var worldSize =
                new Vector2(
                    Sprite.SourceRect.Width,
                    Sprite.SourceRect.Height) *
                GetSpriteDrawScale();

            position =
                GetDrawPosition();

            var left =
                position.X -
                worldOrigin.X;

            var top =
                position.Y -
                worldOrigin.Y;

            var right =
                left +
                worldSize.X;

            var bottom =
                top +
                worldSize.Y;

            return new RectangleF(
                left,
                top,
                right - left,
                bottom - top);
        }
    }

    public SpriteDrawer WithSprite(
        string assetPath)
    {
#pragma warning disable CS0618
        SpritePath = assetPath;
#pragma warning restore CS0618

        return this;
    }

    public SpriteDrawer WithTint(
        Color tint)
    {
        Tint = tint;

        return this;
    }

    public SpriteDrawer WithOpacity(
        float opacity)
    {
        Opacity =
            MathHelper.Clamp(
                opacity,
                0f,
                1f);

        return this;
    }

    public SpriteDrawer WithPivot(
        PivotType type)
    {
        PivotType = type;

        return this;
    }

    public SpriteDrawer WithPivot(
        Vector2 pivotInPixels)
    {
        PivotType =
            PivotType.Custom;

        Pivot =
            pivotInPixels;

        return this;
    }

    public SpriteDrawer SetSprite(
        Sprite sprite)
    {
        Sprite = sprite;

        return this;
    }

    protected override void OnDraw()
    {
        ArgumentNullException.ThrowIfNull(
            Sprite);

        ArgumentNullException.ThrowIfNull(
            Sprite.Texture);

        Core.SpriteBatch.DrawWorldSprite(
            Sprite.Texture,
            GetDrawPosition(),
            Sprite.SourceRect,
            GetPremultipliedTint(),
            GetDrawRotation(),
            // SpriteBatch applies the draw scale to this pixel-space origin.
            GetOriginToUse(),
            GetSpriteDrawScale(),
            GetSpriteEffects());
    }

    private Color GetPremultipliedTint()
    {
        // SpriteBatch's default blend state expects premultiplied colors. Color's
        // scalar operator does not premultiply an explicitly authored alpha into RGB.
        // Without this, (255,255,255,0) appears additive instead of transparent.
        var alpha = Math.Clamp(Tint.A / 255f * Opacity, 0f, 1f);
        return new Color(
            Tint.R / 255f * alpha,
            Tint.G / 255f * alpha,
            Tint.B / 255f * alpha,
            alpha);
    }

    protected virtual Vector2 GetDrawPosition()
    {
        return Transform.WorldPosition2D;
    }

    protected virtual float GetDrawRotation()
    {
        return Transform.WorldRotation2D;
    }

    protected virtual Vector2 GetDrawScale()
    {
        return Transform.WorldScale2D;
    }

    protected virtual Vector2 GetSpriteDrawScale()
    {
        return GetDrawScale() / Sprite.PixelsPerUnit;
    }

    /// <summary>
    /// Gets the origin relative to the sprite's source rectangle, in pixels.
    /// </summary>
    protected virtual Vector2 GetOriginToUse()
    {
        var origin =
            Pivot;

        if (PivotType != PivotType.Custom)
        {
            var relative =
                PivotHelper.GetRelativePivot(
                    PivotType);

            origin =
                new Vector2(
                    relative.X *
                    Sprite.SourceRect.Width,

                    relative.Y *
                    Sprite.SourceRect.Height);
        }

        if (FlipX)
        {
            origin.X =
                Sprite.SourceRect.Width -
                origin.X;
        }

        return origin;
    }

    /// <summary>
    /// Converts the pixel-space origin to the offset used by world-space bounds.
    /// </summary>
    protected virtual Vector2 GetWorldOriginToUse()
    {
        return GetOriginToUse() * GetSpriteDrawScale();
    }

    protected virtual SpriteEffects GetSpriteEffects()
    {
        return FlipX
            ? SpriteEffects.FlipHorizontally
            : SpriteEffects.None;
    }

    public override void OnDebugDraw()
    {
        if (Sprite is null)
            return;

        Core.SpriteBatch.DrawHollowRectangle(
            Bounds,
            Color.Yellow,
            Scene.MainCamera
                .WorldUnitsPerScreenPixel);
    }

    public override void OnDestroyed()
    {
        Sprite = null;
    }
}
