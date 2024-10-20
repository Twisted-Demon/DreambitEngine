using Microsoft.Xna.Framework;

namespace PixelariaEngine.ECS;

public class UINineSlice : UIElement
{
    private SpriteSheet _spriteSheet;
    public float Width { get; set; }
    public float Height { get; set; }


    public static UINineSlice Create(Canvas canvas, float width, float height, string texturePath)
    {
        var ns = canvas.CreateUIElement<UINineSlice>();
        ns.Width = width;
        ns.Height = height;

        var spriteSheet = SpriteSheet.Create(3, 3, texturePath);

        if (spriteSheet == null) return ns;

        ns._spriteSheet = spriteSheet;

        return ns;
    }

    public override void OnDrawUI()
    {
        if (_spriteSheet == null) return;

        //get the size 
        var size = Canvas.ConvertToScreenSize(new Vector2(Width, Height));
        var pos = GetScreenPos();

        //calculate the offset
        Vector2 offset;

        if (PivotType != PivotType.Custom)
        {
            offset = PivotHelper.GetRelativePivot(PivotType);

            offset = new Vector2(offset.X * size.X, offset.Y * size.Y);
        }
        else
        {
            offset = Pivot;
        }

        //Main Destination Rectangle
        var dr = new RectangleF(
            pos.X - offset.X,
            pos.Y - offset.Y,
            size.X,
            size.Y
        ).ToRectangle();

        //Define the source rectangles
        if (!_spriteSheet.TryGetFrame(0, out var topLeftSource)) return;
        if (!_spriteSheet.TryGetFrame(1, out var topMiddleSource)) return;
        if (!_spriteSheet.TryGetFrame(2, out var topRightSource)) return;

        if (!_spriteSheet.TryGetFrame(3, out var middleLeftSource)) return;
        if (!_spriteSheet.TryGetFrame(4, out var middleSource)) return;
        if (!_spriteSheet.TryGetFrame(5, out var middleRightSource)) return;

        if (!_spriteSheet.TryGetFrame(6, out var bottomLeftSource)) return;
        if (!_spriteSheet.TryGetFrame(7, out var bottomMiddleSource)) return;
        if (!_spriteSheet.TryGetFrame(8, out var bottomRightSource)) return;

        var middleWidth = (int)(size.X - (topLeftSource.Width + topRightSource.Width));
        var middleHeight = (int)(size.Y - (topMiddleSource.Height + topMiddleSource.Height));

        //Define the destination rectangles
        var topLeftDest = new Rectangle(dr.Left, dr.Top, topLeftSource.Width, topLeftSource.Height);
        var topMiddleDest = new Rectangle(dr.Left + topLeftSource.Width, dr.Top, middleWidth, topMiddleSource.Height);
        var topRightDest = new Rectangle(dr.Right - topRightSource.Width, dr.Top, topRightSource.Width,
            topRightSource.Height);

        var middleLeftDest = new Rectangle(dr.Left, dr.Top + topLeftSource.Height, topLeftSource.Width, middleHeight);
        var middleDest = new Rectangle(dr.Left + topLeftSource.Width, dr.Top + topLeftSource.Height, middleWidth,
            middleHeight);
        var middleRightDest = new Rectangle(dr.Right - topRightSource.Width, dr.Top + topLeftSource.Height,
            topRightSource.Width, middleHeight);

        var bottomLeftDest = new Rectangle(dr.Left, dr.Bottom - bottomLeftSource.Height, bottomLeftSource.Width,
            bottomLeftSource.Height);
        var bottomMiddleDest = new Rectangle(dr.Left + bottomRightSource.Width, dr.Bottom - bottomMiddleSource.Height,
            middleWidth, bottomMiddleSource.Height);
        var bottomRightDest = new Rectangle(dr.Right - bottomRightSource.Width, dr.Bottom - bottomMiddleSource.Height,
            bottomRightSource.Width, bottomMiddleSource.Height);


        //draw the slices
        Core.SpriteBatch.Draw(_spriteSheet.Texture, topLeftDest, topLeftSource, Color * Alpha);
        Core.SpriteBatch.Draw(_spriteSheet.Texture, topMiddleDest, topMiddleSource, Color * Alpha);
        Core.SpriteBatch.Draw(_spriteSheet.Texture, topRightDest, topRightSource, Color * Alpha);

        Core.SpriteBatch.Draw(_spriteSheet.Texture, middleLeftDest, middleLeftSource, Color * Alpha);
        Core.SpriteBatch.Draw(_spriteSheet.Texture, middleDest, middleSource, Color * Alpha);
        Core.SpriteBatch.Draw(_spriteSheet.Texture, middleRightDest, middleRightSource, Color * Alpha);

        Core.SpriteBatch.Draw(_spriteSheet.Texture, bottomLeftDest, bottomLeftSource, Color * Alpha);
        Core.SpriteBatch.Draw(_spriteSheet.Texture, bottomMiddleDest, bottomMiddleSource, Color * Alpha);
        Core.SpriteBatch.Draw(_spriteSheet.Texture, bottomRightDest, bottomRightSource, Color * Alpha);
    }
}