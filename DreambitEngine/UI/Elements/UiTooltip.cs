using System;
using System.Xml;

namespace Dreambit.UI;

/// <summary>
///     A delayed, non-interactive popup associated with another UI element through
///     its <c>Tooltip</c> property element.
/// </summary>
public sealed class UiTooltip : UiPopup
{
    private float _hoverElapsed;
    private UiElement _target;

    /// <summary>Creates a non-interactive tooltip.</summary>
    public UiTooltip()
    {
        IsEnabled = false;
        IsHitTestVisible = false;
        Placement = UiPopupPlacement.Bottom;
        VerticalOffset = 4;
        StaysOpen = true;
    }

    /// <summary>Gets or sets the hover delay in seconds.</summary>
    public float Delay { get; set; } = 0.5f;

    internal void SetTarget(UiElement target)
    {
        _target = target;
        PlacementTarget = target;
    }

    internal void UpdateForTarget(UiElement target)
    {
        if (!ReferenceEquals(_target, target))
            SetTarget(target);

        if (!target.IsPointerOver)
        {
            _hoverElapsed = 0f;
            Close();
            return;
        }

        if (IsOpen)
            return;

        _hoverElapsed += Time.UnscaledDeltaTime;
        if (_hoverElapsed >= Delay)
            Open();
    }

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        base.Parse(node);
        Delay = Math.Max(0f, UiXmlParser.ParseFloat(node, "delay", 0.5f));
        StaysOpen = true;
        IsEnabled = false;
        IsHitTestVisible = false;
    }
}