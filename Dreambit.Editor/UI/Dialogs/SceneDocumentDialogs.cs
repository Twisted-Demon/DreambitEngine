using System.Numerics;
using Dreambit.Editor.Assets;
using Dreambit.Editor.Commands;
using Dreambit.Editor.Inspection;
using Dreambit.Editor.Scenes;
using Dreambit.EditorApi;
using Dreambit.LDtk;
using Dreambit.Tiled;
using ImGuiNET;

namespace Dreambit.Editor.UI.Dialogs;

/// <summary>
/// Owns transient state and rendering for scene/document workflows. The associated document
/// operations live in <see cref="EditorDocumentCommands"/>, allowing menus and shortcuts to
/// invoke the same semantic commands without duplicating orchestration.
/// </summary>
internal sealed class SceneDocumentDialogs
{
    private const string NewScenePopup = "New Scene##Dreambit.Editor";
    private const string NewLDtkScenePopup = "New LDtk Scene##Dreambit.Editor";
    private const string NewTiledScenePopup = "New Tiled Scene##Dreambit.Editor";
    private const string OpenScenePopup = "Open Scene##Dreambit.Editor";
    private const string SaveSceneAsPopup = "Save Scene As##Dreambit.Editor";
    private const string CreateFromBlueprintPopup = "Create From Blueprint##Dreambit.Editor";

    private readonly EditorDocumentCommands _commands;
    private readonly AssetDatabase _assets;
    private readonly SceneDocumentService _scenes;

    private bool _newSceneRequested;
    private string _newSceneName = "Untitled";
    private string? _newSceneError;

    private bool _newLdtkSceneRequested;
    private string _ldtkSearch = string.Empty;
    private LDtkImportOptions _ldtkImportOptions = new();
    private string? _ldtkSceneError;

    private bool _newTiledSceneRequested;
    private string _tiledSearch = string.Empty;
    private TiledImportOptions _tiledImportOptions = new();
    private string? _tiledSceneError;

    private bool _openSceneRequested;
    private string _openScenePath = string.Empty;
    private string? _openSceneError;

    private bool _saveSceneAsRequested;
    private string _saveScenePath = string.Empty;
    private string? _saveSceneError;

    private bool _createFromBlueprintRequested;
    private string _blueprintSearch = string.Empty;
    private string? _blueprintError;

    public SceneDocumentDialogs(
        EditorDocumentCommands commands,
        AssetDatabase assets,
        SceneDocumentService scenes)
    {
        _commands = commands;
        _assets = assets;
        _scenes = scenes;
    }

    public void RequestNewScene() => _newSceneRequested = true;

    public void RequestNewLDtkScene()
    {
        _ldtkSearch = string.Empty;
        _ldtkImportOptions = new LDtkImportOptions();
        _ldtkSceneError = null;
        _newLdtkSceneRequested = true;
    }

    public void RequestNewTiledScene()
    {
        _tiledSearch = string.Empty;
        _tiledImportOptions = new TiledImportOptions();
        _tiledSceneError = null;
        _newTiledSceneRequested = true;
    }

    public void RequestOpenScene() => _openSceneRequested = true;

    public void RequestSaveSceneAs()
    {
        var initialPath = _commands.GetSaveSceneAsInitialPath();
        if (initialPath is null)
            return;

        _saveScenePath = initialPath;
        _saveSceneError = null;
        _saveSceneAsRequested = true;
    }

    public void RequestCreateFromBlueprint()
    {
        _blueprintSearch = string.Empty;
        _blueprintError = null;
        _createFromBlueprintRequested = true;
    }

    public void Draw()
    {
        OpenRequestedPopups();
        DrawNewScenePopup();
        DrawNewLDtkScenePopup();
        DrawNewTiledScenePopup();
        DrawCreateFromBlueprintPopup();
        DrawScenePathPopup(
            OpenScenePopup,
            "Open",
            ref _openScenePath,
            ref _openSceneError,
            _commands.OpenScene);
        DrawScenePathPopup(
            SaveSceneAsPopup,
            "Save",
            ref _saveScenePath,
            ref _saveSceneError,
            _commands.SaveScene);
    }

