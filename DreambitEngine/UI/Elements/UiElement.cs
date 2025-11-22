using System.Collections.Generic;
using System.Xml;
using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

public abstract class UiElement
{
    public string Id;
    public UiContainer Parent;

    public UiLength X = UiLength.Pixels(0);
    public UiLength Y = UiLength.Pixels(0);
    public UiLength Width = UiLength.Pixels(0);
    public UiLength Height = UiLength.Pixels(0);
    
    public UiAnchor Anchor { get; set; }
    public int ZIndex = 0;

    public Rectangle Bounds;

    public List<UiElement> Children = [];
    
    private bool _isDirty;
    public bool IsDirty
    {
        get => _isDirty;
        set
        {
            _isDirty = value;
            
            if(value == true)
                ResolveDependencies();
        }
    }
    
    public virtual void Arrange(Rectangle parentBounds)
    {
        int w = Width.Resolve(parentBounds.Width);
        int h = Height.Resolve(parentBounds.Height);

        int x = X.Resolve(parentBounds.Width);
        int y = Y.Resolve(parentBounds.Height);

        // anchor offset
        int offsetX = 0;
        int offsetY = 0;

        switch (Anchor)
        {
            case UiAnchor.TopLeft:
                offsetX = 0;
                offsetY = 0;
                break;
            case UiAnchor.TopCenter:
                offsetX = w / 2;
                offsetY = 0;
                break;
            case UiAnchor.TopRight:
                offsetX = w;
                offsetY = 0;
                break;
            case UiAnchor.CenterLeft:
                offsetX = 0;
                offsetY = h / 2;
                break;
            case UiAnchor.Center:
                offsetX = w / 2;
                offsetY = h / 2;
                break;
            case UiAnchor.CenterRight:
                offsetX = w;
                offsetY = h / 2;
                break;
            case UiAnchor.BottomLeft:
                offsetX = 0;
                offsetY = h;
                break;
            case UiAnchor.BottomCenter:
                offsetX = w / 2;
                offsetY = h;
                break;
            case UiAnchor.BottomRight:
                offsetX = w;
                offsetY = h;
                break;
        }

        int screenX = parentBounds.X + x + offsetX;
        int screenY = parentBounds.Y + y + offsetY;

        Bounds = new Rectangle(screenX, screenY, w, h);

        // default: arrange children within own bounds
        foreach (var child in Children)
            child.Arrange(Bounds);
    }

    public virtual void Draw()
    {
        Graphics.SpriteBatch.DrawHollowRectangle(Bounds, Color.Red);
        
    }
    public virtual void OnDebugDraw(){}

    public virtual void OnUpdate() 
    {
        foreach(var child in Children)
            child.OnUpdate();
    }
    

    public virtual void Parse(XmlNode node)
    {
        
    }

    public virtual void ResolveDependencies()
    {
        
    }
}