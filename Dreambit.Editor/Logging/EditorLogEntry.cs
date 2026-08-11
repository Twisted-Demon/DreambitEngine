namespace Dreambit.Editor.Logging;

internal sealed record EditorLogEntry(
    DateTimeOffset Timestamp,
    EditorLogSeverity Severity,
    string Category,
    string Message,
    string? Details);
