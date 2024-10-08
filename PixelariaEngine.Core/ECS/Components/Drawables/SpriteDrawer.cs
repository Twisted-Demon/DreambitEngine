using LDtk;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelariaEngine.Graphics;

namespace PixelariaEngine.ECS;

public class SpriteDrawer : DrawableComponent<SpriteDrawer>
{
    private SpriteSheet _spriteSheet;

    private string _spriteSheetPath = string.Empty;
    public Color Color { get; set; } = Color.White;
    public Vector2 Pivot { get; set; } = Vector2.Zero;
    public SpriteOrigin OriginType { get; set; } = SpriteOrigin.Center;
    public int CurrentFrameIndex { get; set; }
    public TilesetRectangle FrameRect { get; set; } = null;

    public bool IsHorizontalFlip { get; set; } = false;

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
        _spriteSheet = Resources.Load<SpriteSheet>(_spriteSheetPath);
    }

    public override void OnDraw()
    {
        if (_spriteSheet?.Texture == null)
        {
            Logger.Warn("Entity {0} is missing a texture", Entity.Name);
            return;
        }

        var spriteFrame = GetDrawRect();

        var originToUse = Pivot;

        if (OriginType != SpriteOrigin.Custom)
        {
            var relative = SpriteSheet.GetRelativeOrigin(OriginType);
            originToUse = new Vector2(relative.X * spriteFrame.Width, relative.Y * spriteFrame.Height);
        }

        var depth = Transform.WorldPosition.Y / Core.Instance.GraphicsDevice.Viewport.Height;

        var spriteEffect = SpriteEffects.None;
        
        if (IsHorizontalFlip)
            spriteEffect |= SpriteEffects.FlipHorizontally;

        Core.SpriteBatch.Draw(
            _spriteSheet.Texture,
            Transform.WorldPosToVec2,
            spriteFrame,
            Color,
            Transform.WorldZRotation,
            originToUse,
            Transform.WorldScaleToVec2,
            spriteEffect,
            depth
        );
    }
    
    private Rectangle GetDrawRect()
    {
        if(FrameRect != null)
            return new Rectangle(FrameRect.X, FrameRect.Y, FrameRect.W, FrameRect.H);
        
        _spriteSheet.TryGetFrame(CurrentFrameIndex, out var spriteFrame);

        return spriteFrame;
    }

    public override void OnDebugDraw()
    {
        var spriteFrame = GetDrawRect();
        
        var originToUse = Pivot;
        
        if (OriginType != SpriteOrigin.Custom)
        {
            var relative = SpriteSheet.GetRelativeOrigin(OriginType);
            originToUse = new Vector2(relative.X * spriteFrame.Width, relative.Y * spriteFrame.Height);
        }

        var rect = new Rectangle(
            (int)(Transform.WorldPosition.X - originToUse.X),
            (int)(Transform.WorldPosition.Y - originToUse.Y),
            spriteFrame.Width,
            spriteFrame.Height
        );

        var thickness = 3.0f;
        var vec2 = Transform.WorldPosToVec2 - new Vector2(thickness / 2, thickness / 2);
        Core.SpriteBatch.DrawHollowRectangle(rect, Color.Yellow);
        Core.SpriteBatch.DrawPoint(vec2, Color.Red, thickness);
    }
}