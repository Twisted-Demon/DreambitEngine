using LDtk;
using LDtk.Renderer;

namespace PixelariaEngine.ECS;

public class LDtkRenderer : DrawableComponent<LDtkRenderer>
{
    private ExampleRenderer _renderer;
    public LDtkLevel Level { get; set; }

    public override void OnCreated()
    {
        _renderer = new ExampleRenderer(Core.SpriteBatch, Core.Instance.Content);
    }

    public override void OnPreDraw()
    {
        _renderer.PrerenderLevel(Level);
    }

    public override void OnDraw()
    {
        _renderer.RenderPrerenderedLevel(Level);
    }

    public override void OnDestroyed()
    {
        _renderer.Dispose();
    }
}