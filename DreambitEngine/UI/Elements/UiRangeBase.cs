using System;
using System.Xml;

namespace Dreambit.UI;

/// <summary>
/// Base class for controls that expose a clamped numeric value between a
/// minimum and maximum, including sliders, scrollbars, and progress bars.
/// </summary>
public abstract class UiRangeBase : UiControl
{
    private float _minimum;
    private float _maximum = 100f;
    private float _value;
    private float _step = 1f;

    /// <summary>Raised whenever the effective value changes.</summary>
    public event Action<UiRangeBase, float> ValueChanged;

    /// <summary>Gets or sets the inclusive lower bound.</summary>
    public float Minimum
    {
        get => _minimum;
        set
        {
            if (Math.Abs(_minimum - value) < float.Epsilon) return;
            _minimum = value;
            if (_maximum < _minimum) _maximum = _minimum;
            Value = _value;
        }
    }

    /// <summary>Gets or sets the inclusive upper bound.</summary>
    public float Maximum
    {
        get => _maximum;
        set
        {
            var next = Math.Max(_minimum, value);
            if (Math.Abs(_maximum - next) < float.Epsilon) return;
            _maximum = next;
            Value = _value;
        }
    }

    /// <summary>Gets or sets the current clamped value.</summary>
    public float Value
    {
        get => _value;
        set
        {
            var next = Math.Clamp(value, Minimum, Maximum);
            if (Math.Abs(_value - next) < float.Epsilon) return;
            _value = next;
            ValueChanged?.Invoke(this, _value);
        }
    }

    /// <summary>Gets or sets the increment used by keyboard/controller input.</summary>
    public float Step
    {
        get => _step;
        set => _step = Math.Max(0f, value);
    }

    /// <summary>Gets the value normalized to the zero-to-one range.</summary>
    public float NormalizedValue => Maximum <= Minimum
        ? 0f
        : (Value - Minimum) / (Maximum - Minimum);

    /// <summary>Sets a value from a normalized zero-to-one position.</summary>
    public void SetNormalizedValue(float normalizedValue)
    {
        var raw = Minimum + Math.Clamp(normalizedValue, 0f, 1f) *
            (Maximum - Minimum);
        Value = Step > 0f
            ? Minimum + MathF.Round((raw - Minimum) / Step) * Step
            : raw;
    }

    /// <inheritdoc />
    public override void Parse(XmlNode node)
    {
        base.Parse(node);
        Minimum = UiXmlParser.ParseFloat(node, "minimum", 0f);
        Maximum = UiXmlParser.ParseFloat(node, "maximum", 100f);
        Step = UiXmlParser.ParseFloat(node, "step", 1f);
        Value = UiXmlParser.ParseFloat(node, "value", Minimum);
    }
}
