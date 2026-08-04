namespace Dreambit.UI;

/// <summary>
///     A clipped single-selection list supporting arbitrary item elements,
///     pointer selection, and keyboard/controller navigation.
/// </summary>
public sealed class UiListBox : UiSelector
{
    /// <summary>Creates a vertical clipped list box.</summary>
    public UiListBox()
    {
        Orientation = StackOrientation.Vertical;
        ClipToBounds = true;
    }
}