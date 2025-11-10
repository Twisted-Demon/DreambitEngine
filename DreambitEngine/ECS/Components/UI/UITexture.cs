using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.ECS;

[Require(typeof(Canvas))]
public class UITexture : UIElement
{
    private string _texturePath = string.Empty;

    public string TexturePath
    {
        get => _texturePath;
        set
        {
            if (_texturePath == value)
                return;

            _texturePath = value;
            Texture = Resources.LoadAsset<Texture2D>(_texturePath);
        }
    }

    public Texture2D Texture { get; private set; }

    public bool HorizontalFlip { get; set; } = false;
    public bool VerticalFlip { get; set; } = false;

    public float Width { get; set; }
    public float Height { get; set; }

    public float Margin { get; set; } = 0;

    public bool CanScale { get; set; } = true;

    public override Rectangle Bounds => GetDestinationRect();


    public static UITexture Create(Canvas canvas, string texturePath, PivotType pivotType = PivotType.Center,
        Vector2? pivot = null)
    {
        var component = canvas.CreateUIElement<UITexture>();
        component._texturePath = texturePath;
        component.Texture = Resources.LoadAsset<Texture2D>(texturePath);
        component.PivotType = pivotType;

        if (pivot != null)
            component.Pivot = new Vector2(pivot.Value.X, pivot.Value.Y);

        return component;
    }

    public override void OnDrawUI()
    {
        if (Texture == null) return;

        Core.SpriteBatch.Draw(
            Texture,
            GetDestinationRect(),
            null,
            Color * Alpha,
            Transform.WorldZRotation,
            Vector2.Zero,
            SpriteEffects.None,
            0f
        );
    }

    private Rectangle GetSourceRect()
    {
        return new Rectangle(0, 0, Texture.Width, Texture.Height);
    }

    private Rectangle GetDestinationRect()
    {
        var screenPos = GetScreenPos();
        var marginSize = Canvas.ConvertToScreenSize(new Vector2(Margin, Margin));
        var size = CanScale
            ? Canvas.ConvertToScreenSize(new Vector2(Width, Height))
            : new Vector2(Texture.Width, Texture.Height);

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


        size.X -= marginSize.X;
        size.Y -= marginSize.Y;

        var rect = new RectangleF(
            screenPos.X - offset.X + Margin,
            screenPos.Y - offset.Y + Margin,
            size.X - Margin,
            size.Y - Margin
        ).ToRectangle();

        return rect;
    }
}