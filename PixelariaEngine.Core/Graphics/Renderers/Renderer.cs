using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine.Graphics;

public class Renderer
{
    public int Order = 0;
    protected static GraphicsDevice Device => Core.Instance.GraphicsDevice;
    protected Effect DefaultEffect { get; private set; }
    protected Scene Scene { get; private set; }
    
    public RenderTarget2D FinalRenderTarget { get; private set; }

    public bool IsActive { get; set; } = true;

    protected Renderer(Scene scene)
    {
        Scene = scene;
        Window.WindowResized += OnWindowResized;
        FinalRenderTarget = CreateRenderTarget();
    }
    protected virtual void OnWindowResized(object sender, WindowEventArgs args)
    {
        FinalRenderTarget?.Dispose();
        FinalRenderTarget = CreateRenderTarget();
    }

    internal void InitializeInternals()
    {
        DefaultEffect = Resources.LoadAsset<Effect>("Effects/ForwardDiffuse");
        Initialize();
    }
    
    public virtual void Initialize() {}
    public virtual void OnDraw(){}

    internal void CleanUpInternal()
    {
        Window.WindowResized -= OnWindowResized;
        FinalRenderTarget?.Dispose();
        FinalRenderTarget = null;
    }

    protected virtual void OnCleanUp()
    {
        
    }

    protected static RenderTarget2D CreateRenderTarget()
    {
        var target = new RenderTarget2D(
            Device,
            Device.PresentationParameters.BackBufferWidth,
            Device.PresentationParameters.BackBufferHeight,
            false,
            Device.PresentationParameters.BackBufferFormat,
            DepthFormat.None
        );

        return target;
    }
}