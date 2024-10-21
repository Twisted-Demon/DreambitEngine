using LDtk;
using Microsoft.Xna.Framework;

namespace Dreambit;

public static class RectangleExtensions
{
    public static Rectangle ToMonoGameRect(this TilesetRectangle tileSetRect)
    {
        return new Rectangle(tileSetRect.X, tileSetRect.Y, tileSetRect.W, tileSetRect.H);
    }
}