    private void OpenRequestedPopups()
    {
        if (_newSceneRequested)
        {
            EditorGui.OpenPopup(NewScenePopup);
            _newSceneRequested = false;
        }
        if (_newLdtkSceneRequested)
        {
            EditorGui.OpenPopup(NewLDtkScenePopup);
            _newLdtkSceneRequested = false;
        }
        if (_newTiledSceneRequested)
        {
            EditorGui.OpenPopup(NewTiledScenePopup);
            _newTiledSceneRequested = false;
        }
        if (_openSceneRequested)
        {
            EditorGui.OpenPopup(OpenScenePopup);
            _openSceneRequested = false;
        }
        if (_saveSceneAsRequested)
        {
            EditorGui.OpenPopup(SaveSceneAsPopup);
            _saveSceneAsRequested = false;
        }
        if (_createFromBlueprintRequested)
        {
            EditorGui.OpenPopup(CreateFromBlueprintPopup);
            _createFromBlueprintRequested = false;
        }
    }

    private void DrawNewScenePopup()
    {
        using var popup = EditorGui.Modal(NewScenePopup);
        if (!popup.IsOpen)
            return;

        EditorGui.Property("NewScene.Name", "Name", ref _newSceneName, maxLength: 128);
        if (EditorGui.Button(
                "NewScene.Create",
                "Create",
                new Vector2(90f, 0f),
                primary: true,
                enabled: !string.IsNullOrWhiteSpace(_newSceneName)))
        {
            var result = _commands.CreateScene(_newSceneName);
            if (result.Succeeded)
            {
                _newSceneError = null;
                EditorGui.ClosePopup();
            }
            else
            {
                _newSceneError = result.Error;
            }
        }

        DrawError(_newSceneError);
        EditorGui.Inline();
        if (EditorGui.Button("NewScene.Cancel", "Cancel", new Vector2(90f, 0f)))
            EditorGui.ClosePopup();
    }

    private void DrawNewLDtkScenePopup()
    {
        using var popup = EditorGui.Modal(NewLDtkScenePopup);
        if (!popup.IsOpen)
            return;

        EditorGui.WrappedText(
            "Choose an LDtk project/world. Its tilemap stays linked and entities placed " +
            "in Dreambit are preserved when LDtk is reimported.");
        ImportOptionsEditorGui.Draw(_ldtkImportOptions, "NewLDtk");
        EditorGui.Separator();
        EditorGui.SearchInput("NewLDtk.Search", "Search LDtk projects", ref _ldtkSearch);
        using (var results = EditorGui.Child(
                   "##LDtkResults",
                   new Vector2(520f, 300f),
                   ImGuiChildFlags.Borders))
        {
            if (results.IsVisible)
            {
                var projects = _assets.GetSnapshot().Assets
                    .Where(asset => asset.Kind == AssetKind.Ldtk &&
                                    asset.RelativePath.EndsWith(".ldtk", StringComparison.OrdinalIgnoreCase) &&
                                    (string.IsNullOrWhiteSpace(_ldtkSearch) ||
                                     asset.RelativePath.Contains(
                                         _ldtkSearch,
                                         StringComparison.OrdinalIgnoreCase)))
                    .ToArray();
                if (projects.Length == 0)
                    EditorGui.MutedText("No matching .ldtk projects were found under Assets.");
                foreach (var asset in projects)
                {
                    try
                    {
                        foreach (var world in _scenes.GetLDtkWorldChoices(asset))
                        {
                            var choiceId = $"{asset.Id.Value:N}-{world.WorldIid:N}";
                            if (!EditorGui.Selectable(
                                    choiceId,
                                    $"{asset.RelativePath}  /  {world.DisplayName}"))
                            {
                                continue;
                            }

                            var result = _commands.CreateSceneFromLDtk(
                                asset,
                                world,
                                _ldtkImportOptions);
                            if (result.Succeeded)
                            {
                                _ldtkSceneError = null;
                                EditorGui.ClosePopup();
                                return;
                            }

                            _ldtkSceneError = result.Error;
                        }
                    }
                    catch (Exception exception)
                    {
                        EditorGui.Error($"{asset.RelativePath}: {exception.Message}");
                    }
                }
            }
        }

        DrawError(_ldtkSceneError);
        if (EditorGui.Button("NewLDtk.Cancel", "Cancel", new Vector2(90f, 0f)))
            EditorGui.ClosePopup();
    }

