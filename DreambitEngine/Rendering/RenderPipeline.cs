using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public class RenderPipeline(Scene scene) : IDisposable
{
    private readonly List<RenderPass> _renderers = [];
    private bool _disposed;

    public RenderTarget2D SceneRenderTarget { get; set; }

    public void Dispose()
    {
        if (_disposed) return;

        Window.WindowResized -= OnWindowResized;

        foreach (var renderer in _renderers)
            renderer?.Dispose();

        _renderers.Clear();

        _disposed = true;
        
        GC.SuppressFinalize(this);
    }

    public void Initialize()
    {
        Window.WindowResized += OnWindowResized;
        SceneRenderTarget = CreateRenderTarget();
    }

    public void AddRenderPass<T>() where T : RenderPass, new()
    {
        var renderer = new T
        {
            Scene = scene,
            RenderPipeline = this
        };

        renderer.InitializeInternals();
        _renderers.Add(renderer);

        _renderers.Sort(static (left, right) =>
            left.Order.CompareTo(right.Order));
    }

    public T GetRenderPass<T>() where T : RenderPass
    {
        foreach (var renderer in _renderers)
            if (renderer is T typedRenderer)
                return typedRenderer;

        return null;
    }

    public void OnDraw()
    {
        foreach (var renderer in _renderers)
        {
            if (!renderer.IsActive)
                continue;
            
            renderer.OnDraw();
        }

        Core.Instance.GraphicsDevice.SetRenderTarget(null);
        Core.Instance.GraphicsDevice.Clear(scene.BackgroundColor);
        
        Core.SpriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            scene.RenderingOptions.SamplerState);

        Core.SpriteBatch.Draw(
            SceneRenderTarget,
            Vector2.Zero,
            Color.White);

        Core.SpriteBatch.End();
    }

    public static RenderTarget2D CreateRenderTarget()
    {
        var target = new RenderTarget2D(
            Core.Instance.GraphicsDevice,
            Window.Width,
            Window.Height,
            false,
            Core.Instance.GraphicsDevice.PresentationParameters.BackBufferFormat,
            DepthFormat.None
        );

        return target;
    }

    public static RenderTarget2D CreateRenderTarget(int width, int height)
    {
        var target = new RenderTarget2D(
            Core.Instance.GraphicsDevice,
            width,
            height,
            false,
            Core.Instance.GraphicsDevice.PresentationParameters.BackBufferFormat,
            DepthFormat.None
        );

        return target;
    }

    public static RenderTarget2D CreateRenderTarget(Point size)
    {
        var target = new RenderTarget2D(
            Core.Instance.GraphicsDevice,
            size.X,
            size.Y,
            false,
            Core.Instance.GraphicsDevice.PresentationParameters.BackBufferFormat,
            DepthFormat.None
        );

        return target;
    }

    private void OnWindowResized(object sender, WindowResizedEventArgs args)
    {
        SceneRenderTarget?.Dispose();
        SceneRenderTarget = CreateRenderTarget();
    }
}