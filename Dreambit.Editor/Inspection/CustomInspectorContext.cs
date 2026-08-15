using Dreambit.EditorApi;

namespace Dreambit.Editor.Inspection;

internal sealed class CustomInspectorContext(
    IReadOnlyList<object> targets,
    Action drawDefault,
    Action<string, Action> recordChange,
    Action<EditorExtensionLogLevel, string, Exception?> log) : IEditorInspectorContext
{
    public object? ActiveTarget => targets.Count == 0 ? null : targets[0];
    public IReadOnlyList<object> Targets => targets;
    public void DrawDefaultInspector() => drawDefault();
    public void RecordChange(string name, Action mutation) => recordChange(name, mutation);
    public void Log(EditorExtensionLogLevel level, string message, Exception? exception = null) =>
        log(level, message, exception);
}
