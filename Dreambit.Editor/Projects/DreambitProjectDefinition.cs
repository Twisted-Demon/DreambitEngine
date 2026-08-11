namespace Dreambit.Editor.Projects;

internal sealed record DreambitProjectDefinition(
    string RootDirectory,
    string MetadataPath,
    DreambitProjectMetadata Metadata,
    string SolutionPath,
    string GameProjectPath,
    string ContentProjectPath,
    string ContentRootPath,
    string LauncherProjectPath);

internal sealed record ProjectValidationResult(
    string? NormalizedProjectRoot,
    DreambitProjectDefinition? Project,
    IReadOnlyList<ProjectDiagnostic> Diagnostics)
{
    public bool IsValid => Project is not null &&
                           !Diagnostics.Any(static diagnostic =>
                               diagnostic.Severity == ProjectDiagnosticSeverity.Error);

    public string ErrorSummary => string.Join(
        Environment.NewLine,
        Diagnostics
            .Where(static diagnostic => diagnostic.Severity == ProjectDiagnosticSeverity.Error)
            .Select(static diagnostic => diagnostic.Message));
}
