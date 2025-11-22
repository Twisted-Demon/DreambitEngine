using System.Linq;

namespace Dreambit.UI;

public class UiContainer : UiElement
{
    public override void Draw()
    {
        base.Draw();
        
        // sort children by ZIndex and draw
        var ordered = Children.OrderBy(c => c.ZIndex).ToList();

        foreach (var child in ordered)
            child.Draw();
    }

    public override void OnDebugDraw()
    {
        // sort children by ZIndex and draw
        var ordered = Children.OrderBy(c => c.ZIndex).ToList();

        foreach (var child in ordered)
            child.OnDebugDraw();
    }
}