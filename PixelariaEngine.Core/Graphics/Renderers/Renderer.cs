using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine.Graphics;

public class Renderer
{
    public int Order = 0;
    protected static GraphicsDevice Device => Core.Instance.GraphicsDevice;
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

    public virtual void Initialize() {}
    public virtual void OnDraw(){}

    public void CleanUpInternal()
    {
        Window.WindowResized -= OnWindowResized;
    }

    protected virtual void OnCleanUp()
    {
        
    }

    protected static RenderTarget2D CreateRenderTarget()
        => new RenderTarget2D(Device, Window.ScreenSize.X,
            Window.ScreenSize.Y, false, SurfaceFormat.Color, DepthFormat.None);
}