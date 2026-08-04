namespace Dreambit.UI;

/// <summary>
///     Provides clipboard text shared by UI controls. Platforms may synchronize
///     this value with their native clipboard at the application boundary.
/// </summary>
public static class UiClipboard
{
    /// <summary>Gets or sets the current clipboard text.</summary>
    public static string Text { get; set; } = string.Empty;
}