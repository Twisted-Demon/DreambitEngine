using System.Text.Json.Serialization;

namespace Dreambit.Editor.Persistence;

internal sealed class EditorGlobalState
{
    public const int CurrentVersion = 3;

    public int Version { get; set; } = CurrentVersion;
    public string? LastProjectPath { get; set; }
    public List<RecentProjectState> RecentProjects { get; set; } = [];
    public int WindowX { get; set; }
    public int WindowY { get; set; }
    public bool HasWindowPosition { get; set; }

    // Retained only so version 1 state can migrate without losing recents.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? RecentProjectPaths { get; set; }
}

internal sealed class RecentProjectState
{
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SdkVersion { get; set; } = string.Empty;
    public DateTimeOffset LastOpenedUtc { get; set; }
}
