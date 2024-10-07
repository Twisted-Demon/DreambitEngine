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
    public Rectangle? Frame { get; set; } = null;

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

        var spriteFrame = Frame;

        if (spriteFrame == null)
            _spriteSheet.TryGetFrame(CurrentFrameIndex, out spriteFrame);

        if (spriteFrame == null) return;

        var originToUse = Pivot;

        if (OriginType != SpriteOrigin.Custom)
        {
            var relative = SpriteSheet.GetRelativeOrigin(OriginType);
            originToUse = new Vector2(relative.X * (float)spriteFrame?.Width, relative.Y * (float)spriteFrame?.Height);
        }

        var depth = Transform.Position.Y / Core.Instance.GraphicsDevice.Viewport.Height;

        Core.SpriteBatch.Draw(
            _spriteSheet.Texture,
            Transform.PositionToVec2,
            spriteFrame,
            Color,
            Transform.SingleRotation,
            originToUse,
            Transform.ScaleToVec2,
            SpriteEffects.None,
            depth
        );
    }

    public override void OnDebugDraw()
    {
        var spriteFrame = Frame;
        
        if (spriteFrame == null)
            _spriteSheet.TryGetFrame(CurrentFrameIndex, out spriteFrame);
        
        if (spriteFrame == null) return;
        
        var originToUse = Pivot;
        
        if (OriginType != SpriteOrigin.Custom)
        {
            var relative = SpriteSheet.GetRelativeOrigin(OriginType);
            originToUse = new Vector2(relative.X * (float)spriteFrame?.Width, relative.Y * (float)spriteFrame?.Height);
        }

        var rect = new Rectangle(
            (int)(Transform.Position.X - (originToUse.X)),
            (int)(Transform.Position.Y - (originToUse.Y)),
            (int)(spriteFrame?.Width),
            (int)(spriteFrame?.Height)
        );
        
        Core.SpriteBatch.DrawHollowRectangle(rect, Color.Yellow);
        Core.SpriteBatch.DrawLine(Transform.PositionToVec2, Transform.PositionToVec2, Color.Red);
    }
}