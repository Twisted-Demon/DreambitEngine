using System.Numerics;
using Dreambit.Editor.Compilation;
using ImGuiNET;

namespace Dreambit.Editor.UI.Panels;

internal sealed class BuildPanel : EditorPanel
{
    private readonly GameCodeService _gameCode;

    public BuildPanel(GameCodeService gameCode)
        : base(EditorPanelIds.Build, "Build")
    {
        _gameCode = gameCode;
    }

    protected override void DrawContents()
    {
        ImGui.BeginDisabled(_gameCode.IsRunning);
        if (ImGui.Button("Build", new Vector2(90f, 0f)))
            _gameCode.RequestBuild(rebuild: false, immediate: true);
        ImGui.SameLine();
        if (ImGui.Button("Rebuild", new Vector2(90f, 0f)))
            _gameCode.RequestBuild(rebuild: true, immediate: true);
        ImGui.EndDisabled();

        var status = _gameCode.Status;
        ImGui.SameLine();
        ImGui.TextDisabled(status.Message);
        var loaded = _gameCode.Assemblies.Current;
        if (loaded is not null)
        {
            ImGui.TextDisabled(
                $"Generation {loaded.Generation}  |  " +
                $"{loaded.Types.ComponentTypes.Count} components  |  " +
                $"{loaded.Types.AssetTypes.Count} custom assets");
        }

        ImGui.Separator();
        if (!ImGui.BeginChild("##BuildDiagnostics", Vector2.Zero, ImGuiChildFlags.None))
        {
            ImGui.EndChild();
            return;
        }

        if (status.CurrentDiagnostics.Count == 0)
        {
            ImGui.TextDisabled("No compiler diagnostics.");
        }
        else
        {
            foreach (var diagnostic in status.CurrentDiagnostics)
            {
                var color = diagnostic.Severity == GameBuildDiagnosticSeverity.Error
                    ? new Vector4(0.96f, 0.34f, 0.36f, 1f)
                    : new Vector4(0.96f, 0.72f, 0.26f, 1f);
                var location = diagnostic.File is null
                    ? string.Empty
                    : $"{diagnostic.File}({diagnostic.Line},{diagnostic.Column}): ";
                ImGui.PushStyleColor(ImGuiCol.Text, color);
                ImGui.TextWrapped($"{location}{diagnostic.Code}: {diagnostic.Message}");
                ImGui.PopStyleColor();
            }
        }

        ImGui.EndChild();
    }
}
