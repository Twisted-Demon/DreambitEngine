using Microsoft.Xna.Framework.Graphics;

namespace PixelariaEngine.Graphics;

public abstract class Renderer(Scene scene)
{
    protected static GraphicsDevice Device => Core.Instance.GraphicsDevice;
    public Scene Scene { get; private set; } = scene;

    public abstract void Initialize();
    public abstract void OnDraw();

    public virtual void CleanUp()
    {
        
    }
}