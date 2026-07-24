using System.Linq;

namespace Dreambit.UI;

public class UiContainer : UiElement
{
    public override void OnDraw()
    {
        base.OnDraw();
        
        // sort children by ZIndex and draw
        var ordered = Children.OrderBy(c => c.ZIndex).ToList();

        foreach (var child in ordered)
            child.OnDraw();
    }
    
}