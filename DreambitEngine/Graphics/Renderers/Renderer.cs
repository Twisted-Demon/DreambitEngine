using Dreambit.ECS;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public abstract class Renderer
{
    public int Order = 0;

    protected Renderer(Scene scene)
    {
        Scene = scene;
    }

    protected static GraphicsDevice Device => Core.Instance.GraphicsDevice;
    protected Effect DefaultEffect { get; private set; }
    protected Scene Scene { get; private set; }
    protected DrawableList Drawables => Scene.Drawables;

    public RenderTarget2D FinalRenderTarget { get; private set; }

    public bool IsActive { get; set; } = true;

    protected virtual void OnWindowResized(object sender, WindowEventArgs args)
    {
        FinalRenderTarget?.Dispose();
        FinalRenderTarget = CreateRenderTarget();
    }

    internal void InitializeInternals()
    {
        Window.WindowResized += OnWindowResized;
        FinalRenderTarget = CreateRenderTarget();
        DefaultEffect = Resources.LoadAsset<Effect>("Effects/ForwardDiffuse");
        Initialize();
    }

    public virtual void Initialize()
    {
    }

    public virtual void OnDraw()
    {
    }

    internal void CleanUpInternal()
    {
        Window.WindowResized -= OnWindowResized;
        FinalRenderTarget?.Dispose();
        FinalRenderTarget = null;
        OnCleanUp();
    }

    protected virtual void OnCleanUp()
    {
    }

    protected static RenderTarget2D CreateRenderTarget()
    {
        var target = new RenderTarget2D(
            Device,
            Window.Width,
            Window.Height,
            false,
            Device.PresentationParameters.BackBufferFormat,
            DepthFormat.None
        );

        return target;
    }

    protected static RenderTarget2D CreateRenderTarget(int width, int height)
    {
        var target = new RenderTarget2D(
            Device,
            width,
            height,
            false,
            Device.PresentationParameters.BackBufferFormat,
            DepthFormat.None
        );

        return target;
    }
}