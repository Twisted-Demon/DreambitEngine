using ImGuiNET;

namespace Dreambit.Editor.UI.Panels;

internal sealed class InspectorPanel : EditorPanel
{
    public InspectorPanel()
        : base(EditorPanelIds.Inspector, "Inspector")
    {
    }

    protected override void DrawContents()
    {
        ImGui.TextDisabled("Nothing selected");
        ImGui.Spacing();
        ImGui.TextWrapped(
            "Select an entity or asset to inspect it. Reflection-driven property drawers are introduced in Milestone 8.");
    }
}
