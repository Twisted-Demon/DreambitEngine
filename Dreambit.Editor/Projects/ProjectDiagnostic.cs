namespace Dreambit.Editor.Projects;

internal enum ProjectDiagnosticSeverity
{
    Warning,
    Error
}

internal sealed record ProjectDiagnostic(
    ProjectDiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Path = null);
