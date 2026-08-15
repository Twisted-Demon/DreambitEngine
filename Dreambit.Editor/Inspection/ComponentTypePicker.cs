using ImGuiNET;

namespace Dreambit.Editor.Inspection;

internal sealed class ComponentTypePicker
{
    private string _search = string.Empty;

    public Type? Draw(
        string popupId,
        IReadOnlyList<Type> componentTypes,
        Func<Type, bool> isDisabled)
    {
        if (ImGui.Button($"Add Component##{popupId}.Button", new System.Numerics.Vector2(-1f, 0f)))
        {
            _search = string.Empty;
            ImGui.OpenPopup(popupId);
        }

        if (!ImGui.BeginPopup(popupId))
            return null;

        Type? selected = null;
        try
        {
            ImGui.SetNextItemWidth(320f);
            ImGui.InputTextWithHint("##ComponentSearch", "Search components", ref _search, 128);
            ImGui.Separator();
            ImGui.BeginChild("##ComponentList", new System.Numerics.Vector2(320f, 280f));
            try
            {
                foreach (var type in componentTypes)
                {
                    if (!MatchesSearch(type))
                        continue;

                    var disabled = isDisabled(type);
                    if (disabled)
                        ImGui.BeginDisabled();
                    var clicked = ImGui.Selectable(type.FullName ?? type.Name);
                    if (disabled)
                        ImGui.EndDisabled();
                    if (!clicked || disabled)
                        continue;

                    selected = type;
                    ImGui.CloseCurrentPopup();
                    break;
                }
            }
            finally
            {
                ImGui.EndChild();
            }
        }
        finally
        {
            ImGui.EndPopup();
        }

        return selected;
    }

    private bool MatchesSearch(Type type) =>
        string.IsNullOrWhiteSpace(_search) ||
        type.Name.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
        (type.FullName?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false);
}
