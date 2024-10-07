using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine;

public static class SpriteBatchExtensions
{
    private static Texture2D _pixelTexture;

    private static void EnsurePixelTextureExists(GraphicsDevice graphicsDevice)
    {
        if (_pixelTexture == null)
        {
            _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
        }
    }

    // Draw a Line
    public static void DrawLine(this SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness = 1f)
    {
        EnsurePixelTextureExists(spriteBatch.GraphicsDevice);
        
        var edge = end - start;
        var angle = (float)Math.Atan2(edge.Y, edge.X);

        spriteBatch.Draw(_pixelTexture, 
            new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), (int)thickness), 
            null, 
            color, 
            angle, 
            Vector2.Zero, 
            SpriteEffects.None, 
            0);
    }
    
    // Draw a Filled Rectangle
    public static void DrawFilledRectangle(this SpriteBatch spriteBatch, Rectangle rectangle, Color color)
    {
        EnsurePixelTextureExists(spriteBatch.GraphicsDevice);
        
        spriteBatch.Draw(_pixelTexture, rectangle, color);
    }

    // Draw a Hollow Rectangle (outline)
    public static void DrawHollowRectangle(this SpriteBatch spriteBatch, Rectangle rectangle, Color color, float thickness = 1f)
    {
        // Top
        spriteBatch.DrawLine(new Vector2(rectangle.Left, rectangle.Top), new Vector2(rectangle.Right, rectangle.Top), color, thickness);
        // Left
        spriteBatch.DrawLine(new Vector2(rectangle.Left, rectangle.Top), new Vector2(rectangle.Left, rectangle.Bottom), color, thickness);
        // Right
        spriteBatch.DrawLine(new Vector2(rectangle.Right, rectangle.Top), new Vector2(rectangle.Right, rectangle.Bottom), color, thickness);
        // Bottom
        spriteBatch.DrawLine(new Vector2(rectangle.Left, rectangle.Bottom), new Vector2(rectangle.Right, rectangle.Bottom), color, thickness);
    }

    // Draw a Circle
    public static void DrawCircle(this SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int segments = 32, float thickness = 1f)
    {
        EnsurePixelTextureExists(spriteBatch.GraphicsDevice);

        var points = new Vector2[segments];
        var angleStep = MathHelper.TwoPi / segments;

        for (var i = 0; i < segments; i++)
        {
            var angle = i * angleStep;
            points[i] = center + radius * new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }

        for (var i = 0; i < segments; i++)
        {
            Vector2 start = points[i];
            Vector2 end = points[(i + 1) % segments];
            spriteBatch.DrawLine(start, end, color, thickness);
        }
    }
}