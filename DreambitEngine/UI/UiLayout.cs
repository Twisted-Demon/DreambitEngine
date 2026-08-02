using System;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>
/// Owns a UI visual tree and coordinates dependency resolution, layout,
/// input updates, lookup, and drawing.
/// </summary>
public class UiLayout
{
    /// <summary>Gets the root container that spans the UI viewport.</summary>
    public UiContainer Root { get; internal set; }

    /// <summary>Finds an element anywhere in the visual tree by ID.</summary>
    /// <param name="id">The case-sensitive element ID.</param>
    /// <returns>The matching element, or <see langword="null"/> when no match exists.</returns>
    public UiElement Find(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return FindRecursive(Root, id);
    }

    /// <summary>Gets an element by ID and verifies its expected type.</summary>
    /// <typeparam name="T">The required element type.</typeparam>
    /// <param name="id">The case-sensitive element ID.</param>
    /// <returns>The matching typed element.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the element is missing or has a different type.
    /// </exception>
    public T GetRequired<T>(string id) where T : UiElement
    {
        var element = Find(id);

        if (element is T typedElement)
            return typedElement;

        throw new InvalidOperationException(
            $"UI element '{id}' was not found or was not a {typeof(T).Name}.");
    }

    /// <summary>
    /// Resolves assets, measures and arranges the visual tree, then dispatches
    /// the current input state.
    /// </summary>
    /// <param name="viewport">The available UI rectangle.</param>
    /// <param name="input">The input snapshot for this update.</param>
    public void Update(Rectangle viewport, in UiInputState input)
    {
        Root.ResolveDependenciesRecursive();
        Root.Measure(viewport.Size);
        Root.Arrange(viewport);
        Root.Update(input);
    }

    /// <summary>Draws the visual tree using its current arranged bounds.</summary>
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
