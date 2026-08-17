using System.Numerics;
using ImGuiNET;

namespace Dreambit.EditorApi;

/// <summary>Balanced containers and list/action primitives used by editor surfaces.</summary>
public static partial class EditorGui
{
    public static EditorGuiWindowScope Window(
        string name,
        ref bool open,
        ImGuiWindowFlags flags = ImGuiWindowFlags.None) => new(name, ref open, flags);

    public static EditorGuiWindowScope Window(
        string name,
        ImGuiWindowFlags flags = ImGuiWindowFlags.None) => new(name, flags);

    public static EditorGuiChildScope Child(
        string id,
        Vector2 size = default,
        ImGuiChildFlags childFlags = ImGuiChildFlags.None,
        ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None) =>
        new(id, size, childFlags, windowFlags);

    public static EditorGuiPopupScope Popup(
        string id,
        ImGuiWindowFlags flags = ImGuiWindowFlags.None) => new(id, false, flags);

    public static EditorGuiPopupScope Modal(
        string id,
        ImGuiWindowFlags flags = ImGuiWindowFlags.AlwaysAutoResize) => new(id, true, flags);

    public static EditorGuiPopupScope Modal(
        string id,
        ref bool open,
        ImGuiWindowFlags flags = ImGuiWindowFlags.AlwaysAutoResize) => new(id, ref open, flags);

    public static EditorGuiPopupScope ContextMenu(
        string id,
        ImGuiPopupFlags flags = ImGuiPopupFlags.MouseButtonRight) => new(id, flags);

    public static EditorGuiPopupScope ContextWindow(
        string id,
        ImGuiPopupFlags flags = ImGuiPopupFlags.MouseButtonRight) => new(id, flags, true);

    public static EditorGuiMenuScope Menu(string label, bool enabled = true) => new(label, enabled);

    public static EditorGuiMenuBarScope MenuBar() => new(true);

    public static EditorGuiItemWidthScope ItemWidth(float width) => new(width);

    public static EditorGuiTooltipScope Tooltip() => new(true);

    public static EditorGuiTextWrapScope TextWrap(float wrapPosition = 0f) => new(wrapPosition);

    /// <summary>Begins a stable-ID, balanced collapsible group for nested editor values.</summary>
    public static EditorGuiCollapsibleGroupScope CollapsibleGroup(
        string id,
        string label,
        bool defaultOpen = false,
        string? tooltip = null) => new(id, label, defaultOpen, tooltip);

    /// <summary>Applies Dreambit's compact breadcrumb spacing for the lifetime of the scope.</summary>
    public static EditorGuiBreadcrumbScope Breadcrumbs() => new(true);

    public static bool BreadcrumbButton(string id, string label)
    {
        using var scope = PushId(id);
        return ImGui.SmallButton(label);
    }

    public static void OpenPopup(string id) => ImGui.OpenPopup(id);

    public static void ClosePopup() => ImGui.CloseCurrentPopup();

    public static bool MenuItem(
        string label,
        string shortcut = "",
        bool selected = false,
        bool enabled = true) => ImGui.MenuItem(label, shortcut, selected, enabled);

    public static bool MenuItem(string label, ref bool selected, bool enabled = true) =>
        ImGui.MenuItem(label, string.Empty, ref selected, enabled);

    public static bool Selectable(
        string id,
        string label,
        bool selected = false,
        ImGuiSelectableFlags flags = ImGuiSelectableFlags.None,
        Vector2 size = default)
    {
        using var scope = PushId(id);
        return ImGui.Selectable(label, selected, flags, size);
    }

    public static bool InputText(
        string id,
        string label,
        ref string value,
        uint maxLength = 256,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None,
        float width = -1f,
        string? hint = null,
        string? tooltip = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        using var scope = PushId(id);
        ImGui.SetNextItemWidth(width);
        var changed = string.IsNullOrWhiteSpace(hint)
            ? ImGui.InputText(label, ref value, maxLength, flags)
            : ImGui.InputTextWithHint(label, hint, ref value, maxLength, flags);
        ShowTooltip(tooltip);
        return changed;
    }

    public static bool Checkbox(
        string id,
        string label,
        ref bool value,
        string? tooltip = null)
    {
        using var scope = PushId(id);
        var changed = ImGui.Checkbox(label, ref value);
        ShowTooltip(tooltip);
        return changed;
    }

    public static bool CompactFloat(
        string id,
        ref float value,
        float width = 80f,
        float speed = 0.1f,
        float min = 0f,
        float max = 0f,
        string format = "%.3f",
        string? tooltip = null)
    {
        using var scope = PushId(id);
        ImGui.SetNextItemWidth(width);
        var changed = ImGui.DragFloat("##Value", ref value, speed, min, max, format);
        ShowTooltip(tooltip);
        return changed;
    }

    public static void Inline(float offsetFromStart = 0f, float spacing = -1f) =>
        ImGui.SameLine(offsetFromStart, spacing);
}

