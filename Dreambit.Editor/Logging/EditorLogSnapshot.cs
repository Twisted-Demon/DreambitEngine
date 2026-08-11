namespace Dreambit.Editor.Logging;

internal readonly record struct EditorLogSnapshot(
    long Version,
    IReadOnlyList<EditorLogEntry> Entries);
