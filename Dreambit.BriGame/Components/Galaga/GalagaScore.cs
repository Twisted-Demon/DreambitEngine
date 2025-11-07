using Dreambit.ECS;

namespace Dreambit.BriGame.Components.Galaga;

public class GalagaScore : Component
{
    private Canvas _canvas;
    private UIText _text;
    
    public override void OnCreated()
    {
        _canvas = Entity.AttachComponent<Canvas>();
        _text = UIText.Create(_canvas, "hello");
    }

    public override void OnUpdate()
    {
        
    }
}