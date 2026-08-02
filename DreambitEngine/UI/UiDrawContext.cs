using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.UI;

internal sealed class UiDrawContext : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly Matrix _transform;
    private readonly Rectangle _previousScissor;
    private readonly Stack<Rectangle> _clips = [];

    public UiDrawContext(GraphicsDevice device, Matrix transform)
    {
        _device = device;
        _transform = transform;
        _previousScissor = device.ScissorRectangle;

        var viewport = device.Viewport.Bounds;
        _clips.Push(viewport);
        _device.ScissorRectangle = viewport;
    }

    public bool IsEmpty => _clips.Peek().IsEmpty;

    public void PushClip(Rectangle bounds)
    {
        var transformed = TransformBounds(bounds);
        var clipped = Rectangle.Intersect(_clips.Peek(), transformed);
        _clips.Push(clipped);
        _device.ScissorRectangle = clipped;
    }

    public void PopClip()
    {
        if (_clips.Count <= 1)
            return;

        _clips.Pop();
        _device.ScissorRectangle = _clips.Peek();
    }

    public void Dispose()
    {
        _device.ScissorRectangle = _previousScissor;
    }

    private Rectangle TransformBounds(Rectangle bounds)
    {
        var topLeft = Vector2.Transform(
            new Vector2(bounds.Left, bounds.Top),
            _transform);
        var topRight = Vector2.Transform(
            new Vector2(bounds.Right, bounds.Top),
            _transform);
        var bottomLeft = Vector2.Transform(
            new Vector2(bounds.Left, bounds.Bottom),
            _transform);
        var bottomRight = Vector2.Transform(
            new Vector2(bounds.Right, bounds.Bottom),
            _transform);

        var minX = MathF.Min(
            MathF.Min(topLeft.X, topRight.X),
            MathF.Min(bottomLeft.X, bottomRight.X));
        var minY = MathF.Min(
            MathF.Min(topLeft.Y, topRight.Y),
            MathF.Min(bottomLeft.Y, bottomRight.Y));
        var maxX = MathF.Max(
            MathF.Max(topLeft.X, topRight.X),
            MathF.Max(bottomLeft.X, bottomRight.X));
        var maxY = MathF.Max(
            MathF.Max(topLeft.Y, topRight.Y),
            MathF.Max(bottomLeft.Y, bottomRight.Y));

        return new Rectangle(
            (int)MathF.Floor(minX),
            (int)MathF.Floor(minY),
            Math.Max(0, (int)MathF.Ceiling(maxX - minX)),
            Math.Max(0, (int)MathF.Ceiling(maxY - minY)));
    }
}
