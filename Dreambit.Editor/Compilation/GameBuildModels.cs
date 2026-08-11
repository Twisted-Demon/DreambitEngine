namespace Dreambit.Editor.Compilation;

internal enum GameBuildState
{
    Idle,
    Waiting,
    Building,
    Succeeded,
    Failed
}

internal enum GameBuildDiagnosticSeverity
{
    Information,
    Warning,
    Error
}

internal sealed record GameBuildDiagnostic(
    GameBuildDiagnosticSeverity Severity,
    string Code,
    string Message,
    string? File = null,
    int? Line = null,
    int? Column = null,
    string? Raw = null);

internal sealed record GameBuildStatus(
    GameBuildState State,
    string Message,
    DateTimeOffset? CompletedUtc = null,
    IReadOnlyList<GameBuildDiagnostic>? Diagnostics = null)
{
    public IReadOnlyList<GameBuildDiagnostic> CurrentDiagnostics => Diagnostics ?? [];
}

internal sealed record GameBuildResult(
    bool Succeeded,
    string? AssemblyPath,
    IReadOnlyList<string> Output,
    IReadOnlyList<GameBuildDiagnostic> Diagnostics,
    TimeSpan Duration);

internal enum GameCodeMessageSeverity
{
    Information,
    Warning,
    Error
}

internal sealed record GameCodeMessage(
    GameCodeMessageSeverity Severity,
    string Message,
    Exception? Exception = null);
