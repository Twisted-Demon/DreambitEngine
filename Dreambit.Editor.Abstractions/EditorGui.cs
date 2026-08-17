using System.Numerics;
using ImGuiNET;

namespace Dreambit.EditorApi;

public enum EditorGuiMessageKind
{
    Information,
    Success,
    Warning,
    Error
}

public enum EditorGuiReferenceAction
{
    None,
    Select,
    Clear,
    DropAccepted
}

public enum EditorGuiSpacing
{
    Compact,
    Normal,
    Section
}

/// <summary>
/// Dreambit's semantic, immediate-mode Editor UI facade.
/// </summary>
/// <remarks>
/// The facade intentionally retains no inspected objects, delegates, or collectible game types.
/// Controls report changes; callers remain responsible for routing mutations through the active
/// Editor document or <see cref="IEditorInspectorContext.RecordChange"/>.
/// </remarks>
public static partial class EditorGui
{
    public static void ApplyTheme()
    {
        ImGui.StyleColorsDark();
        var style = ImGui.GetStyle();
        style.WindowPadding = EditorGuiTheme.WindowPadding;
        style.FramePadding = EditorGuiTheme.FramePadding;
        style.CellPadding = EditorGuiTheme.CellPadding;
        style.ItemSpacing = EditorGuiTheme.ItemSpacing;
        style.ItemInnerSpacing = EditorGuiTheme.ItemInnerSpacing;
        style.ScrollbarSize = EditorGuiTheme.ScrollbarSize;
        style.GrabMinSize = EditorGuiTheme.GrabMinimumSize;
        style.IndentSpacing = 18f;
        style.ColumnsMinSpacing = 8f;
        style.WindowBorderSize = EditorGuiTheme.BorderThickness;
        style.ChildBorderSize = EditorGuiTheme.BorderThickness;
        style.PopupBorderSize = EditorGuiTheme.BorderThickness;
        style.FrameBorderSize = EditorGuiTheme.BorderThickness;
        style.TabBorderSize = 0f;
        style.WindowRounding = EditorGuiTheme.WindowRounding;
        style.ChildRounding = EditorGuiTheme.SurfaceRounding;
        style.FrameRounding = EditorGuiTheme.FrameRounding;
        style.PopupRounding = EditorGuiTheme.PopupRounding;
        style.ScrollbarRounding = EditorGuiTheme.ScrollbarRounding;
        style.GrabRounding = EditorGuiTheme.FrameRounding;
        style.TabRounding = EditorGuiTheme.FrameRounding;

        var colors = style.Colors;
        colors[(int)ImGuiCol.Text] = EditorGuiTheme.PrimaryText;
        colors[(int)ImGuiCol.TextDisabled] = EditorGuiTheme.DisabledText;
        colors[(int)ImGuiCol.WindowBg] = EditorGuiTheme.WindowBackground;
        colors[(int)ImGuiCol.ChildBg] = EditorGuiTheme.PanelBackground;
        colors[(int)ImGuiCol.PopupBg] = EditorGuiTheme.ElevatedSurface;
        colors[(int)ImGuiCol.Border] = EditorGuiTheme.Border;
        colors[(int)ImGuiCol.BorderShadow] = Vector4.Zero;
        colors[(int)ImGuiCol.FrameBg] = EditorGuiTheme.SurfaceBackground;
        colors[(int)ImGuiCol.FrameBgHovered] = EditorGuiTheme.HoveredSurface;
        colors[(int)ImGuiCol.FrameBgActive] = EditorGuiTheme.ActiveSurface;
        colors[(int)ImGuiCol.TitleBg] = EditorGuiTheme.WindowBackground;
        colors[(int)ImGuiCol.TitleBgActive] = EditorGuiTheme.PanelBackground;
        colors[(int)ImGuiCol.TitleBgCollapsed] = EditorGuiTheme.WindowBackground;
        colors[(int)ImGuiCol.MenuBarBg] = EditorGuiTheme.PanelBackground;
        colors[(int)ImGuiCol.ScrollbarBg] = EditorGuiTheme.WindowBackground;
        colors[(int)ImGuiCol.ScrollbarGrab] = EditorGuiTheme.StrongBorder;
        colors[(int)ImGuiCol.ScrollbarGrabHovered] = EditorGuiTheme.HoveredSurface;
        colors[(int)ImGuiCol.ScrollbarGrabActive] = EditorGuiTheme.PrimaryAccentActive;
        colors[(int)ImGuiCol.CheckMark] = EditorGuiTheme.PrimaryAccent;
        colors[(int)ImGuiCol.SliderGrab] = EditorGuiTheme.PrimaryAccent;
        colors[(int)ImGuiCol.SliderGrabActive] = EditorGuiTheme.PrimaryAccentHovered;
        colors[(int)ImGuiCol.Button] = EditorGuiTheme.SurfaceBackground;
        colors[(int)ImGuiCol.ButtonHovered] = EditorGuiTheme.HoveredSurface;
        colors[(int)ImGuiCol.ButtonActive] = EditorGuiTheme.ActiveSurface;
        colors[(int)ImGuiCol.Header] = EditorGuiTheme.SurfaceBackground;
        colors[(int)ImGuiCol.HeaderHovered] = EditorGuiTheme.HoveredSurface;
        colors[(int)ImGuiCol.HeaderActive] = EditorGuiTheme.SelectedBackground;
        colors[(int)ImGuiCol.Separator] = EditorGuiTheme.Border;
        colors[(int)ImGuiCol.SeparatorHovered] = EditorGuiTheme.PrimaryAccent;
        colors[(int)ImGuiCol.SeparatorActive] = EditorGuiTheme.PrimaryAccentActive;
        colors[(int)ImGuiCol.ResizeGrip] = WithAlpha(EditorGuiTheme.PrimaryAccent, 0.22f);
        colors[(int)ImGuiCol.ResizeGripHovered] = WithAlpha(EditorGuiTheme.PrimaryAccent, 0.65f);
        colors[(int)ImGuiCol.ResizeGripActive] = EditorGuiTheme.PrimaryAccent;
        colors[(int)ImGuiCol.Tab] = EditorGuiTheme.WindowBackground;
        colors[(int)ImGuiCol.TabHovered] = EditorGuiTheme.HoveredSurface;
        colors[(int)ImGuiCol.TabSelected] = EditorGuiTheme.SurfaceBackground;
        colors[(int)ImGuiCol.TabSelectedOverline] = EditorGuiTheme.PrimaryAccent;
        colors[(int)ImGuiCol.TabDimmed] = EditorGuiTheme.WindowBackground;
        colors[(int)ImGuiCol.TabDimmedSelected] = EditorGuiTheme.SurfaceBackground;
        colors[(int)ImGuiCol.DockingPreview] = WithAlpha(EditorGuiTheme.PrimaryAccent, 0.68f);
        colors[(int)ImGuiCol.DockingEmptyBg] = EditorGuiTheme.WindowBackground;
        colors[(int)ImGuiCol.TableHeaderBg] = EditorGuiTheme.SurfaceBackground;
        colors[(int)ImGuiCol.TableBorderStrong] = EditorGuiTheme.StrongBorder;
        colors[(int)ImGuiCol.TableBorderLight] = EditorGuiTheme.Border;
        colors[(int)ImGuiCol.TableRowBgAlt] = WithAlpha(EditorGuiTheme.ElevatedSurface, 0.34f);
        colors[(int)ImGuiCol.TextLink] = EditorGuiTheme.PrimaryAccent;
        colors[(int)ImGuiCol.TextSelectedBg] = WithAlpha(EditorGuiTheme.PrimaryAccent, 0.34f);
        colors[(int)ImGuiCol.DragDropTarget] = EditorGuiTheme.SecondaryAccent;
        colors[(int)ImGuiCol.NavCursor] = EditorGuiTheme.PrimaryAccent;
        colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.015f, 0.020f, 0.028f, 0.72f);
    }

    public static EditorGuiIdScope PushId(string id) => new(id);

    public static EditorGuiDisabledScope Disabled(bool disabled = true) => new(disabled);

    public static EditorGuiMutedScope Muted(bool muted = true) => new(muted);

    public static EditorGuiSectionScope Section(
        string id,
        string title,
        bool defaultOpen = true,
        string? description = null,
        bool allowRemove = false,
        string? statusText = null) =>
        new(id, title, defaultOpen, description, allowRemove, statusText);

    public static bool Property(
        string id,
        string label,
        ref bool value,
        bool mixed = false,
        bool readOnly = false,
        string? tooltip = null)
    {
        using var row = new EditorGuiPropertyRowScope(id, label, mixed, readOnly, tooltip);
        if (!row.IsVisible)
            return false;

        var changed = ImGui.Checkbox("##Value", ref value);
        ShowTooltip(tooltip);
        return changed && !readOnly;
    }

    public static bool Property(
        string id,
        string label,
        ref int value,
        float speed = 1f,
        int min = 0,
        int max = 0,
        string format = "%d",
        bool mixed = false,
        bool readOnly = false,
        string? tooltip = null)
    {
        using var row = new EditorGuiPropertyRowScope(id, label, mixed, readOnly, tooltip);
        if (!row.IsVisible)
            return false;

        var changed = ImGui.DragInt("##Value", ref value, speed, min, max, format);
        ShowTooltip(tooltip);
        return changed && !readOnly;
    }

    public static bool Property(
        string id,
        string label,
        ref float value,
        float speed = 0.1f,
        float min = 0f,
        float max = 0f,
        string format = "%.3f",
        bool mixed = false,
        bool readOnly = false,
        string? tooltip = null)
    {
        using var row = new EditorGuiPropertyRowScope(id, label, mixed, readOnly, tooltip);
        if (!row.IsVisible)
            return false;

        var changed = ImGui.DragFloat("##Value", ref value, speed, min, max, format);
        ShowTooltip(tooltip);
        return changed && !readOnly;
    }

    public static bool Property(
        string id,
        string label,
        ref double value,
        double step = 0d,
        double stepFast = 0d,
        string format = "%.6f",
        bool mixed = false,
        bool readOnly = false,
        string? tooltip = null)
    {
        using var row = new EditorGuiPropertyRowScope(id, label, mixed, readOnly, tooltip);
        if (!row.IsVisible)
            return false;

        var changed = ImGui.InputDouble("##Value", ref value, step, stepFast, format);
        ShowTooltip(tooltip);
        return changed && !readOnly;
    }

    public static bool Property(
        string id,
        string label,
        ref string value,
        uint maxLength = 1024,
        string? hint = null,
        bool commitOnEnter = false,
        bool mixed = false,
        bool readOnly = false,
        string? tooltip = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (maxLength < 2)
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Text capacity must be at least two characters.");

        using var row = new EditorGuiPropertyRowScope(id, label, mixed, readOnly, tooltip);
        if (!row.IsVisible)
            return false;

        var flags = commitOnEnter ? ImGuiInputTextFlags.EnterReturnsTrue : ImGuiInputTextFlags.None;
        var changed = string.IsNullOrEmpty(hint)
            ? ImGui.InputText("##Value", ref value, maxLength, flags)
            : ImGui.InputTextWithHint("##Value", hint, ref value, maxLength, flags);
        ShowTooltip(tooltip);
        return changed && !readOnly;
    }

    public static bool Property(
        string id,
        string label,
        ref Vector2 value,
        float speed = 0.1f,
        float min = 0f,
        float max = 0f,
        string format = "%.3f",
        bool mixed = false,
        bool readOnly = false,
        string? tooltip = null)
    {
        using var row = new EditorGuiPropertyRowScope(id, label, mixed, readOnly, tooltip);
        if (!row.IsVisible)
            return false;

        var changed = DrawVector2(ref value, speed, min, max, format);
        ShowTooltip(tooltip);
        return changed && !readOnly;
    }

    public static bool Property(
        string id,
        string label,
        ref Vector3 value,
        float speed = 0.1f,
        float min = 0f,
        float max = 0f,
        string format = "%.3f",
        bool mixed = false,
        bool readOnly = false,
        string? tooltip = null)
    {
        using var row = new EditorGuiPropertyRowScope(id, label, mixed, readOnly, tooltip);
        if (!row.IsVisible)
            return false;

        var changed = DrawVector3(ref value, speed, min, max, format);
        ShowTooltip(tooltip);
        return changed && !readOnly;
    }

    public static bool Property(
        string id,
        string label,
        ref Vector4 value,
        float speed = 0.1f,
        float min = 0f,
        float max = 0f,
        string format = "%.3f",
        bool mixed = false,
        bool readOnly = false,
        string? tooltip = null)
    {
        using var row = new EditorGuiPropertyRowScope(id, label, mixed, readOnly, tooltip);
        if (!row.IsVisible)
            return false;

        var changed = ImGui.DragFloat4("##Value", ref value, speed, min, max, format);
        ShowTooltip(tooltip);
        return changed && !readOnly;
    }

    public static bool ColorProperty(
        string id,
        string label,
        ref Vector4 value,
        bool includeAlpha = true,
        bool mixed = false,
        bool readOnly = false,
        string? tooltip = null)
    {
        using var row = new EditorGuiPropertyRowScope(id, label, mixed, readOnly, tooltip);
        if (!row.IsVisible)
            return false;

        var flags = includeAlpha ? ImGuiColorEditFlags.None : ImGuiColorEditFlags.NoAlpha;
        var changed = ImGui.ColorEdit4("##Value", ref value, flags);
        ShowTooltip(tooltip);
        return changed && !readOnly;
    }

    public static bool ChoiceProperty(
        string id,
        string label,
        ref int selectedIndex,
        IReadOnlyList<string> choices,
        bool mixed = false,
        bool readOnly = false,
        string? tooltip = null)
    {
        ArgumentNullException.ThrowIfNull(choices);
        using var row = new EditorGuiPropertyRowScope(id, label, mixed, readOnly, tooltip);
        if (!row.IsVisible)
            return false;

        var preview = mixed
            ? "Multiple values"
            : selectedIndex >= 0 && selectedIndex < choices.Count
                ? choices[selectedIndex]
                : "None";
        var changed = false;
        if (ImGui.BeginCombo("##Value", preview))
        {
            try
            {
                for (var index = 0; index < choices.Count; index++)
                {
                    using var optionId = PushId(index.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    var selected = index == selectedIndex;
                    if (ImGui.Selectable(choices[index], selected) && !readOnly)
                    {
                        selectedIndex = index;
                        changed = true;
                    }
                    if (selected)
                        ImGui.SetItemDefaultFocus();
                }
            }
            finally
            {
                ImGui.EndCombo();
            }
        }
        ShowTooltip(tooltip);
        return changed;
    }

    public static bool EnumProperty<TEnum>(
        string id,
        string label,
        ref TEnum value,
        IReadOnlyList<TEnum> choices,
        Func<TEnum, string>? displayName = null,
        bool mixed = false,
        bool readOnly = false,
        string? tooltip = null)
        where TEnum : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(choices);
        using var row = new EditorGuiPropertyRowScope(id, label, mixed, readOnly, tooltip);
        if (!row.IsVisible)
            return false;

        var preview = mixed ? "Multiple values" : FormatEnum(value, displayName);
        var changed = false;
        if (ImGui.BeginCombo("##Value", preview))
        {
            try
            {
                for (var index = 0; index < choices.Count; index++)
                {
                    using var optionId = PushId(index.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    var option = choices[index];
                    var selected = EqualityComparer<TEnum>.Default.Equals(option, value);
                    if (ImGui.Selectable(FormatEnum(option, displayName), selected) && !readOnly)
                    {
                        value = option;
                        changed = true;
                    }
                    if (selected)
                        ImGui.SetItemDefaultFocus();
                }
            }
            finally
            {
                ImGui.EndCombo();
            }
        }
        ShowTooltip(tooltip);
        return changed;
    }

    public static bool CustomProperty(
        string id,
        string label,
        Func<bool> drawControl,
        bool mixed = false,
        bool readOnly = false,
        string? tooltip = null)
    {
        ArgumentNullException.ThrowIfNull(drawControl);
        using var row = new EditorGuiPropertyRowScope(id, label, mixed, readOnly, tooltip);
        if (!row.IsVisible)
            return false;

        var changed = drawControl();
        ShowTooltip(tooltip);
        return changed && !readOnly;
    }

    public static void ReadOnlyProperty(
        string id,
        string label,
        string? value,
        bool mixed = false,
        string? tooltip = null)
    {
        using var row = new EditorGuiPropertyRowScope(id, label, mixed, true, tooltip);
        if (!row.IsVisible)
            return;

        ImGui.TextWrapped(mixed ? "Multiple values" : value ?? "None");
        ShowTooltip(tooltip);
    }

    /// <summary>
    /// Draws a reference selector. <paramref name="acceptDrop"/> is invoked immediately after the
    /// value button, while that button is still the last ImGui item, so it can safely establish a
    /// drag/drop target.
    /// </summary>
    public static EditorGuiReferenceAction ReferenceProperty(
        string id,
        string label,
        string? displayValue,
        bool mixed = false,
        bool readOnly = false,
        bool canClear = true,
        Func<bool>? acceptDrop = null,
        string? tooltip = null)
    {
        using var row = new EditorGuiPropertyRowScope(id, label, mixed, readOnly, tooltip);
        if (!row.IsVisible)
            return EditorGuiReferenceAction.None;

        var clearWidth = ImGui.GetFrameHeight();
        var available = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        var valueWidth = canClear
            ? MathF.Max(1f, available - clearWidth - ImGui.GetStyle().ItemSpacing.X)
            : -1f;
        var valueText = mixed ? "Multiple values" : displayValue ?? "None";
        var select = ImGui.Button(valueText, new Vector2(valueWidth, 0f));
        ShowTooltip(tooltip);

        var dropAccepted = !readOnly && acceptDrop?.Invoke() == true;

        var clear = false;
        if (canClear)
        {
            ImGui.SameLine();
            using var clearId = PushId("Clear");
            using var disabled = Disabled(readOnly || string.IsNullOrEmpty(displayValue));
            clear = ImGui.Button("×", new Vector2(clearWidth, 0f));
            ShowTooltip("Clear reference");
        }

        if (dropAccepted)
            return EditorGuiReferenceAction.DropAccepted;
        if (clear && !readOnly)
            return EditorGuiReferenceAction.Clear;
        return select && !readOnly
            ? EditorGuiReferenceAction.Select
            : EditorGuiReferenceAction.None;
    }

    public static bool Button(
        string id,
        string label,
        Vector2 size = default,
        bool primary = false,
        bool enabled = true,
        string? tooltip = null)
    {
        using var idScope = PushId(id);
        using var disabled = Disabled(!enabled);
        if (primary)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, EditorGuiTheme.PrimaryAccentActive);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, EditorGuiTheme.PrimaryAccent);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, EditorGuiTheme.PrimaryAccentHovered);
        }

        try
        {
            var clicked = ImGui.Button(label, size);
            ShowTooltip(tooltip);
            return clicked && enabled;
        }
        finally
        {
            if (primary)
                ImGui.PopStyleColor(3);
        }
    }

    public static bool FullWidthButton(
        string id,
        string label,
        bool primary = false,
        bool enabled = true,
        string? tooltip = null) =>
        Button(id, label, new Vector2(-1f, 0f), primary, enabled, tooltip);

    public static bool SearchInput(
        string id,
        string hint,
        ref string value,
        uint maxLength = 256,
        bool readOnly = false,
        string? tooltip = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(hint);
        if (maxLength < 2)
            throw new ArgumentOutOfRangeException(nameof(maxLength));

        using var idScope = PushId(id);
        using var disabled = Disabled(readOnly);
        ImGui.SetNextItemWidth(-1f);
        var changed = ImGui.InputTextWithHint("##Search", hint, ref value, maxLength);
        ShowTooltip(tooltip);
        return changed && !readOnly;
    }

    public static void Header(string title, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ImGui.PushStyleColor(ImGuiCol.Text, EditorGuiTheme.PrimaryText);
        try
        {
            ImGui.SeparatorText(title);
        }
        finally
        {
            ImGui.PopStyleColor();
        }

        if (!string.IsNullOrWhiteSpace(description))
            MutedText(description, wrapped: true);
    }

    public static void Text(string text) => ImGui.TextUnformatted(text ?? string.Empty);

    public static void WrappedText(string text) => ImGui.TextWrapped(text ?? string.Empty);

    public static void MutedText(string text, bool wrapped = false)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, EditorGuiTheme.MutedText);
        try
        {
            if (wrapped)
                ImGui.TextWrapped(text ?? string.Empty);
            else
                ImGui.TextUnformatted(text ?? string.Empty);
        }
        finally
        {
            ImGui.PopStyleColor();
        }
    }

    public static void Message(EditorGuiMessageKind kind, string message)
    {
        var color = kind switch
        {
            EditorGuiMessageKind.Success => EditorGuiTheme.Success,
            EditorGuiMessageKind.Warning => EditorGuiTheme.Warning,
            EditorGuiMessageKind.Error => EditorGuiTheme.Error,
            _ => EditorGuiTheme.PrimaryAccent
        };
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        try
        {
            ImGui.TextWrapped(message ?? string.Empty);
        }
        finally
        {
            ImGui.PopStyleColor();
        }
    }

    public static void Error(string message) => Message(EditorGuiMessageKind.Error, message);

    public static void Warning(string message) => Message(EditorGuiMessageKind.Warning, message);

    public static void Success(string message) => Message(EditorGuiMessageKind.Success, message);

    public static void Separator() => ImGui.Separator();

    public static void Space(EditorGuiSpacing spacing = EditorGuiSpacing.Normal)
    {
        var height = spacing switch
        {
            EditorGuiSpacing.Compact => EditorGuiTheme.CompactSpacing,
            EditorGuiSpacing.Section => EditorGuiTheme.SectionSpacing,
            _ => EditorGuiTheme.NormalSpacing
        };
        ImGui.Dummy(new Vector2(0f, height));
    }

    public static void ShowTooltip(string? tooltip)
    {
        if (!string.IsNullOrWhiteSpace(tooltip) && ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);
    }

    private static string FormatEnum<TEnum>(TEnum value, Func<TEnum, string>? displayName)
        where TEnum : struct, Enum =>
        displayName?.Invoke(value) ?? value.ToString();

    private static bool DrawVector2(
        ref Vector2 value,
        float speed,
        float min,
        float max,
        string format)
    {
        var width = GetVectorComponentWidth(2);
        var changed = DrawVectorAxis("X", ref value.X, width, speed, min, max, format);
        ImGui.SameLine(0f, ImGui.GetStyle().ItemSpacing.X);
        return DrawVectorAxis("Y", ref value.Y, width, speed, min, max, format) || changed;
    }

    private static bool DrawVector3(
        ref Vector3 value,
        float speed,
        float min,
        float max,
        string format)
    {
        var width = GetVectorComponentWidth(3);
        var changed = DrawVectorAxis("X", ref value.X, width, speed, min, max, format);
        ImGui.SameLine(0f, ImGui.GetStyle().ItemSpacing.X);
        changed |= DrawVectorAxis("Y", ref value.Y, width, speed, min, max, format);
        ImGui.SameLine(0f, ImGui.GetStyle().ItemSpacing.X);
        return DrawVectorAxis("Z", ref value.Z, width, speed, min, max, format) || changed;
    }

    private static bool DrawVectorAxis(
        string axis,
        ref float value,
        float width,
        float speed,
        float min,
        float max,
        string format)
    {
        ImGui.AlignTextToFramePadding();
        using (Muted())
            ImGui.TextUnformatted(axis);
        ImGui.SameLine(0f, EditorGuiTheme.VectorAxisLabelSpacing);
        ImGui.SetNextItemWidth(width);
        return ImGui.DragFloat($"##{axis}", ref value, speed, min, max, format);
    }

    private static float GetVectorComponentWidth(int componentCount)
    {
        var style = ImGui.GetStyle();
        var axisWidth = ImGui.CalcTextSize("X").X + EditorGuiTheme.VectorAxisLabelSpacing;
        var spacing = style.ItemSpacing.X * (componentCount - 1);
        var available = ImGui.GetContentRegionAvail().X;
        return MathF.Max(
            EditorGuiTheme.MinimumVectorComponentWidth,
            (available - axisWidth * componentCount - spacing) / componentCount);
    }

    private static Vector4 WithAlpha(Vector4 color, float alpha) =>
        new(color.X, color.Y, color.Z, alpha);
}
