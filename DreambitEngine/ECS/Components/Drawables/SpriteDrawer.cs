using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.ECS;

public class SpriteDrawer : DrawableComponent<SpriteDrawer>
{
    private SpriteSheet _spriteSheet;

    private string _spriteSheetPath = string.Empty;
    public Color Tint { get; private set; } = Color.White;
    public float Opacity { get; private set; } = 1.0f;
    public Vector2 Pivot { get; private set; } = Vector2.Zero;
    public PivotType PivotType { get; private set; } = PivotType.Center;
    public int CurrentFrameIndex { get; private set; }
    public Rectangle? FrameRect { get; private set; } = null;

    public bool FlipX { get; set; } = false;
    
    public SpriteDrawer WithSprite(string assetPath, int frame = 0) { SpriteSheetPath = assetPath; SetFrame(frame); return this; }
    public SpriteDrawer WithTint(Color tint) { Tint = tint; return this; }
    public SpriteDrawer WithOpacity(float a) { Opacity = MathHelper.Clamp(a, 0f, 1f); return this; }
    public SpriteDrawer WithPivot(PivotType type) { PivotType = type; return this; }
    public SpriteDrawer WithPivot(Vector2 pivot) { PivotType = PivotType.Custom; Pivot = pivot; return this; }
    public SpriteDrawer SetFrame(int index) { CurrentFrameIndex = index; return this; }
    public SpriteDrawer SetFrameRect(Rectangle rect) { FrameRect = rect; return this; }
    

    public override Rectangle Bounds
    {
        get
        {
            var spriteFrame = GetDrawRect();

            var originToUse = Pivot;

            if (PivotType != PivotType.Custom)
            {
                var relative = PivotHelper.GetRelativePivot(PivotType);
                originToUse = new Vector2(relative.X * spriteFrame.Width, relative.Y * spriteFrame.Height);
            }

            var rect = new Rectangle(
                (int)(Transform.WorldPosition.X - originToUse.X),
                (int)(Transform.WorldPosition.Y - originToUse.Y),
                spriteFrame.Width,
                spriteFrame.Height
            );

            return rect;
        }
    }

    public string SpriteSheetPath
    {
        get => _spriteSheetPath;
        set
        {
            if (_spriteSheetPath == value) return;
            OnSpriteSheetPathChanged(value);
        }
    }

    public SpriteSheet SpriteSheet
    {
        get => _spriteSheet;
        set
        {
            if (_spriteSheet == value) return;
            _spriteSheet = value;
            _spriteSheetPath = _spriteSheet.AssetName;
        }
    }

    private void OnSpriteSheetPathChanged(string newPath)
    {
        _spriteSheetPath = newPath;
        _spriteSheet = Resources.LoadAsset<SpriteSheet>(_spriteSheetPath);
    }


    public override void OnDraw()
    {
        if (_spriteSheet is null)
            return; 
        
        if (_spriteSheet?.Texture == null)
        {
            Logger.Warn("Entity {0} is missing a texture", Entity.Name);
            return;
        }

        var spriteFrame = GetDrawRect();

        var originToUse = Pivot;

        if (PivotType != PivotType.Custom)
        {
            var relative = PivotHelper.GetRelativePivot(PivotType);
            originToUse = new Vector2(relative.X * spriteFrame.Width, relative.Y * spriteFrame.Height);
        }

        //var depth = Transform.WorldPosition.Y / float.MaxValue;

        var spriteEffect = SpriteEffects.None;

        if (FlipX)
        {
            spriteEffect |= SpriteEffects.FlipHorizontally;
            originToUse.X = spriteFrame.Width - originToUse.X;
        }

        Core.SpriteBatch.Draw(
            _spriteSheet.Texture,
            Transform.WorldPosToVec2,
            spriteFrame,
            Tint * Opacity,
            Transform.WorldZRotation,
            originToUse,
            Transform.WorldScaleToVec2,
            spriteEffect,
            0f
        );
    }

    private Rectangle GetDrawRect()
    {
        if (FrameRect != null)
            return FrameRect.Value!;

        if (_spriteSheet is null)
            return new Rectangle(0, 0, 0, 0);

        _spriteSheet.TryGetFrame(CurrentFrameIndex, out var spriteFrame);

        return spriteFrame;
    }

    public override void OnDebugDraw()
    {
        var vec2 = Transform.WorldPosToVec2;
        Core.SpriteBatch.DrawHollowRectangle(Bounds, Color.Yellow);
        Core.SpriteBatch.DrawPoint(vec2, Color.Red, 3.0f);
    }

    public override void OnDestroyed()
    {
        FrameRect = null;
        _spriteSheet = null;
    }
}