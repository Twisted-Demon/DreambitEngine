using ImGuiNET;

namespace Dreambit.Editor.Inspection;

internal static class InspectorUi
{
    public static void PropertyRow(string id, string label, Action drawValue)
    {
        PropertyRowCore(id, label, () =>
        {
            drawValue();
            return true;
        }, null, false);
    }

    public static T PropertyRow<T>(
        string id,
        string label,
        Func<T> drawValue,
        string? tooltip = null)
    {
        return PropertyRowCore(id, label, drawValue, tooltip, true);
    }

    private static T PropertyRowCore<T>(
        string id,
        string label,
        Func<T> drawValue,
        string? tooltip,
        bool drawWhenTableUnavailable)
    {
        var availableWidth = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        var labelWidth = Math.Clamp(availableWidth * 0.35f, 120f, 190f);
        var flags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings;
        if (!ImGui.BeginTable($"##PropertyRow.{id}", 2, flags))
            return drawWhenTableUnavailable ? drawValue() : default!;

        try
        {
            ImGui.TableSetupColumn("##Label", ImGuiTableColumnFlags.WidthFixed, labelWidth);
            ImGui.TableSetupColumn("##Value", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(label);
            if (!string.IsNullOrWhiteSpace(tooltip) && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
            ImGui.TableSetColumnIndex(1);
            ImGui.SetNextItemWidth(-1f);
            var result = drawValue();
            if (!string.IsNullOrWhiteSpace(tooltip) && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
            return result;
        }
        finally
        {
            ImGui.EndTable();
        }
    }

    public static void ReferenceField(
        string id,
        string label,
        string value,
        bool clearDisabled,
        Action select,
        Action clear)
    {
        var availableWidth = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        var labelWidth = Math.Clamp(availableWidth * 0.35f, 120f, 190f);
        var flags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings;
        if (!ImGui.BeginTable($"##ReferenceField.{id}", 3, flags))
            return;

        try
        {
            ImGui.TableSetupColumn("##Label", ImGuiTableColumnFlags.WidthFixed, labelWidth);
            ImGui.TableSetupColumn("##Value", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("##Clear", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(label);
            ImGui.TableSetColumnIndex(1);
            if (ImGui.Button($"{value}##{id}", new System.Numerics.Vector2(-1f, 0f)))
                select();
            ImGui.TableSetColumnIndex(2);
            ImGui.BeginDisabled(clearDisabled);
            if (ImGui.SmallButton($"×##{id}.Clear"))
                clear();
            ImGui.EndDisabled();
        }
        finally
        {
            ImGui.EndTable();
        }
    }

    public static void MixedValueIndicator(string label, bool mixed)
    {
        if (!mixed)
            return;

        ImGui.SameLine();
        ImGui.TextDisabled($"{label}: —");
    }

    public static (bool Open, bool RemoveRequested) RemovableHeader(
        string title,
        bool allowRemove = true,
        string? statusText = null)
    {
        var flags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings;
        if (!ImGui.BeginTable("##RemovableHeader", 2, flags))
            return (false, false);

        try
        {
            ImGui.TableSetupColumn("##Title", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("##Remove", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            var open = ImGui.CollapsingHeader(title, ImGuiTreeNodeFlags.DefaultOpen);
            var removeRequested = false;

            ImGui.TableSetColumnIndex(1);
            if (allowRemove)
            {
                removeRequested = ImGui.SmallButton("×");
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip($"Remove {title}");
            }
            else if (!string.IsNullOrWhiteSpace(statusText))
            {
                ImGui.TextDisabled(statusText);
            }

            return (open, removeRequested);
        }
        finally
        {
            ImGui.EndTable();
        }
    }
}
