using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>
///     An invisible, non-interactive layout element used to reserve empty space in
///     stacks, grids, wrap panels, and item controls.
/// </summary>
public sealed class UiSpacer : UiElement
{
    /// <summary>Creates an empty spacer.</summary>
    public UiSpacer()
    {
        IsHitTestVisible = false;
    }

    /// <summary>Creates a spacer with a fixed pixel size.</summary>
    /// <param name="width">The reserved width.</param>
    /// <param name="height">The reserved height.</param>
    public UiSpacer(int width, int height)
        : this()
    {
        Width = UiLength.Pixels(width);
        Height = UiLength.Pixels(height);
    }

    /// <inheritdoc />
    protected override Point MeasureContent(Point availableSize)
    {
        return Point.Zero;
    }
}