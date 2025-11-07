using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.ECS;


[Require(typeof(UITexture), typeof(UIText))]
public class UIButton : UIElement
{
#region Public Members & Properties    

    /// <summary>Texture Element of the button</summary>
    public UITexture Texture { get; private set; }

    /// <summary>Text Element of the button</summary>
    public UIText UIText { get; private set; }

    /// <summary>Action hook is called when the button is clicked</summary>
    public Action OnClick;
    
    /// <summary>Action hook is called when the mouse is hovering over the button</summary>
    public Action OnHover;
    
    public override Rectangle Bounds => Texture.Bounds;
    
#endregion    

#region Fields (internals)

    private bool _isHovering = false;
    
#endregion

#region LifeCycle

    public override void OnCreated()
    {
        Texture = Entity.GetComponent<UITexture>();
        UIText = Entity.GetComponent<UIText>();
        Canvas = Entity.GetComponent<Canvas>();
    }

    public override void OnUpdate()
    {
        CheckIfHovered();
        CheckIfClicked();
    }

#endregion    

#region Internal Functions
    private void CheckIfClicked()
    {
        var screenPos = Input.GetMousePosition();
        var convertedPos = Scene.UICamera.UIScreenToWorld(screenPos);

        if (Input.LeftPressed())
        {
            if (Bounds.Contains(convertedPos)) 
                OnClick?.Invoke();
        }
    }

    private void CheckIfHovered()
    {
        var screenPos = Input.GetMousePosition();
        var convertedPos = Scene.UICamera.UIScreenToWorld(screenPos);
        
        if (Bounds.Contains(convertedPos) && !_isHovering)
        {
            _isHovering = true;
            OnHover?.Invoke();
        }
        else
        {
            _isHovering = false;
        }
    }
    
#endregion    
}