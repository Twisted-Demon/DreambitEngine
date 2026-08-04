namespace Dreambit.UI;

/// <summary>Specifies how a grid row or column receives space.</summary>
public enum UiGridUnitType
{
    /// <summary>Uses a fixed number of pixels.</summary>
    Pixel,

    /// <summary>Uses a percentage of the grid's available size.</summary>
    Percent,

    /// <summary>Uses the largest desired size of content in the track.</summary>
    Auto,

    /// <summary>Receives a weighted share of the remaining space.</summary>
    Star
}

/// <summary>Represents one row or column definition in a <see cref="UiGrid" />.</summary>
public readonly record struct UiGridLength
{
    /// <summary>Creates a grid length with the supplied value and unit.</summary>
    /// <param name="value">The pixel, normalized percentage, or star weight.</param>
    /// <param name="unitType">The sizing strategy.</param>
    public UiGridLength(float value, UiGridUnitType unitType)
    {
        Value = value;
        UnitType = unitType;
    }

    /// <summary>Gets the pixel, normalized percentage, or star weight.</summary>
    public float Value { get; }

    /// <summary>Gets the sizing strategy.</summary>
    public UiGridUnitType UnitType { get; }

    /// <summary>Creates a fixed-pixel track.</summary>
    public static UiGridLength Pixels(float value)
    {
        return new UiGridLength(value, UiGridUnitType.Pixel);
    }

    /// <summary>Creates a normalized parent-relative track.</summary>
    public static UiGridLength Percent(float value)
    {
        return new UiGridLength(value, UiGridUnitType.Percent);
    }

    /// <summary>Creates a content-sized track.</summary>
    public static UiGridLength Auto()
    {
        return new UiGridLength(0f, UiGridUnitType.Auto);
    }

    /// <summary>Creates a weighted remaining-space track.</summary>
    public static UiGridLength Star(float weight = 1f)
    {
        return new UiGridLength(weight, UiGridUnitType.Star);
    }
}