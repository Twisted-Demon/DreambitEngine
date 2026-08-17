using ImGuiNET;

namespace Dreambit.EditorApi;

/// <summary>A balanced, allocation-free ImGui ID scope.</summary>
public ref struct EditorGuiIdScope
{
    private bool _active;

    internal EditorGuiIdScope(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ImGui.PushID(id);
        _active = true;
    }

    public void Dispose()
    {
        if (!_active)
            return;

        ImGui.PopID();
        _active = false;
    }
}

/// <summary>A balanced, allocation-free disabled-control scope.</summary>
public ref struct EditorGuiDisabledScope
{
    private bool _active;

    internal EditorGuiDisabledScope(bool disabled)
    {
        _active = disabled;
        if (disabled)
            ImGui.BeginDisabled();
    }

    public void Dispose()
    {
        if (!_active)
            return;

        ImGui.EndDisabled();
        _active = false;
    }
}

/// <summary>A balanced muted-text scope for read-only or inactive content.</summary>
public ref struct EditorGuiMutedScope
{
    private bool _active;

    internal EditorGuiMutedScope(bool muted)
    {
        _active = muted;
        if (muted)
            ImGui.PushStyleColor(ImGuiCol.Text, EditorGuiTheme.MutedText);
    }

    public void Dispose()
    {
        if (!_active)
            return;
        ImGui.PopStyleColor();
        _active = false;
    }
}

/// <summary>A styled, balanced collapsible Inspector section.</summary>
public ref struct EditorGuiSectionScope
{
    private bool _idActive;
    private bool _indented;

    internal EditorGuiSectionScope(
        string id,
        string title,
        bool defaultOpen,
        string? description,
        bool allowRemove,
        string? statusText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        IsOpen = false;
        RemoveRequested = false;
        _idActive = false;
        _indented = false;

        ImGui.Dummy(new System.Numerics.Vector2(0f, EditorGuiTheme.CompactSpacing));
        ImGui.PushID(id);
        _idActive = true;

        var headerOpen = false;
        var removeRequested = false;
        var tableFlags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings;
        if (ImGui.BeginTable("##SectionHeader", 2, tableFlags))
        {
            try
            {
                ImGui.TableSetupColumn("##Title", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(
                    "##Action",
                    ImGuiTableColumnFlags.WidthFixed,
                    allowRemove ? 64f : string.IsNullOrWhiteSpace(statusText) ? 1f : 58f);
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                headerOpen = DrawHeader(title, defaultOpen);
                ImGui.TableSetColumnIndex(1);
                if (allowRemove)
                {
                    removeRequested = ImGui.SmallButton("Remove##Section");
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip($"Remove {title}");
                }
                else if (!string.IsNullOrWhiteSpace(statusText))
                {
                    ImGui.TextDisabled(statusText);
                }
            }
            finally
            {
                ImGui.EndTable();
            }
        }
        else
        {
            headerOpen = DrawHeader(title, defaultOpen);
        }

        IsOpen = headerOpen;
        RemoveRequested = removeRequested;

        if (!IsOpen)
            return;

        ImGui.Indent(EditorGuiTheme.PropertyIndent);
        _indented = true;
        if (!string.IsNullOrWhiteSpace(description))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, EditorGuiTheme.MutedText);
            try
            {
                ImGui.TextWrapped(description);
            }
            finally
            {
                ImGui.PopStyleColor();
            }
        }

        ImGui.Dummy(new System.Numerics.Vector2(0f, EditorGuiTheme.CompactSpacing));
    }

    public bool IsOpen { get; }

    public bool RemoveRequested { get; }

    public void Dispose()
    {
        if (_indented)
        {
            ImGui.Unindent(EditorGuiTheme.PropertyIndent);
            ImGui.Dummy(new System.Numerics.Vector2(0f, EditorGuiTheme.CompactSpacing));
            _indented = false;
        }

        if (!_idActive)
            return;

        ImGui.PopID();
        _idActive = false;
    }

    private static bool DrawHeader(string title, bool defaultOpen)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, EditorGuiTheme.SectionHeaderPadding);
        ImGui.PushStyleColor(ImGuiCol.Header, EditorGuiTheme.SurfaceBackground);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, EditorGuiTheme.HoveredSurface);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, EditorGuiTheme.ActiveSurface);
        try
        {
            var flags = defaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
            return ImGui.CollapsingHeader(title, flags);
        }
        finally
        {
            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar();
        }
    }
}

internal ref struct EditorGuiPropertyRowScope
{
    private bool _idActive;
    private bool _tableActive;
    private bool _disabledActive;

    internal EditorGuiPropertyRowScope(
        string id,
        string label,
        bool mixed,
        bool readOnly,
        string? tooltip)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        _idActive = false;
        _tableActive = false;
        _disabledActive = false;
        IsVisible = false;

        ImGui.PushID(id);
        _idActive = true;

        var availableWidth = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        var labelWidth = Math.Clamp(
            availableWidth * EditorGuiTheme.PropertyLabelRatio,
            EditorGuiTheme.MinimumPropertyLabelWidth,
            EditorGuiTheme.PropertyLabelWidth);
        var flags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings;
        if (!ImGui.BeginTable("##Property", 2, flags))
            return;

        _tableActive = true;
        ImGui.TableSetupColumn("##Label", ImGuiTableColumnFlags.WidthFixed, labelWidth);
        ImGui.TableSetupColumn("##Control", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow(ImGuiTableRowFlags.None, EditorGuiTheme.ControlHeight);
        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();

        if (readOnly)
            ImGui.TextColored(EditorGuiTheme.MutedText, label);
        else
            ImGui.TextUnformatted(label);
        EditorGui.ShowTooltip(tooltip);

        if (mixed)
        {
            ImGui.SameLine();
            ImGui.TextColored(EditorGuiTheme.SecondaryAccent, "—");
            EditorGui.ShowTooltip("Multiple values");
        }

        ImGui.TableSetColumnIndex(1);
        ImGui.SetNextItemWidth(-1f);
        if (readOnly)
        {
            ImGui.BeginDisabled();
            _disabledActive = true;
        }

        IsVisible = true;
    }

    public bool IsVisible { get; }

    public void Dispose()
    {
        if (_disabledActive)
        {
            ImGui.EndDisabled();
            _disabledActive = false;
        }

        if (_tableActive)
        {
            ImGui.EndTable();
            _tableActive = false;
        }

        if (!_idActive)
            return;

        ImGui.PopID();
        _idActive = false;
    }
}
