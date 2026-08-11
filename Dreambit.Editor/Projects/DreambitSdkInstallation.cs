namespace Dreambit.Editor.Projects;

internal enum DreambitSdkSourceKind
{
    Bundled,
    DevelopmentSource
}

internal sealed record DreambitSdkInstallation(
    string Version,
    string RootDirectory,
    string PackagesDirectory,
    string TemplateHiveDirectory,
    string TemplatePackagePath,
    DreambitSdkSourceKind SourceKind);

internal sealed class DreambitSdkManifest
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public string Version { get; set; } = string.Empty;
    public string SourceKind { get; set; } = string.Empty;
    public DateTimeOffset InstalledAtUtc { get; set; }
    public List<string> Packages { get; set; } = [];
}
