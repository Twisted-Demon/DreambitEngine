using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public sealed class RenderPipeline(Scene scene) : IDisposable
{
    /// <summary>
    /// HDR scene color is retained until presentation so future post-process passes
    /// such as bloom can inspect values above display white.
    /// </summary>
    public static SurfaceFormat SceneColorFormat => SurfaceFormat.HdrBlendable;

    private readonly List<RenderPass> _renderers = [];
    private bool _disposed;
    private Effect _presentEffect;

    public RenderTarget2D SceneRenderTarget { get; set; }

    public void Dispose()
    {
        if (_disposed) return;

        Window.WindowResized -= OnWindowResized;

        foreach (var renderer in _renderers)
            renderer?.Dispose();

        _renderers.Clear();

        if (_presentEffect is not null)
        {
            Resources.UnloadAsset(_presentEffect.Name);
            _presentEffect = null;
        }

        _disposed = true;

        GC.SuppressFinalize(this);
    }

    public void Initialize()
    {
        Window.WindowResized += OnWindowResized;
        SceneRenderTarget = CreateRenderTarget();
        _presentEffect = Resources.LoadAsset<Effect>("Effects/Present");
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
            if (renderer.RendersToBackBuffer)
                continue;

            renderer.OnDraw();
        }

        Core.Instance.GraphicsDevice.SetRenderTarget(null);
        Core.Instance.GraphicsDevice.Clear(scene.BackgroundColor);

        _presentEffect.Parameters["Exposure"]?.SetValue(Mathf.Max(0f, scene.Settings.Exposure));
        
        Core.SpriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.Opaque,
            scene.RenderingOptions.SamplerState,
            DepthStencilState.None,
            RasterizerState.CullNone,
            _presentEffect);

        Core.SpriteBatch.Draw(
            SceneRenderTarget,
            Vector2.Zero,
            Color.White);

        Core.SpriteBatch.End();

        // UI is composed after the scene has been presented so it is neither
        // post-processed nor sampled through the scene render target.
        foreach (var renderer in _renderers)
            if (renderer.RendersToBackBuffer)
                renderer.OnDraw();
    }

    public static RenderTarget2D CreateRenderTarget()
    {
        var target = new RenderTarget2D(
            Core.Instance.GraphicsDevice,
            Window.Width,
            Window.Height,
            false,
            SceneColorFormat,
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
            SceneColorFormat,
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
            SceneColorFormat,
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
