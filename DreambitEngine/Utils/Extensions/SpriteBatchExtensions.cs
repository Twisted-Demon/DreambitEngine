using System;
using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public static class SpriteBatchExtensions
{
    public static Texture2D PixelTexture;

    public static void EnsurePixelTextureExists(GraphicsDevice graphicsDevice)
    {
        if (PixelTexture == null)
        {
            PixelTexture = new Texture2D(graphicsDevice, 1, 1);
            PixelTexture.SetData([Color.White]);
        }
    }

    private static float GetLineHeight(SpriteFontBase font, float lineSpacingMultiplier = 1f)
    {
        float h = font.LineHeight;
        if (h <= 0f)
            // "Ay" or "Mg" tends to give a decent vertical extent if you need a fallback
            h = font.MeasureString("Ay").Y;
        return h * lineSpacingMultiplier;
    }

    private static List<string> SplitTextIntoLines(SpriteFontBase spriteFont, string text, float maxWidth)
    {
        var lines = new List<string>();
        var words = text.Split(' ');
        var currentLine = string.Empty;

        foreach (var word in words)
        {
            var testLine = currentLine + (currentLine.Length > 0 ? " " : "") + word;
            var size = spriteFont.MeasureString(testLine);

            if (size.X > maxWidth)
            {
                if (currentLine.Length > 0)
                    lines.Add(currentLine);

                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (currentLine.Length > 0)
            lines.Add(currentLine);

        return lines;
    }

    //draw multi lined text
    public static void DrawMultiLineText(this SpriteBatch spriteBatch,
        SpriteFontBase font,
        string text,
        Vector2 position,
        HorizontalAlignment horizontalAlignment,
        VerticalAlignment verticalAlignment,
        Color color,
        float maxWidth,
        float lineSpacingMultiplier = 1f)
    {
        var lines = SplitTextIntoLines(font, text, maxWidth);

        //calculate the total height
        var lineHeight = GetLineHeight(font, lineSpacingMultiplier);
        var totalHeight = lines.Count * lineHeight;

        switch (verticalAlignment)
        {
            case VerticalAlignment.Center: position.Y -= totalHeight * 0.5f; break;
            case VerticalAlignment.Bottom: position.Y -= totalHeight; break;
            // Top = no change
        }


        for (var i = 0; i < lines.Count; i++)
        {
            //adjust the horizontal position based on alignment

            var alignmentOffset = GetAlignmentOffset(font, lines[i], horizontalAlignment);
            var linePos = new Vector2(position.X + alignmentOffset.X, position.Y + i * lineHeight);
            spriteBatch.DrawString(font, lines[i], linePos, color);
        }
    }

    public static void DrawTextAligned(this SpriteBatch spriteBatch,
        SpriteFontBase font,
        string text,
        Vector2 position,
        HorizontalAlignment horizontalAlignment,
        VerticalAlignment verticalAlignment,
        Color color)
    {
        var size = font.MeasureString(text);

        // Vertical
        position.Y -= verticalAlignment switch
        {
            VerticalAlignment.Top => 0f,
            VerticalAlignment.Center => size.Y * 0.5f,
            VerticalAlignment.Bottom => size.Y,
            _ => 0f
        };

        // Horizontal
        var xOffset = horizontalAlignment switch
        {
            HorizontalAlignment.Left => 0f,
            HorizontalAlignment.Center => size.X * 0.5f,
            HorizontalAlignment.Right => size.X,
            _ => 0f
        };

        spriteBatch.DrawString(font, text, new Vector2(position.X + xOffset, position.Y), color);
    }

    private static Vector2 GetAlignmentOffset(SpriteFontBase spriteFont, string text,
        HorizontalAlignment horizontalAlignment)
    {
        var textSize = spriteFont.MeasureString(text);

        return horizontalAlignment switch
        {
            HorizontalAlignment.Center => new Vector2(-textSize.X / 2, textSize.Y / 2),
            HorizontalAlignment.Left => new Vector2(0, textSize.Y / 2),
            HorizontalAlignment.Right => new Vector2(-textSize.X, textSize.Y / 2),
            _ => Vector2.Zero
        };
    }

    // Draw a Line
    public static void DrawLine(this SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color,
        float thickness = 1f)
    {
        EnsurePixelTextureExists(spriteBatch.GraphicsDevice);

        var edge = end - start;
        var angle = (float)Math.Atan2(edge.Y, edge.X);

        spriteBatch.Draw(PixelTexture,
            new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), (int)thickness),
            null,
            color,
            angle,
            Vector2.Zero,
            SpriteEffects.None,
            0);
    }

    // Draw a line
    public static void DrawLine(this SpriteBatch spriteBatch, Vector3 start, Vector3 end, Color color,
        float thickness = 1f)
    {
        EnsurePixelTextureExists(spriteBatch.GraphicsDevice);

        var edge = end - start;
        var angle = (float)Math.Atan2(edge.Y, edge.X);

        spriteBatch.Draw(PixelTexture,
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

        for (var i = 0; i < points.Length; i++)
        {
            var current = points[i];
            var next = points[(i + 1) % points.Length];

            DrawLine(spriteBatch, current, next, color, thickness);
        }
    }

    public static void DrawPath(this SpriteBatch spriteBatch, Vector2[] points, Color color, float thickness = 1f)
    {
        for (var i = 0; i < points.Length - 1; i++)
        {
            var current = points[i];
            var next = points[i + 1];

            DrawLine(spriteBatch, current, next, color, thickness);
        }
    }

    public static void DrawPath(this SpriteBatch spriteBatch, Vector3[] points, Color color, float thickness = 1f)
    {
        for (var i = 0; i < points.Length - 1; i++)
        {
            var current = points[i];
            var next = points[i + 1];

            DrawLine(spriteBatch, current, next, color, thickness);
        }
    }

    public static void DrawPoint(this SpriteBatch spriteBatch, Vector2 point, Color color, float thickness = 1f)
    {
        EnsurePixelTextureExists(spriteBatch.GraphicsDevice);

        var pos = new Vector2(point.X - thickness * 0.5f, point.Y - thickness * 0.5f);

        spriteBatch.Draw(PixelTexture, pos, null, color, 0, Vector2.Zero, thickness, SpriteEffects.None, 0);
    }

    // Draw a Filled Rectangle
    public static void DrawFilledRectangle(this SpriteBatch spriteBatch, Rectangle rectangle, Color color)
    {
        EnsurePixelTextureExists(spriteBatch.GraphicsDevice);

        spriteBatch.Draw(PixelTexture, rectangle, color);
    }

    //draw a filled rectangle with a square

    // Draw a Hollow Rectangle (outline)
    public static void DrawHollowRectangle(this SpriteBatch spriteBatch, Rectangle rectangle, Color color,
        float thickness = 1f)
    {
        // Top
        spriteBatch.DrawLine(new Vector2(rectangle.Left, rectangle.Top), new Vector2(rectangle.Right, rectangle.Top),
            color, thickness);
        // Left
        spriteBatch.DrawLine(new Vector2(rectangle.Left, rectangle.Top), new Vector2(rectangle.Left, rectangle.Bottom),
            color, thickness);
        // Right
        spriteBatch.DrawLine(new Vector2(rectangle.Right, rectangle.Top),
            new Vector2(rectangle.Right, rectangle.Bottom), color, thickness);
        // Bottom
        spriteBatch.DrawLine(new Vector2(rectangle.Left, rectangle.Bottom),
            new Vector2(rectangle.Right, rectangle.Bottom), color, thickness);
    }

    // Draw a Circle
    public static void DrawCircle(this SpriteBatch spriteBatch, Vector2 center, float radius, Color color,
        int segments = 32, float thickness = 1f)
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
            var start = points[i];
            var end = points[(i + 1) % segments];
            spriteBatch.DrawLine(start, end, color, thickness);
        }
    }
}