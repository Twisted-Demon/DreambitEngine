namespace Dreambit.UI;

/// <summary>
///     Identifies one of nine reference points used to anchor an element to its
///     parent or choose the point on the element treated as its origin.
/// </summary>
public enum UiAnchor
{
    /// <summary>The upper-left corner.</summary>
    TopLeft,

    /// <summary>The center of the top edge.</summary>
    TopCenter,

    /// <summary>The upper-right corner.</summary>
    TopRight,

    /// <summary>The center of the left edge.</summary>
    CenterLeft,

    /// <summary>The center point.</summary>
    Center,

    /// <summary>The center of the right edge.</summary>
    CenterRight,

    /// <summary>The lower-left corner.</summary>
    BottomLeft,

    /// <summary>The center of the bottom edge.</summary>
    BottomCenter,

    /// <summary>The lower-right corner.</summary>
    BottomRight
}