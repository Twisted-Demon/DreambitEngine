using System;
using System.Xml;

namespace Dreambit.UI;

/// <summary>A button that retains an on/off checked state after activation.</summary>
public class UiToggleButton : UiButton
{
    private bool _isChecked;

    /// <summary>Raised whenever <see cref="IsChecked"/> changes.</summary>
    public event Action<UiToggleButton, bool> CheckedChanged;

    /// <summary>Gets or sets whether this button is toggled on.</summary>
    public virtual bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value) return;
            _isChecked = value;
            CheckedChanged?.Invoke(this, value);
        }
    }

    /// <summary>Gets whether activation should invert the checked state.</summary>
    protected virtual bool TogglesOnClick => true;

    /// <inheritdoc />
    protected override bool IsCheckedForVisualState => IsChecked;

    /// <inheritdoc />
    protected override void OnClick()
    {
        if (TogglesOnClick)
            IsChecked = !IsChecked;

        base.OnClick();
    }

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        base.Parse(node);
        IsChecked = UiXmlParser.ParseBool(node, "is-checked", false);
    }
}
