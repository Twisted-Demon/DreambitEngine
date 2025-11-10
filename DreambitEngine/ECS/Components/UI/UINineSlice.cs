using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class UINineSlice : UIElement
{
    private SpriteSheet _spriteSheet;

    private string _texturePath;

    public string TexturePath
    {
        get => _texturePath;
        set
        {
            if (_texturePath == value) return;
            _texturePath = value;

            _spriteSheet = SpriteSheet.Create(3, 3, _texturePath);
        }
    }

    public float Width { get; set; } = 1f;
    public float Height { get; set; } = 1f;


    public static UINineSlice Create(Canvas canvas, float width, float height, string texturePath)
    {
        var ns = canvas.CreateUIElement<UINineSlice>();
        ns.Width = width;
        ns.Height = height;

        ns.TexturePath = texturePath;

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

        var middleWidth = (int)(size.X - (topLeftSource.Source.Width + topRightSource.Source.Width));
        var middleHeight = (int)(size.Y - (topMiddleSource.Source.Height + topMiddleSource.Source.Height));

        //Define the destination rectangles
        var topLeftDest = new Rectangle(dr.Left, dr.Top, topLeftSource.Source.Width, topLeftSource.Source.Height);
        var topMiddleDest = new Rectangle(dr.Left + topLeftSource.Source.Width, dr.Top, middleWidth, topMiddleSource.Source.Height);
        var topRightDest = new Rectangle(dr.Right - topRightSource.Source.Width, dr.Top, topRightSource.Source.Width,
            topRightSource.Source.Height);

        var middleLeftDest = new Rectangle(dr.Left, dr.Top + topLeftSource.Source.Height, topLeftSource.Source.Width, middleHeight);
        var middleDest = new Rectangle(dr.Left + topLeftSource.Source.Width, dr.Top + topLeftSource.Source.Height, middleWidth,
            middleHeight);
        var middleRightDest = new Rectangle(dr.Right - topRightSource.Source.Width, dr.Top + topLeftSource.Source.Height,
            topRightSource.Source.Width, middleHeight);

        var bottomLeftDest = new Rectangle(dr.Left, dr.Bottom - bottomLeftSource.Source.Height, bottomLeftSource.Source.Width,
            bottomLeftSource.Source.Height);
        var bottomMiddleDest = new Rectangle(dr.Left + bottomRightSource.Source.Width, dr.Bottom - bottomMiddleSource.Source.Height,
            middleWidth, bottomMiddleSource.Source.Height);
        var bottomRightDest = new Rectangle(dr.Right - bottomRightSource.Source.Width, dr.Bottom - bottomMiddleSource.Source.Height,
            bottomRightSource.Source.Width, bottomMiddleSource.Source.Height);


        //draw the slices
        Core.SpriteBatch.Draw(_spriteSheet.Texture, topLeftDest, topLeftSource.Source, Color * Alpha);
        Core.SpriteBatch.Draw(_spriteSheet.Texture, topMiddleDest, topMiddleSource.Source, Color * Alpha);
        Core.SpriteBatch.Draw(_spriteSheet.Texture, topRightDest, topRightSource.Source, Color * Alpha);

        Core.SpriteBatch.Draw(_spriteSheet.Texture, middleLeftDest, middleLeftSource.Source, Color * Alpha);
        Core.SpriteBatch.Draw(_spriteSheet.Texture, middleDest, middleSource.Source, Color * Alpha);
        Core.SpriteBatch.Draw(_spriteSheet.Texture, middleRightDest, middleRightSource.Source, Color * Alpha);

        Core.SpriteBatch.Draw(_spriteSheet.Texture, bottomLeftDest, bottomLeftSource.Source, Color * Alpha);
        Core.SpriteBatch.Draw(_spriteSheet.Texture, bottomMiddleDest, bottomMiddleSource.Source, Color * Alpha);
        Core.SpriteBatch.Draw(_spriteSheet.Texture, bottomRightDest, bottomRightSource.Source, Color * Alpha);
    }
}