public ref struct EditorGuiWindowScope
{
    public bool IsVisible { get; }

    internal EditorGuiWindowScope(string name, ref bool open, ImGuiWindowFlags flags)
    {
        IsVisible = ImGui.Begin(name, ref open, flags);
    }

    internal EditorGuiWindowScope(string name, ImGuiWindowFlags flags)
    {
        IsVisible = ImGui.Begin(name, flags);
    }

    public void Dispose() => ImGui.End();
}

public ref struct EditorGuiChildScope
{
    public bool IsVisible { get; }

    internal EditorGuiChildScope(
        string id,
        Vector2 size,
        ImGuiChildFlags childFlags,
        ImGuiWindowFlags windowFlags)
    {
        IsVisible = ImGui.BeginChild(id, size, childFlags, windowFlags);
    }

    public void Dispose() => ImGui.EndChild();
}

public ref struct EditorGuiPopupScope
{
    private bool _active;
    public bool IsOpen { get; }

    internal EditorGuiPopupScope(string id, bool modal, ImGuiWindowFlags flags)
    {
        IsOpen = modal ? ImGui.BeginPopupModal(id, flags) : ImGui.BeginPopup(id, flags);
        _active = IsOpen;
    }

    internal EditorGuiPopupScope(string id, ref bool open, ImGuiWindowFlags flags)
    {
        IsOpen = ImGui.BeginPopupModal(id, ref open, flags);
        _active = IsOpen;
    }

    internal EditorGuiPopupScope(string id, ImGuiPopupFlags flags)
    {
        IsOpen = ImGui.BeginPopupContextItem(id, flags);
        _active = IsOpen;
    }

    internal EditorGuiPopupScope(string id, ImGuiPopupFlags flags, bool window)
    {
        IsOpen = window
            ? ImGui.BeginPopupContextWindow(id, flags)
            : ImGui.BeginPopupContextItem(id, flags);
        _active = IsOpen;
    }

    public void Dispose()
    {
        if (!_active)
            return;
        ImGui.EndPopup();
        _active = false;
    }
}

public ref struct EditorGuiMenuScope
{
    private bool _active;
    public bool IsOpen { get; }

    internal EditorGuiMenuScope(string label, bool enabled)
    {
        IsOpen = ImGui.BeginMenu(label, enabled);
        _active = IsOpen;
    }

    public void Dispose()
    {
        if (!_active)
            return;
        ImGui.EndMenu();
        _active = false;
    }
}

public ref struct EditorGuiMenuBarScope
{
    private bool _active;
    public bool IsOpen { get; }

    internal EditorGuiMenuBarScope(bool _)
    {
        IsOpen = ImGui.BeginMenuBar();
        _active = IsOpen;
    }

    public void Dispose()
    {
        if (!_active)
            return;
        ImGui.EndMenuBar();
        _active = false;
    }
}

public ref struct EditorGuiItemWidthScope
{
    private bool _active;

    internal EditorGuiItemWidthScope(float width)
    {
        ImGui.PushItemWidth(width);
        _active = true;
    }

    public void Dispose()
    {
        if (!_active)
            return;
        ImGui.PopItemWidth();
        _active = false;
    }
}

public ref struct EditorGuiTooltipScope
{
    private bool _active;

    internal EditorGuiTooltipScope(bool _)
    {
        ImGui.BeginTooltip();
        _active = true;
    }

    public void Dispose()
    {
        if (!_active)
            return;
        ImGui.EndTooltip();
        _active = false;
    }
}

public ref struct EditorGuiTextWrapScope
{
    private bool _active;

    internal EditorGuiTextWrapScope(float wrapPosition)
    {
        ImGui.PushTextWrapPos(wrapPosition);
        _active = true;
    }

    public void Dispose()
    {
        if (!_active)
            return;
        ImGui.PopTextWrapPos();
        _active = false;
    }
}

public ref struct EditorGuiCollapsibleGroupScope
{
    private bool _idActive;

    public bool IsOpen { get; }

    internal EditorGuiCollapsibleGroupScope(
        string id,
        string label,
        bool defaultOpen,
        string? tooltip)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        ImGui.PushID(id);
        _idActive = true;
        var flags = ImGuiTreeNodeFlags.SpanAvailWidth;
        if (defaultOpen)
            flags |= ImGuiTreeNodeFlags.DefaultOpen;
        IsOpen = ImGui.TreeNodeEx(label, flags);
        EditorGui.ShowTooltip(tooltip);
    }

    public void Dispose()
    {
        if (!_idActive)
            return;
        if (IsOpen)
            ImGui.TreePop();
        ImGui.PopID();
        _idActive = false;
    }
}

public ref struct EditorGuiBreadcrumbScope
{
    private bool _active;

    internal EditorGuiBreadcrumbScope(bool _)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, EditorGuiTheme.BreadcrumbSpacing);
        _active = true;
    }

    public void Dispose()
    {
        if (!_active)
            return;
        ImGui.PopStyleVar();
        _active = false;
    }
}