    private void DrawNewTiledScenePopup()
    {
        using var popup = EditorGui.Modal(NewTiledScenePopup);
        if (!popup.IsOpen)
            return;

        EditorGui.WrappedText(
            "Choose a Tiled TMX map. Its tile layers stay linked while entities placed in " +
            "Dreambit are preserved on reimport. Object and image layers are ignored.");
        ImportOptionsEditorGui.Draw(_tiledImportOptions, "NewTiled");
        EditorGui.Separator();
        EditorGui.SearchInput("NewTiled.Search", "Search TMX maps", ref _tiledSearch);
        using (var results = EditorGui.Child(
                   "##TiledResults",
                   new Vector2(520f, 300f),
                   ImGuiChildFlags.Borders))
        {
            if (results.IsVisible)
            {
                var maps = _assets.GetSnapshot().Assets
                    .Where(asset => asset.Kind == AssetKind.TiledMap &&
                                    asset.RelativePath.EndsWith(".tmx", StringComparison.OrdinalIgnoreCase) &&
                                    (string.IsNullOrWhiteSpace(_tiledSearch) ||
                                     asset.RelativePath.Contains(
                                         _tiledSearch,
                                         StringComparison.OrdinalIgnoreCase)))
                    .ToArray();
                if (maps.Length == 0)
                    EditorGui.MutedText("No matching .tmx maps were found under Assets.");
                foreach (var asset in maps)
                {
                    if (!EditorGui.Selectable(asset.Id.Value.ToString("N"), asset.RelativePath))
                        continue;

                    var result = _commands.CreateSceneFromTiled(asset, _tiledImportOptions);
                    if (result.Succeeded)
                    {
                        _tiledSceneError = null;
                        EditorGui.ClosePopup();
                        return;
                    }

                    _tiledSceneError = result.Error;
                }
            }
        }

        DrawError(_tiledSceneError);
        if (EditorGui.Button("NewTiled.Cancel", "Cancel", new Vector2(90f, 0f)))
            EditorGui.ClosePopup();
    }

    private void DrawCreateFromBlueprintPopup()
    {
        using var popup = EditorGui.Modal(CreateFromBlueprintPopup);
        if (!popup.IsOpen)
            return;

        EditorGui.SearchInput(
            "CreateBlueprint.Search",
            "Search Blueprints",
            ref _blueprintSearch);
        using (var results = EditorGui.Child(
                   "##BlueprintResults",
                   new Vector2(460f, 300f),
                   ImGuiChildFlags.Borders))
        {
            if (results.IsVisible)
            {
                var blueprints = _assets.GetSnapshot().Assets
                    .Where(asset => asset.Kind == AssetKind.Blueprint &&
                                    (string.IsNullOrWhiteSpace(_blueprintSearch) ||
                                     asset.RelativePath.Contains(
                                         _blueprintSearch,
                                         StringComparison.OrdinalIgnoreCase)))
                    .ToArray();
                if (blueprints.Length == 0)
                    EditorGui.MutedText("No matching Entity Blueprints.");
                foreach (var blueprint in blueprints)
                {
                    if (!EditorGui.Selectable(
                            blueprint.Id.Value.ToString("N"),
                            blueprint.RelativePath))
                    {
                        continue;
                    }

                    var result = _commands.CreateEntityFromBlueprint(blueprint);
                    if (result.Succeeded)
                    {
                        _blueprintError = null;
                        EditorGui.ClosePopup();
                        return;
                    }

                    _blueprintError = result.Error;
                }
            }
        }

        DrawError(_blueprintError);
        if (EditorGui.Button("CreateBlueprint.Cancel", "Cancel", new Vector2(90f, 0f)))
            EditorGui.ClosePopup();
    }

    private static void DrawScenePathPopup(
        string popupName,
        string action,
        ref string path,
        ref string? error,
        Func<string, EditorCommandResult> execute)
    {
        using var popup = EditorGui.Modal(popupName);
        if (!popup.IsOpen)
            return;

        EditorGui.MutedText("Path is relative to the project's raw Assets folder.");
        var submit = EditorGui.Property(
            $"{popupName}.Path",
            "Path",
            ref path,
            maxLength: 1024,
            commitOnEnter: true);
        DrawError(error);
        if (submit || EditorGui.Button(
                $"{popupName}.Submit",
                action,
                new Vector2(90f, 0f),
                primary: true))
        {
            var result = execute(path);
            if (result.Succeeded)
            {
                error = null;
                EditorGui.ClosePopup();
            }
            else
            {
                error = result.Error;
            }
        }

        EditorGui.Inline();
        if (EditorGui.Button($"{popupName}.Cancel", "Cancel", new Vector2(90f, 0f)))
            EditorGui.ClosePopup();
    }

    private static void DrawError(string? error)
    {
        if (!string.IsNullOrWhiteSpace(error))
            EditorGui.Error(error);
    }
}
