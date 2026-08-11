using ImGuiNET;

namespace Dreambit.Editor.UI.Panels;

internal sealed class HierarchyPanel : EditorPanel
{
    public HierarchyPanel()
        : base(EditorPanelIds.Hierarchy, "Hierarchy")
    {
    }

    protected override void DrawContents()
    {
        if (ImGui.Button("+", new System.Numerics.Vector2(28f, 0f)))
            ImGui.OpenPopup("CreateEntityPlaceholder");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("##HierarchySearch", "Search entities", ref _search, 128);

        if (ImGui.BeginPopup("CreateEntityPlaceholder"))
        {
            ImGui.BeginDisabled();
            ImGui.MenuItem("Create Empty");
            ImGui.MenuItem("Create From Blueprint");
            ImGui.EndDisabled();
            ImGui.Separator();
            ImGui.TextDisabled("Scene documents arrive in Milestone 6.");
            ImGui.EndPopup();
        }

        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("No scene is open.");
    }

    private string _search = string.Empty;
}
