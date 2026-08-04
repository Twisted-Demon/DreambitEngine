namespace Dreambit.UI;

/// <summary>
///     Describes independent inset values for the four edges of a rectangle.
/// </summary>
/// <param name="Left">The left inset.</param>
/// <param name="Top">The top inset.</param>
/// <param name="Right">The right inset.</param>
/// <param name="Bottom">The bottom inset.</param>
public readonly record struct UiThickness(
    int Left,
    int Top,
    int Right,
    int Bottom)
{
    /// <summary>Gets the combined left and right inset.</summary>
    public int Horizontal => Left + Right;

    /// <summary>Gets the combined top and bottom inset.</summary>
    public int Vertical => Top + Bottom;

    /// <summary>Creates a thickness with the same inset on every edge.</summary>
    /// <param name="value">The inset assigned to all four edges.</param>
    /// <returns>A uniformly sized thickness.</returns>
    public static UiThickness Uniform(int value)
    {
        return new UiThickness(value, value, value, value);
    }
}