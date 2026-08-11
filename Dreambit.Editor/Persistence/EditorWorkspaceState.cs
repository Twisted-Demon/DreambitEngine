namespace Dreambit.Editor.Persistence;

internal sealed class EditorWorkspaceState
{
    public const int CurrentVersion = 1;
    public const int DefaultWindowWidth = 1440;
    public const int DefaultWindowHeight = 900;

    public int Version { get; set; } = CurrentVersion;
    public int WindowWidth { get; set; } = DefaultWindowWidth;
    public int WindowHeight { get; set; } = DefaultWindowHeight;
    public Dictionary<string, bool> PanelVisibility { get; set; } =
        new(StringComparer.Ordinal);
}
