using LDtk;
using LDtk.Renderer;

namespace Dreambit.ECS;

[BlueprintType($"{nameof(LDtkRenderer)}")]
public class LDtkRenderer : DrawableComponent<LDtkRenderer>
{
    private ExampleRenderer _renderer;
    public override RectangleF Bounds { get; }
    public LDtkLevel Level { get; set; }

    public override void OnCreated()
    {
        _renderer = LDtkManager.Instance.LDtkRenderer;
    }

    public override void OnAddedToEntity()
    {
        _renderer.PrerenderLevel(Level);
    }

    public override void OnPreDraw()
    {
    }


    protected override void OnDraw()
    {
        _renderer.RenderPrerenderedLevel(Level);
    }

    public override void OnDestroyed()
    {
    }

    public override bool IsVisibleFromCamera(RectangleF cameraBounds)
    {
        return true;
    }
}