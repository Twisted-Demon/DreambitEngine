using System;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public abstract class RenderPass : IDisposable
{
    public int Order = 0;
    private bool _isDisposed;

    internal Scene Scene { get; init; }
    protected static GraphicsDevice Device => Core.Instance.GraphicsDevice;
    protected Effect DefaultEffect { get; private set; }
    protected DrawableRepository Drawables => Scene.Drawables;
    protected Camera2D RenderCamera => RenderPipeline.ActiveCamera;
    protected Point ViewportSize => RenderPipeline.ViewportSize;
    public virtual bool RendersToBackBuffer => false;

    public RenderPipeline RenderPipeline { get; internal init; }

    public bool IsActive { get; set; } = true;

    public void Dispose()
    {
        if (_isDisposed) return;

        Window.WindowResized -= OnWindowResized;
        OnDisposing();
        // Resources owns the shared effect cache. A render pass owns only its
        // scene-specific state and must not invalidate effects used by other scenes.
        DefaultEffect = null;

        _isDisposed = true;

        GC.SuppressFinalize(this);
    }

    internal void InitializeInternals()
    {
        Window.WindowResized += OnWindowResized;
        DefaultEffect = Resources.LoadAsset<Effect>("Effects/ForwardDiffuse");
        Initialize();
    }

    internal void ResizeInternals()
    {
        OnViewportResized();
    }

    public virtual void Initialize()
    {
    }

    public virtual void OnDraw()
    {
    }

    protected virtual void OnViewportResized()
    {
    }

    /// <summary>
    ///     Preserves the runtime window notification for custom passes. Built-in passes
    ///     resize through <see cref="OnViewportResized" /> so offscreen hosts work too.
    /// </summary>
    protected virtual void OnWindowResized(object sender, WindowResizedEventArgs args)
    {
    }

    protected virtual void OnDisposing()
    {
    }
}
