using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.ECS;

public class TextureDrawer : DrawableComponent
{
    private int _pixelsPerUnit = 1;
    
    [DreambitSerialize]
    public int PixelsPerUnit { get =>  _pixelsPerUnit;
        init
        {
            if(value < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(value), value, $"Pixels PerUnit must be greater than or equal to 1.");
            
            _pixelsPerUnit = value;
        } 
    }
    
    [DreambitSerialize]
    public Color Tint { get; internal set; } = new(255, 255, 255, 255);

    [DreambitSerialize]
    public float Opacity { get; internal set; }
        = 1.0f;
    
    [DreambitSerialize]
    public Vector2 Pivot { get; internal set; } = Vector2.Zero;
    
    [DreambitSerialize]
    public PivotType PivotType { get; internal set; } =
        PivotType.Center;
    
    [DreambitSerialize]
    public TextureAsset Texture { get; set; }
    
    [DreambitSerialize]
    public Rectangle SourceRectangle { get; internal set; }
    
    [DreambitSerialize]
    
    public bool FlipX { get; set; }
    
    [DreambitSerialize]
    public bool FlipY { get; set; }

    public override RectangleF Bounds
    {
        get
        {
            Vector2 position;

            if (Texture is null)
            {
                position =
                    GetDrawPosition();

                return new RectangleF(
                    position.X,
                    position.Y,
                    1f,
                    1f);
            }

            var worldOrigin = GetWorldOriginToUse();

            var worldSize = new Vector2(
                SourceRectangle.Width,
                SourceRectangle.Height) * GetSpriteDrawScale();

            position = GetDrawPosition();
            
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

    public TextureDrawer WithTexture(TextureAsset texture)
    {
        Texture = texture;
        return this;
    }

    public TextureDrawer WithTint(Color color)
    {
        Tint = color;
        return this;
    }

    public TextureDrawer WithOpacity(float opacity)
    {
        Opacity = opacity;
        return this;
    }

    public TextureDrawer WithPivot(PivotType pivot)
    {
        PivotType = pivot;
        return this;
    }

    public TextureDrawer WithPivot(Vector2 pivot)
    {
        Pivot = pivot;
        return this;
    }

    protected override void OnDraw()
    {
        ArgumentNullException.ThrowIfNull(Texture);
        
        Core.SpriteBatch.DrawWorldSprite(
            Texture,
            GetDrawPosition(),
            SourceRectangle,
            GetPremultipliedTint(),
            GetDrawRotation(),
            GetOriginToUse(),
            GetSpriteDrawScale(),
            GetSpriteEffects()
            );
    }

    private SpriteEffects GetSpriteEffects()
    {
        var effects = SpriteEffects.None;

        if (FlipX)
            effects |= SpriteEffects.FlipHorizontally;
        if (FlipY)
            effects |= SpriteEffects.FlipVertically;
        
        return effects;
}

    public Vector2 GetOriginToUse()
    {
        var origin = Pivot;
        
        if(PivotType != PivotType.Custom)
        {
            var relative = PivotHelper.GetRelativePivot(PivotType);


            origin = new Vector2(
                relative.X * SourceRectangle.Width,
                relative.Y * SourceRectangle.Height);
        }

        if (FlipX)
        {
            origin.X = SourceRectangle.Width - origin.X;
        }

        if (FlipY)
        {
            origin.Y = SourceRectangle.Height - origin.Y;
        }

        return origin;
    }

    private float GetDrawRotation()
    {
        return Transform.WorldRotation2D;
    }

    private Vector2 GetSpriteDrawScale()
    {
        return Transform.WorldScale2D / PixelsPerUnit;
    }

    private Vector2 GetWorldOriginToUse()
    {
        return GetOriginToUse() * GetSpriteDrawScale();
    }

    private Vector2 GetDrawPosition()
    {
        return Transform.WorldPosition2D;
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
}