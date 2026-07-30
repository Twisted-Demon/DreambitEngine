using LDtk;
using LDtk.Renderer;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

[BlueprintType($"Dreambit.{nameof(LDtkRenderer)}")]
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


    public override void OnDraw()
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