using System.Numerics;
using Dreambit.Editor.Logging;
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
        if (ImGui.Button("Clear"))
            _logs.Clear();

        ImGui.SameLine();
        ImGui.Checkbox("Trace", ref _showTrace);
        ImGui.SameLine();
        ImGui.Checkbox("Info", ref _showInformation);
        ImGui.SameLine();
        ImGui.Checkbox("Warnings", ref _showWarnings);
        ImGui.SameLine();
        ImGui.Checkbox("Errors", ref _showErrors);
        ImGui.SameLine();
        ImGui.Checkbox("Auto-scroll", ref _autoScroll);

        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("##ConsoleSearch", "Filter messages", ref _search, 256);
        ImGui.Separator();

        var snapshot = _logs.GetSnapshot();
        var receivedNewEntries = snapshot.Version != _lastSnapshotVersion;
        _lastSnapshotVersion = snapshot.Version;

        if (!ImGui.BeginChild("##ConsoleEntries", Vector2.Zero, ImGuiChildFlags.None))
        {
            ImGui.EndChild();
            return;
        }

        foreach (var entry in snapshot.Entries)
        {
            if (!ShouldShow(entry) || !MatchesSearch(entry))
                continue;

            ImGui.PushStyleColor(ImGuiCol.Text, GetSeverityColor(entry.Severity));
            ImGui.TextUnformatted(
                $"{entry.Timestamp:HH:mm:ss}  {GetSeverityLabel(entry.Severity),-5}  [{entry.Category}] {entry.Message}");
            ImGui.PopStyleColor();

            if (!string.IsNullOrWhiteSpace(entry.Details) && ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(MathF.Min(700f, ImGui.GetFontSize() * 60f));
                ImGui.TextUnformatted(entry.Details);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        if (_autoScroll && receivedNewEntries)
            ImGui.SetScrollHereY(1f);

        ImGui.EndChild();
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

    private static Vector4 GetSeverityColor(EditorLogSeverity severity) => severity switch
    {
        EditorLogSeverity.Trace => new Vector4(0.56f, 0.59f, 0.65f, 1f),
        EditorLogSeverity.Information => new Vector4(0.80f, 0.82f, 0.86f, 1f),
        EditorLogSeverity.Warning => new Vector4(0.96f, 0.72f, 0.26f, 1f),
        EditorLogSeverity.Error => new Vector4(0.96f, 0.34f, 0.36f, 1f),
        _ => Vector4.One
    };
}
