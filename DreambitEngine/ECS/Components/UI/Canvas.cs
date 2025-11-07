using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

public class Canvas : UIComponent
{

    private List<UIElement> _uiComponents = [];
    

    public override void OnDrawUI()
    {
        foreach (var component in _uiComponents) component.OnDrawUI();
    }

    internal Vector2 ConvertToScreenCoord(Vector2 position)
    {
        
        var xScale = Window.Width / (float)100;
        var yScale = Window.Height / (float)100;

        return new Vector2(position.X * xScale, position.Y * yScale);
    }

    internal Vector2 ConvertToScreenSize(Vector2 size)
    {
        var xScale = Window.Width / 100;
        var yScale = Window.Height / 100;

        return new Vector2(size.X * xScale, size.Y * yScale);
    }

    public T CreateUIElement<T>(string name = null) where T : UIElement
    {
        name ??= typeof(T).Name;

        var entity = Entity.CreateChildOf(Entity, name);
        var uiElement = entity.AttachComponent<T>();
        
        foreach (var component in entity.GetAllComponents())
        {
            if (component is UIElement element)
            {
                element.Canvas = this;
                _uiComponents.Add(element);
            }
        }
        
        return uiElement;
    }

    public static (Canvas canvas, Entity entity) Create()
    {
        var entity = Entity.Create("canvas");
        var canvas = entity.AttachComponent<Canvas>();

        return (canvas, entity);
    }

    public static (T canvas, Entity entity) Create<T>() where T : Canvas
    {
        var entity = Entity.Create(typeof(T).Name);
        var canvas = entity.AttachComponent<T>();

        return (canvas, entity);
    }

    public override void OnDestroyed()
    {
        _uiComponents.Clear();
        _uiComponents = null;
    }
}