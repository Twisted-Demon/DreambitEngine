using System.Numerics;
using Dreambit.Editor.Compilation;
using Dreambit.Editor.UI;
using Dreambit.EditorApi;

namespace Dreambit.Editor.UI.Panels;

internal sealed class BuildPanel : EditorPanel
{
    private readonly GameCodeService _gameCode;
    private readonly EditorIconService _icons;

    public BuildPanel(GameCodeService gameCode, EditorIconService icons)
        : base(EditorPanelIds.Build, "Build")
    {
        _gameCode = gameCode;
        _icons = icons;
    }

    protected override void DrawContents()
    {
        using (EditorGui.Disabled(_gameCode.IsRunning))
        {
            if (_icons.Button("BuildGame", "build", "Build game code"))
                _gameCode.RequestBuild(rebuild: false, immediate: true);
            EditorGui.Inline();
            if (_icons.Button("RebuildGame", "restart_alt", "Rebuild game code"))
                _gameCode.RequestBuild(rebuild: true, immediate: true);
        }

        var status = _gameCode.Status;
        EditorGui.Inline();
        EditorGui.MutedText(status.Message);
        var loaded = _gameCode.Assemblies.Current;
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
