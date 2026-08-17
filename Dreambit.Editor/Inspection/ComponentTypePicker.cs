using System.Numerics;
using Dreambit.EditorApi;

namespace Dreambit.Editor.Inspection;

internal sealed class ComponentTypePicker
{
    private string _search = string.Empty;

    public Type? Draw(
        string popupId,
        IReadOnlyList<Type> componentTypes,
        Func<Type, bool> isDisabled)
    {
        if (EditorGui.FullWidthButton($"{popupId}.Button", "Add Component", primary: true))
        {
            _search = string.Empty;
            EditorGui.OpenPopup(popupId);
        }

        using var popup = EditorGui.Popup(popupId);
        if (!popup.IsOpen)
            return null;

        Type? selected = null;
        EditorGui.SearchInput("ComponentSearch", "Search components", ref _search);
        EditorGui.Separator();
        using (var child = EditorGui.Child("ComponentList", new Vector2(320f, 280f)))
        {
            if (child.IsVisible)
            {
                foreach (var type in componentTypes)
                {
                    if (!MatchesSearch(type))
                        continue;

                    var disabled = isDisabled(type);
                    using var disabledScope = EditorGui.Disabled(disabled);
                    var clicked = EditorGui.Selectable(type.FullName ?? type.Name, type.FullName ?? type.Name);
                    if (!clicked || disabled)
                        continue;

                    selected = type;
                    EditorGui.ClosePopup();
                    break;
                }
            }
        }

        return selected;
    }

    private bool MatchesSearch(Type type) =>
        string.IsNullOrWhiteSpace(_search) ||
        type.Name.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
        (type.FullName?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false);
}
