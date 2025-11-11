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

    public override Rectangle Bounds
    {
        get
        {
            if (Sprite is null) return new Rectangle(0, 0, 1, 1);
            
            var originToUse = Pivot;

            if (PivotType != PivotType.Custom)
            {
                var relative = PivotHelper.GetRelativePivot(PivotType);
                originToUse = new Vector2(relative.X * Sprite.SourceRect.Width, relative.Y * Sprite.SourceRect.Height);
            }

            var rect = new Rectangle(
                (int)(Transform.WorldPosition.X - originToUse.X),
                (int)(Transform.WorldPosition.Y - originToUse.Y),
                Sprite.SourceRect.Width,
                Sprite.SourceRect.Height
            );

            return rect;
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
            Logger.Warn("Entity {0} is missing a texture", Entity.Name);
            return;
        }

        var originToUse = Pivot;

        if (PivotType != PivotType.Custom)
        {
            var relative = PivotHelper.GetRelativePivot(PivotType);
            originToUse = new Vector2(relative.X * Sprite.SourceRect.Width, relative.Y * Sprite.SourceRect.Height);
        }

        //var depth = Transform.WorldPosition.Y / float.MaxValue;

        var spriteEffect = SpriteEffects.None;

        if (FlipX)
        {
            spriteEffect |= SpriteEffects.FlipHorizontally;
            originToUse.X = Sprite.SourceRect.Width - originToUse.X;
        }

        Core.SpriteBatch.Draw(
            Sprite.Texture,
            Transform.WorldPosToVec2,
            Sprite.SourceRect,
            Tint * Opacity,
            Transform.WorldZRotation,
            originToUse,
            Transform.WorldScaleToVec2,
            spriteEffect,
            0f
        );
    }

    public override void OnDebugDraw()
    {
        var vec2 = Transform.WorldPosToVec2;
        Core.SpriteBatch.DrawHollowRectangle(Bounds, Color.Yellow);
        Core.SpriteBatch.DrawPoint(vec2, Color.Red, 3.0f);
    }

    public override void OnDestroyed()
    {
        Sprite = null;
    }
}