using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using PixelariaEngine.Graphics;

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
    
    private static List<string> SplitTextIntoLines(BitmapFont spriteFont, string text, float maxWidth)
    {
        var lines = new List<string>();
        var words = text.Split(' ');
        var currentLine = string.Empty;

        foreach (var word in words)
        {
            var testLine = currentLine + (currentLine.Length > 0 ? " " : "") + word;
            var size = spriteFont.MeasureString(testLine);

            if (size.Width > maxWidth)
            {
                if(currentLine.Length > 0)
                    lines.Add(currentLine);
                
                currentLine = word;
            }
            else
                currentLine = testLine;
        }
        
        if(currentLine.Length > 0)
            lines.Add(currentLine);

        return lines;
    }
    
    //draw multi lined text
    public static void DrawMultiLineText(this SpriteBatch spriteBatch, BitmapFont spriteFont, string text, Vector2 position, 
        HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, Color color, float maxWidth)
    {
        var lines = SplitTextIntoLines(spriteFont, text, maxWidth);
        
        //calculate the total height
        float totalHeight = lines.Count * spriteFont.LineHeight;

        switch (verticalAlignment)
        {
            case VerticalAlignment.Top:
                break;
            case VerticalAlignment.Center:
                position.Y -= totalHeight / 2;
                break;
            case VerticalAlignment.Bottom:
                position.Y -= totalHeight;
                break;
        }
        

        for (int i = 0; i < lines.Count; i++)
        {
            
            //adjust the horizontal position based on alighn

            var alignmentOffset = GetAlignmentOffset(spriteFont, lines[i], horizontalAlignment);

            var pos = new Vector2(position.X + alignmentOffset.X, position.Y);
            spriteBatch.DrawString(spriteFont, lines[i], new Vector2(pos.X, pos.Y + i * spriteFont.LineHeight), color);
        }
    }
    
    private static Vector2 GetAlignmentOffset(BitmapFont spriteFont, string text, HorizontalAlignment horizontalAlignment)
    {
        var textSize = spriteFont.MeasureString(text);

        return horizontalAlignment switch
        {
            HorizontalAlignment.Center => new Vector2(-textSize.Width/ 2, textSize.Height / 2),
            HorizontalAlignment.Left => new Vector2(-textSize.Width, textSize.Height / 2),
            HorizontalAlignment.Right => new Vector2(textSize.Width / 2, textSize.Height / 2),
            _ => Vector2.Zero
        };
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
    
    //Draw a polygon
    public static void DrawPolygon(this SpriteBatch spriteBatch, Vector2[] points, Color color, float thickness = 1f)
    {
        EnsurePixelTextureExists(spriteBatch.GraphicsDevice);
        
        for (int i = 0; i < points.Length; i++)
        {
            var current = points[i];
            var next = points[(i + 1) % points.Length];
            
            DrawLine(spriteBatch, current, next, color, thickness);
        }
    }

    public static void DrawPoint(this SpriteBatch spriteBatch, Vector2 point, Color color, float thickness = 1f)
    {
        EnsurePixelTextureExists(spriteBatch.GraphicsDevice);
        
        spriteBatch.Draw(_pixelTexture, point, null, color, 0, Vector2.Zero, thickness, SpriteEffects.None, 0);
    }
    
    // Draw a Filled Rectangle
    public static void DrawFilledRectangle(this SpriteBatch spriteBatch, Rectangle rectangle, Color color)
    {
        EnsurePixelTextureExists(spriteBatch.GraphicsDevice);
        
        spriteBatch.Draw(_pixelTexture, rectangle, color);
    }
    
    //draw a filled rectangle with a square

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