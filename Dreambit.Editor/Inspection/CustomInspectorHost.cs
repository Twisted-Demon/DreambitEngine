using Dreambit.Editor.Logging;
using Dreambit.EditorApi;
using ImGuiNET;

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
            customEditor!.Draw(context);
        }
        catch (Exception exception)
        {
            logs.Error("Game Editor", failureDescription, exception);
            ImGui.TextColored(
                new System.Numerics.Vector4(0.96f, 0.34f, 0.36f, 1f),
                exception.Message);
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
