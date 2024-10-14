using LDtk;
using LDtk.Renderer;
using Microsoft.Xna.Framework;

namespace PixelariaEngine.ECS;

public class LDtkRenderer : DrawableComponent<LDtkRenderer>
{
    public override Rectangle Bounds { get; }
    private ExampleRenderer _renderer;
    public LDtkLevel Level { get; set; }

    public override void OnCreated()
    {
        _renderer = LDtkManager.Instance.LDtkRenderer;
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
    }

    public override bool IsVisibleFromCamera(Rectangle cameraBounds)
    {
        return true;
    }
}