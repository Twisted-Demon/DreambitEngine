using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.ECS;

[BlueprintType(nameof(SpriteDrawer))]
public class SpriteDrawer :
    DrawableComponent<SpriteDrawer>
{
    private string _spritePath;

    public Color Tint { get; internal set; } =
        Color.White;

    public float Opacity { get; internal set; } =
        1.0f;

    public Vector2 Pivot { get; internal set; } =
        Vector2.Zero;

    public PivotType PivotType { get; internal set; } =
        PivotType.Center;

    public Sprite Sprite { get; set; }

    public string SpritePath
    {
        get => _spritePath;

        set
        {
            if (_spritePath == value)
                return;

            _spritePath = value;

            Sprite =
                Resources.LoadAsset<Sprite>(
                    _spritePath);
        }
    }

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

            var origin =
                GetOriginToUse();

            var drawScale =
                Scene.MainCamera
                    .GetSpriteDrawScale(
                        GetDrawScale());

            var worldOrigin =
                origin *
                drawScale;

            var worldSize =
                new Vector2(
                    Sprite.SourceRect.Width,
                    Sprite.SourceRect.Height) *
                drawScale;

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
        SpritePath = assetPath;

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
        Vector2 pivot)
    {
        PivotType =
            PivotType.Custom;

        Pivot =
            pivot;

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
            Scene.MainCamera,
            Sprite.Texture,
            GetDrawPosition(),
            Sprite.SourceRect,
            Tint * Opacity,
            GetDrawRotation(),
            GetOriginToUse(),
            GetDrawScale(),
            GetSpriteEffects());
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
                .WorldUnitsPerTexturePixel);
    }

    public override void OnDestroyed()
    {
        Sprite = null;
    }
}
