using System.Numerics;
using Dreambit.Editor.Commands;
using Dreambit.Editor.Compilation;
using Dreambit.Editor.UI;
using Dreambit.EditorApi;

namespace Dreambit.Editor.UI.Panels;

internal sealed class BuildPanel : EditorPanel
{
    private readonly EditorBuildCommands _commands;
    private readonly EditorIconService _icons;

    public BuildPanel(EditorBuildCommands commands, EditorIconService icons)
        : base(EditorPanelIds.Build, "Build")
    {
        _commands = commands;
        _icons = icons;
    }

    protected override void DrawContents()
    {
        using (EditorGui.Disabled(_commands.IsGameBuildRunning))
        {
            if (_icons.Button("BuildGame", "build", "Build game code"))
                _commands.BuildGame();
            EditorGui.Inline();
            if (_icons.Button("RebuildGame", "restart_alt", "Rebuild game code"))
                _commands.RebuildGame();
        }

        var status = _commands.GameBuildStatus;
        EditorGui.Inline();
        EditorGui.MutedText(status.Message);
        var loaded = _commands.Assemblies.Current;
        if (loaded is not null)
        {
            EditorGui.MutedText(
                $"Generation {loaded.Generation}  |  " +
                $"{loaded.Types.ComponentTypes.Count} components  |  " +
                $"{loaded.Types.AssetTypes.Count} custom assets");
        }

        EditorGui.Separator();
        using var diagnostics = EditorGui.Child("Build.Diagnostics", Vector2.Zero);
        if (!diagnostics.IsVisible)
            return;

        if (status.CurrentDiagnostics.Count == 0)
        {
            EditorGui.MutedText("No compiler diagnostics.");
        }
        else
        {
            foreach (var diagnostic in status.CurrentDiagnostics)
            {
                var location = diagnostic.File is null
                    ? string.Empty
                    : $"{diagnostic.File}({diagnostic.Line},{diagnostic.Column}): ";
                EditorGui.Message(
                    diagnostic.Severity == GameBuildDiagnosticSeverity.Error
                        ? EditorGuiMessageKind.Error
                        : EditorGuiMessageKind.Warning,
                    $"{location}{diagnostic.Code}: {diagnostic.Message}");
            }
        }

    }
}
