using System;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>Creates the arbitrary visual content for a control.</summary>
/// <param name="owner">The control receiving the generated content.</param>
/// <returns>The root element of the generated visual tree.</returns>
public delegate UiElement UiControlTemplate(UiControl owner);

/// <summary>
/// Base class for interactive single-content controls with composable
/// backgrounds, reusable templates, and consistent visual states.
/// </summary>
public class UiControl : UiContentControl
{
    private UiControlTemplate _template;

    /// <summary>Gets the state-specific style used by this control.</summary>
    public UiControlStyle Style { get; } = new();

    /// <inheritdoc />
    public override Color BackgroundTint
    {
        get => base.BackgroundTint;
        set
        {
            base.BackgroundTint = value;
            Style.NormalTint = value;
        }
    }

    /// <summary>Gets or sets a function that creates this control's arbitrary content.</summary>
    public UiControlTemplate Template
    {
        get => _template;
        set
        {
            if (ReferenceEquals(_template, value))
                return;

            _template = value;
            ApplyTemplate();
        }
    }

    /// <summary>Gets the control's current combined visual state.</summary>
    public UiControlState VisualState
    {
        get
        {
            var state = UiControlState.Normal;
            if (!IsEffectivelyEnabled) state |= UiControlState.Disabled;
            if (IsPointerOver) state |= UiControlState.Hovered;
            if (IsFocused) state |= UiControlState.Focused;
            if (IsPressedForVisualState) state |= UiControlState.Pressed;
            if (IsCheckedForVisualState) state |= UiControlState.Checked;
            if (IsSelected) state |= UiControlState.Selected;
            if (IsOpenForVisualState) state |= UiControlState.Open;
            return state;
        }
    }

    /// <summary>Gets whether this control is selected by a selector.</summary>
    public bool IsSelected { get; internal set; }

    /// <summary>Gets or sets the hover tint.</summary>
    public Color HoverTint
    {
        get => Style.HoveredTint ?? Style.NormalTint;
        set => Style.HoveredTint = value;
    }

    /// <summary>Gets or sets the pressed tint.</summary>
    public Color PressedTint
    {
        get => Style.PressedTint ?? Style.NormalTint;
        set => Style.PressedTint = value;
    }

    /// <summary>Gets or sets the focused tint.</summary>
    public Color FocusedTint
    {
        get => Style.FocusedTint ?? Style.NormalTint;
        set => Style.FocusedTint = value;
    }

    /// <summary>Gets or sets the disabled tint.</summary>
    public Color DisabledTint
    {
        get => Style.DisabledTint ?? Style.NormalTint;
        set => Style.DisabledTint = value;
    }

    /// <summary>Gets or sets the checked tint.</summary>
    public Color CheckedTint
    {
        get => Style.CheckedTint ?? Style.NormalTint;
        set => Style.CheckedTint = value;
    }

    /// <summary>Gets or sets the selected tint.</summary>
    public Color SelectedTint
    {
        get => Style.SelectedTint ?? Style.NormalTint;
        set => Style.SelectedTint = value;
    }

    /// <summary>Gets whether the control should use its pressed state.</summary>
    protected virtual bool IsPressedForVisualState => false;
    /// <summary>Gets whether the control should use its checked state.</summary>
    protected virtual bool IsCheckedForVisualState => false;
    /// <summary>Gets whether the control should use its expanded state.</summary>
    protected virtual bool IsOpenForVisualState => false;

    /// <summary>Regenerates content from <see cref="Template"/>.</summary>
    public void ApplyTemplate()
    {
        if (Template is null)
            return;

        SetContent(Template(this));
    }

    /// <inheritdoc />
    protected override Color GetBackgroundTint()
    {
        return Style.Resolve(VisualState);
    }

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        base.Parse(node);
        ParseOptionalColor(node, "hover-tint", value => Style.HoveredTint = value);
        ParseOptionalColor(node, "pressed-tint", value => Style.PressedTint = value);
        ParseOptionalColor(node, "focused-tint", value => Style.FocusedTint = value);
        ParseOptionalColor(node, "disabled-tint", value => Style.DisabledTint = value);
        ParseOptionalColor(node, "checked-tint", value => Style.CheckedTint = value);
        ParseOptionalColor(node, "selected-tint", value => Style.SelectedTint = value);
        ParseOptionalColor(node, "open-tint", value => Style.OpenTint = value);
    }

    private static void ParseOptionalColor(
        XmlNode node,
        string attribute,
        Action<Color> setter)
    {
        if (node.Attributes?[attribute] is not null)
            setter(UiXmlParser.ParseColor(node, attribute));
    }
}
