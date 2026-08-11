namespace Dreambit.Editor.Persistence;

internal sealed class EditorWorkspaceState
{
    public const int CurrentVersion = 1;
    public const int DefaultWindowWidth = 1440;
    public const int DefaultWindowHeight = 900;

    public int Version { get; set; } = CurrentVersion;
    public int WindowWidth { get; set; } = DefaultWindowWidth;
    public int WindowHeight { get; set; } = DefaultWindowHeight;
    public bool AutoSave { get; set; }
    public double AutoSaveDelaySeconds { get; set; } = 2.0;
    public string? LastScenePath { get; set; }
    public bool ShowGrid { get; set; } = true;
    public float SceneCameraX { get; set; }
    public float SceneCameraY { get; set; }
    public float SceneCameraZoom { get; set; } = 1f;
    public int GizmoMode { get; set; } = 1;
    public bool SnapEnabled { get; set; }
    public float MoveSnap { get; set; } = 1f;
    public float RotateSnapDegrees { get; set; } = 15f;
    public float ScaleSnap { get; set; } = 0.1f;
    public string ProjectBrowserFolder { get; set; } = string.Empty;
    public string? LastSelectedAssetPath { get; set; }
    public bool LastSelectedAssetIsFolder { get; set; }
    public string? LastSelectionKind { get; set; }
    public List<Guid> LastSelectedEntityIds { get; set; } = [];
    public HashSet<Guid> HierarchyExpandedEntityIds { get; set; } = [];
    public Dictionary<string, bool> PanelVisibility { get; set; } =
        new(StringComparer.Ordinal);
}
