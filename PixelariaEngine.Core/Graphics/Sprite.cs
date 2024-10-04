using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine.Graphics;

public class Sprite : DisposableObject
{
    public Texture2D Texture { get; set; }
    public string TexturePath { get; set; }
    
    public Vector2 Origin { get; set; }
    public Rectangle? SourceRectangle { get; set; }

    public static Sprite FromTexture2D(Texture2D texture, Vector2? origin = null, Rectangle? sourceRectangle = null)
    {
        return new Sprite
        {
            Texture = texture,
            TexturePath = texture.Name,
            Origin = origin ?? new Vector2((float)texture.Width / 2, (float)texture.Height / 2),
            SourceRectangle = sourceRectangle
        };
    }
}