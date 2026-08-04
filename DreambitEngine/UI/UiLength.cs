namespace Dreambit.UI;

/// <summary>
///     Represents a UI dimension expressed as pixels, a parent-relative
///     percentage, or automatic content size.
/// </summary>
public struct UiLength
{
    /// <summary>Gets or sets the numeric pixel or normalized percentage value.</summary>
    public float Value;

    /// <summary>Gets or sets whether <see cref="Value" /> is a normalized percentage.</summary>
    public bool IsPercent;

    /// <summary>Gets or sets whether the dimension is measured from its content.</summary>
    public bool IsAuto;

    /// <summary>Creates a pixel or percentage UI length.</summary>
    /// <param name="value">The numeric length value.</param>
    /// <param name="isPercent">Whether <paramref name="value" /> is a normalized percentage.</param>
    public UiLength(float value, bool isPercent)
    {
        Value = value;
        IsPercent = isPercent;
        IsAuto = false;
    }

    private UiLength(float value, bool isPercent, bool isAuto)
    {
        Value = value;
        IsPercent = isPercent;
        IsAuto = isAuto;
    }

    /// <summary>Creates a fixed pixel length.</summary>
    public static UiLength Pixels(float px)
    {
        return new UiLength(px, false);
    }

    /// <summary>Creates a normalized parent-relative length, where 1 is 100%.</summary>
    public static UiLength Percent(float px)
    {
        return new UiLength(px, true);
    }

    /// <summary>Creates a length measured from the element's content.</summary>
    public static UiLength Auto()
    {
        return new UiLength(0, false, true);
    }

    /// <summary>Resolves this length against a parent dimension.</summary>
    /// <param name="parentSize">The parent width or height in pixels.</param>
    /// <returns>The resolved pixel length, or zero for an automatic length.</returns>
    public int Resolve(int parentSize)
    {
        if (IsAuto)
            return 0;

        if (IsPercent)
            return (int)(parentSize * Value); // Value as 0.0–1.0
        return (int)Value;
    }
}