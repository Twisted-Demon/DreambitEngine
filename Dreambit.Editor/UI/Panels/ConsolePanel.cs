using Dreambit.Editor.Logging;
using Dreambit.EditorApi;
using ImGuiNET;

namespace Dreambit.Editor.UI.Panels;

internal sealed class ConsolePanel : EditorPanel
{
    private readonly EditorLogService _logs;
    private string _search = string.Empty;
    private bool _showTrace = true;
    private bool _showInformation = true;
    private bool _showWarnings = true;
    private bool _showErrors = true;
    private bool _autoScroll = true;
    private long _lastSnapshotVersion = -1;

    public ConsolePanel(EditorLogService logs)
        : base(EditorPanelIds.Console, "Console")
    {
        _logs = logs;
    }

    protected override void DrawContents()
    {
        if (EditorGui.Button("Console.Clear", "Clear"))
            _logs.Clear();

        EditorGui.Inline();
        EditorGui.Checkbox("Console.ShowTrace", "Trace", ref _showTrace);
        EditorGui.Inline();
        EditorGui.Checkbox("Console.ShowInformation", "Info", ref _showInformation);
        EditorGui.Inline();
        EditorGui.Checkbox("Console.ShowWarnings", "Warnings", ref _showWarnings);
        EditorGui.Inline();
        EditorGui.Checkbox("Console.ShowErrors", "Errors", ref _showErrors);
        EditorGui.Inline();
        EditorGui.Checkbox("Console.AutoScroll", "Auto-scroll", ref _autoScroll);

        EditorGui.SearchInput("Console.Search", "Filter messages", ref _search);
        EditorGui.Separator();

        var snapshot = _logs.GetSnapshot();
        var receivedNewEntries = snapshot.Version != _lastSnapshotVersion;
        _lastSnapshotVersion = snapshot.Version;

        using var entries = EditorGui.Child("Console.Entries");
        if (!entries.IsVisible)
            return;

        foreach (var entry in snapshot.Entries)
        {
            if (!ShouldShow(entry) || !MatchesSearch(entry))
                continue;

            EditorGui.Message(
                GetSeverityKind(entry.Severity),
                $"{entry.Timestamp:HH:mm:ss}  {GetSeverityLabel(entry.Severity),-5}  [{entry.Category}] {entry.Message}");
            if (!string.IsNullOrWhiteSpace(entry.Details) && ImGui.IsItemHovered())
            {
                using var tooltip = EditorGui.Tooltip();
                using var wrap = EditorGui.TextWrap(MathF.Min(700f, ImGui.GetFontSize() * 60f));
                EditorGui.Text(entry.Details);
            }
        }

        if (_autoScroll && receivedNewEntries)
            ImGui.SetScrollHereY(1f);

    }

    private bool ShouldShow(EditorLogEntry entry) => entry.Severity switch
    {
        EditorLogSeverity.Trace => _showTrace,
        EditorLogSeverity.Information => _showInformation,
        EditorLogSeverity.Warning => _showWarnings,
        EditorLogSeverity.Error => _showErrors,
        _ => true
    };

    private bool MatchesSearch(EditorLogEntry entry) =>
        string.IsNullOrWhiteSpace(_search) ||
        entry.Category.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
        entry.Message.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
        (entry.Details?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false);

    private static string GetSeverityLabel(EditorLogSeverity severity) => severity switch
    {
        EditorLogSeverity.Trace => "TRACE",
        EditorLogSeverity.Information => "INFO",
        EditorLogSeverity.Warning => "WARN",
        EditorLogSeverity.Error => "ERROR",
        _ => "LOG"
    };

    private static EditorGuiMessageKind GetSeverityKind(EditorLogSeverity severity) => severity switch
    {
        EditorLogSeverity.Warning => EditorGuiMessageKind.Warning,
        EditorLogSeverity.Error => EditorGuiMessageKind.Error,
        _ => EditorGuiMessageKind.Information
    };
}
