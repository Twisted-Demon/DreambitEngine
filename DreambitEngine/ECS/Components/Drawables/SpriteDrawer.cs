using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.ECS;

public class SpriteDrawer : DrawableComponent<SpriteDrawer>
{
    private string _spritePath;
    public Color Tint { get; internal set; } = Color.White;
    public float Opacity { get; internal set; } = 1.0f;
    public Vector2 Pivot { get; internal set; } = Vector2.Zero;
    public PivotType PivotType { get; internal set; } = PivotType.Center;
    public Sprite Sprite { get; set; }

    public string SpritePath
    {
        get => _spritePath;
        set
        {
            if (_spritePath == value) return;

            _spritePath = value;
            Sprite = Resources.LoadAsset<Sprite>(_spritePath);
        }
    }

    public bool FlipX { get; set; } = false;

    public override RectangleF Bounds
    {
        get
        {
            if (Sprite is null)
                return new RectangleF(0, 0, 1, 1);

            var origin = GetOriginToUse();

            var drawScale =
                Scene.MainCamera.GetSpriteDrawScale(
                    Transform.WorldScaleToVec2);

            var worldOrigin =
                origin * drawScale;

            var worldSize =
                new Vector2(
                    Sprite.SourceRect.Width,
                    Sprite.SourceRect.Height) * drawScale;

            var position =
                Transform.WorldPosToVec2;

            var left =
                position.X - worldOrigin.X;

            var top =
                position.Y - worldOrigin.Y;

            var right =
                left + worldSize.X;

            var bottom =
                top + worldSize.Y;

            return new RectangleF(
                left,
                top,
                right - left,
                bottom - top);
        }
    }

    public SpriteDrawer WithSprite(string assetPath)
    {
        SpritePath = assetPath;
        return this;
    }

    public SpriteDrawer WithTint(Color tint)
    {
        Tint = tint;
        return this;
    }

    public SpriteDrawer WithOpacity(float a)
    {
        Opacity = MathHelper.Clamp(a, 0f, 1f);
        return this;
    }

    public SpriteDrawer WithPivot(PivotType type)
    {
        PivotType = type;
        return this;
    }

    public SpriteDrawer WithPivot(Vector2 pivot)
    {
        PivotType = PivotType.Custom;
        Pivot = pivot;
        return this;
    }

    public SpriteDrawer SetSprite(Sprite sprite)
    {
        Sprite = sprite;
        return this;
    }

    public override void OnDraw()
    {
        if (Sprite?.Texture == null)
        {
            Logger.Warn(
                "Entity {0} is missing a texture",
                Entity.Name);

            return;
        }

        var originToUse = Pivot;

        if (PivotType != PivotType.Custom)
        {
            var relative =
                PivotHelper.GetRelativePivot(PivotType);

            originToUse = new Vector2(
                relative.X * Sprite.SourceRect.Width,
                relative.Y * Sprite.SourceRect.Height);
        }

        var spriteEffect = SpriteEffects.None;

        if (FlipX)
        {
            spriteEffect |= SpriteEffects.FlipHorizontally;

            originToUse.X =
                Sprite.SourceRect.Width - originToUse.X;
        }

        Core.SpriteBatch.DrawWorldSprite(
            Scene.MainCamera,
            Sprite.Texture,
            Transform.WorldPosToVec2,
            Sprite.SourceRect,
            Tint * Opacity,
            Transform.WorldZRotation,
            originToUse,
            Transform.WorldScaleToVec2,
            spriteEffect,
            0f);
    }

    public override void OnDebugDraw()
    {
        if (Sprite is null)
            return;

        //Core.SpriteBatch.DrawHollowRectangle(
        //    Transform.WorldPosToVec2,
        //    new Vector2(Sprite.SourceRect.Width, Sprite.SourceRect.Height),
        //    Color.Yellow,
        //    Transform.WorldZRotation,
        //    GetOriginToUse(),
        //    GetSpriteScale(),
        //    1f / Scene.MainCamera.PixelsPerUnit);
        
        Core.SpriteBatch.DrawHollowRectangle(
            Bounds, Color.Yellow, Scene.MainCamera.WorldUnitsPerTexturePixel);

        Core.SpriteBatch.DrawPoint(Transform.WorldPosToVec2, Color.Red, 3f * Scene.MainCamera.WorldUnitsPerTexturePixel);
    }
    
    private Vector2 GetOriginToUse()
    {
        var origin = Pivot;

        if (PivotType != PivotType.Custom)
        {
            var relative = PivotHelper.GetRelativePivot(PivotType);
            origin = new Vector2(
                relative.X * Sprite.SourceRect.Width,
                relative.Y * Sprite.SourceRect.Height);
        }

        if (FlipX)
            origin.X = Sprite.SourceRect.Width - origin.X;

        return origin;
    }

    private Vector2 GetSpriteScale()
    {
        return Transform.WorldScaleToVec2 / Scene.MainCamera.PixelsPerUnit;
    }

    public override void OnDestroyed()
    {
        Sprite = null;
    }
}