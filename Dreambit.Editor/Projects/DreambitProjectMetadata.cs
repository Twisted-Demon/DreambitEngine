namespace Dreambit.Editor.Projects;

internal sealed class DreambitProjectMetadata
{
    public const int CurrentSchemaVersion = 1;
    public const string RelativePath = ".dreambit/project.json";

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    public string GameProject { get; set; } = string.Empty;
    public string ContentProject { get; set; } = string.Empty;
    public string ContentRoot { get; set; } = string.Empty;
    public string LauncherProject { get; set; } = string.Empty;
    public string TargetRenderer { get; set; } = string.Empty;
    public DreambitSdkReference Sdk { get; set; } = new();
}

internal sealed class DreambitSdkReference
{
    public string Version { get; set; } = string.Empty;
}
