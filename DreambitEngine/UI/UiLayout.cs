using System;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

public class UiLayout
{
    public UiContainer Root { get; internal set; }

    public UiElement Find(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return FindRecursive(Root, id);
    }

    public T GetRequired<T>(string id) where T : UiElement
    {
        var element = Find(id);

        if (element is T typedElement)
            return typedElement;

        throw new InvalidOperationException(
            $"UI element '{id}' was not found or was not a {typeof(T).Name}.");
    }

    public void Update(Rectangle viewport, in UiInputState input)
    {
        Root.ResolveDependenciesRecursive();
        Root.Arrange(viewport);
        Root.Update(input);
    }

    public void Draw()
    {
        Root.OnDraw();
    }

    private static UiElement FindRecursive(UiElement element, string id)
    {
        if (element is null)
            return null;

        if (string.Equals(element.Id, id, StringComparison.Ordinal))
            return element;

        foreach (var child in element.Children)
        {
            var result = FindRecursive(child, id);
            if (result is not null)
                return result;
        }

        return null;
    }
}
