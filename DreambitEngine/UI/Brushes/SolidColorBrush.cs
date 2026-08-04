using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>Fills the owning control's bounds with its supplied tint.</summary>
public sealed class SolidColorBrush : UiBrush
{
    /// <inheritdoc />
    public override Point MinimumSize => new(1, 1);

    /// <inheritdoc />
    public override void Draw(Rectangle bounds, Color tint)
    {
        var boundsF = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);

        Graphics.SpriteBatch.DrawFilledRectangle(boundsF, tint);
    }
}