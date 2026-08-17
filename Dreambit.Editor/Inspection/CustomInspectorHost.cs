using Dreambit.Editor.Logging;
using Dreambit.EditorApi;

namespace Dreambit.Editor.Inspection;

internal sealed class CustomInspectorHost(
    CustomEditorRegistry customEditors,
    EditorLogService logs)
{
    public bool TryDraw(
        Type targetType,
        IReadOnlyList<object> targets,
        Action drawDefault,
        Action<string, Action> recordChange,
        string failureDescription)
    {
        if (!customEditors.TryGet(targetType, out var customEditor))
            return false;

        var context = new CustomInspectorContext(targets, drawDefault, recordChange, LogExtension);
        try
        {
            using var id = EditorGui.PushId($"CustomEditor.{targetType.FullName ?? targetType.Name}");
            customEditor!.Draw(context);
        }
        catch (Exception exception)
        {
            logs.Error("Game Editor", failureDescription, exception);
            EditorGui.Error(exception.Message);
            drawDefault();
        }

        return true;
    }

    private void LogExtension(EditorExtensionLogLevel level, string message, Exception? exception)
    {
        switch (level)
        {
            case EditorExtensionLogLevel.Information:
                logs.Info("Game Editor", message);
                break;
            case EditorExtensionLogLevel.Warning:
                logs.Warning("Game Editor", message);
                break;
            case EditorExtensionLogLevel.Error:
                logs.Error("Game Editor", message, exception);
                break;
        }
    }
}
