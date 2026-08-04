using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>A mutually exclusive checked button within a named group.</summary>
public sealed class UiRadioButton : UiCheckBox
{
    /// <summary>Gets or sets the group whose checked item is exclusive.</summary>
    public string GroupName { get; set; } = string.Empty;

    /// <inheritdoc />
    public override bool IsChecked
    {
        get => base.IsChecked;
        set
        {
            if (value)
                UncheckGroupPeers();
            base.IsChecked = value;
        }
    }

    /// <inheritdoc />
    protected override bool TogglesOnClick => false;

    /// <inheritdoc />
    protected override void OnClick()
    {
        CheckExclusively();
        base.OnClick();
    }

    /// <inheritdoc />
    protected override void DrawIndicator(Rectangle indicatorBounds)
    {
        var radius = indicatorBounds.Width * 0.5f;
        Graphics.SpriteBatch.DrawFilledEllipse(
            indicatorBounds.Center.ToVector2(),
            new Vector2(radius, radius),
            IndicatorTint);
        if (!IsChecked)
            return;

        var markRadius = Math.Max(1f, radius * 0.5f);
        Graphics.SpriteBatch.DrawFilledEllipse(
            indicatorBounds.Center.ToVector2(),
            new Vector2(markRadius, markRadius),
            MarkTint);
    }

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        base.Parse(node);
        GroupName = UiXmlParser.ParseString(node, "group", string.Empty);
    }

    private void CheckExclusively()
    {
        IsChecked = true;
    }

    /// <inheritdoc />
    protected override void OnAttachedToLayout(
        UiLayout previousLayout,
        UiLayout currentLayout)
    {
        if (currentLayout is not null && IsChecked)
            UncheckGroupPeers();
    }

    private void UncheckGroupPeers()
    {
        if (Layout is not null)
            foreach (var radio in Enumerate(Layout.Root))
                if (!ReferenceEquals(radio, this) &&
                    string.Equals(
                        radio.GroupName,
                        GroupName,
                        StringComparison.Ordinal))
                    radio.IsChecked = false;
    }

    private static IEnumerable<UiRadioButton> Enumerate(UiElement element)
    {
        if (element is null)
            yield break;

        if (element is UiRadioButton radio)
            yield return radio;
        foreach (var child in element.Children)
        foreach (var descendant in Enumerate(child))
            yield return descendant;
    }
}