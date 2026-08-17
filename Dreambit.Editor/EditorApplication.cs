using System.Globalization;
using System.Numerics;
using Dreambit.Editor.Assets;
using Dreambit.Editor.Compilation;
using Dreambit.Editor.Infrastructure;
using Dreambit.Editor.Inspection;
using Dreambit.Editor.Graphics;
using Dreambit.Editor.Logging;
using Dreambit.Editor.Persistence;
using Dreambit.Editor.Projects;
using Dreambit.Editor.Scenes;
using Dreambit.Editor.UI;
using Dreambit.Editor.UI.Panels;
using Dreambit.EditorApi;
using Dreambit.LDtk;
using Dreambit.Tiled;
using ImGuiNET;

namespace Dreambit.Editor;

internal sealed class EditorApplication : IDisposable
{
    private const string DockspaceName = "Dreambit.Editor.DockSpace";
    private const string DockHostName = "Dreambit Editor##Dreambit.Editor.DockHost";
    private const float StatusBarHeight = 23f;

    private readonly Action _requestExit;
    private readonly DreambitProjectManager _projectManager;
    private readonly EditorStateStore _stateStore;
    private readonly EditorGlobalState _globalState;
    private readonly EditorWorkspaceState _workspaceState;
    private readonly EditorLogService _logs;
    private readonly EditorPanelRegistry _panels;
    private readonly ProjectLauncherView _projectLauncher;
    private readonly ProjectCreationService _projectCreationService;
    private readonly ProjectUpgradeService _projectUpgradeService;
    private readonly EditorDragDropService _dragDrop = new();
    private readonly EditorIconService _icons;
    private readonly CancellationTokenSource _projectCreationLifetime = new();
    private readonly CancellationTokenSource _projectUpgradeLifetime = new();

    private readonly DreambitProjectDefinition? _project;
    private string _openProjectPath = string.Empty;
    private string? _openProjectError;
    private bool _openProjectPopupRequested;
    private bool _showAbout;
    private bool _newScenePopupRequested;
    private bool _newLdtkScenePopupRequested;
    private bool _newTiledScenePopupRequested;
    private bool _openScenePopupRequested;
    private bool _saveSceneAsPopupRequested;
    private bool _createFromBlueprintPopupRequested;
    private string _newSceneName = "Untitled";
    private string _scenePath = string.Empty;
    private string _blueprintSearch = string.Empty;
    private string _ldtkSearch = string.Empty;
    private string _tiledSearch = string.Empty;
    private LDtkImportOptions _newLdtkImportOptions = new();
    private TiledImportOptions _newTiledImportOptions = new();
    private string? _sceneOperationError;
    private bool _rebuildDockLayout;
    private bool _exitAfterProjectLaunch;
    private bool _disposed;
    private Task<ProjectCreationResult>? _projectCreationTask;
    private ProjectCreationStatus _projectCreationStatus = new(false);
    private Task<ProjectUpgradeResult>? _projectUpgradeTask;
    private ProjectUpgradeCandidate? _projectUpgradeCandidate;
    private string? _projectUpgradeMessage;
    private bool _projectUpgradeIsError;
    private bool _projectUpgradePopupRequested;
    private bool _closeProjectUpgradePopup;

    public EditorApplication(
        EditorLaunchOptions options,
        EditorPaths paths,
        EditorStateStore stateStore,
        EditorGlobalState globalState,
        EditorWorkspaceState workspaceState,
        ImGuiRenderer imGuiRenderer,
        Action requestExit)
    {
        _requestExit = requestExit;
        _stateStore = stateStore;
        _globalState = globalState;
        _workspaceState = workspaceState;
        _logs = new EditorLogService(errorLogPath: paths.ErrorLogPath);
        _icons = new EditorIconService(Core.Instance.GraphicsDevice, imGuiRenderer);
        _projectManager = new DreambitProjectManager(
            paths,
            reportAssetDiagnostic: LogAssetDiagnostic,
            reportAssetBake: LogAssetBake,
            reportGameCode: LogGameCode,
            reportSceneError: LogSceneError);
        var sdkManager = new DreambitSdkManager(paths, _logs);
        _projectCreationService = new ProjectCreationService(sdkManager, _logs);
        _projectUpgradeService = new ProjectUpgradeService(sdkManager, _logs);

        string? projectError = null;
        if (!string.IsNullOrWhiteSpace(options.ProjectPath))
        {
            if (QueueProjectUpgradeIfNeeded(options.ProjectPath))
            {
                projectError = null;
            }
            else if (_projectManager.TryOpen(
                    options.ProjectPath,
                    out var validation,
                    out projectError))
            {
                _project = _projectManager.CurrentSession!.Project;
                RecordRecentProject(_project);
                TryPersistGlobalState();
            }
            else
            {
                LogProjectDiagnostics(validation.Diagnostics);
            }
        }

        _projectLauncher = new ProjectLauncherView(_globalState, projectError);
        _panels = new EditorPanelRegistry(_workspaceState);

        if (_project is not null)
        {
            var session = _projectManager.CurrentSession!;
            var blueprintEditing = session.Blueprints;
            var documentContext = session.Documents;
            var dockLayoutMissingNewTabs =
                !_workspaceState.PanelVisibility.ContainsKey(EditorPanelIds.Blueprint) ||
                !_workspaceState.PanelVisibility.ContainsKey(EditorPanelIds.LDtkImportOptions) ||
                !_workspaceState.PanelVisibility.ContainsKey(EditorPanelIds.TiledImportOptions) ||
                !_workspaceState.PanelVisibility.ContainsKey(EditorPanelIds.SceneSettings);
            _panels.Register(new HierarchyPanel(
                documentContext,
                _dragDrop,
                session.Assets,
                session.BlueprintSources,
                _workspaceState,
                _icons));
            _panels.Register(new ScenePanel(
                session.Scenes,
                documentContext,
                _workspaceState,
                new SceneViewportRenderer(
                    Core.Instance.GraphicsDevice,
                    imGuiRenderer,
                    LogSceneError),
                _dragDrop,
                session.Assets,
                session.BlueprintSources,
                _icons,
                LogSceneError));
            var blueprintView = new BlueprintViewPanel(
                session.Assets,
                session.AssetEditing,
                blueprintEditing,
                documentContext,
                _workspaceState,
                new SceneViewportRenderer(
                    Core.Instance.GraphicsDevice,
                    imGuiRenderer,
                    LogSceneError),
                _icons,
                LogSceneError);
            _panels.Register(blueprintView);
            _panels.Register(new InspectorPanel(
                documentContext,
                session.InspectorMetadata,
                session.EditorTypes,
                session.AssetEditing,
                session.Assets,
                _dragDrop,
                new AssetPreviewService(
                    Core.Instance.GraphicsDevice,
                    imGuiRenderer,
                    session.Assets.ContentRoot),
                session.CustomEditors,
                _logs));
            _panels.Register(new LDtkImportOptionsPanel(documentContext));
            _panels.Register(new TiledImportOptionsPanel(documentContext));
            _panels.Register(new SceneSettingsPanel(documentContext));
            _panels.Register(new ProjectPanel(
                _project,
                _projectManager.CurrentSession!.Assets,
                _logs,
                _dragDrop,
                session.AssetEditing,
                session.Scenes,
                documentContext,
                session.EditorTypes,
                _workspaceState,
                _icons,
                blueprintView.Open));
            _panels.Register(new ConsolePanel(_logs));
            _panels.Register(new BuildPanel(_projectManager.CurrentSession.GameCode, _icons));
            _rebuildDockLayout = !imGuiRenderer.HasSavedLayout || dockLayoutMissingNewTabs;

            if (!string.IsNullOrWhiteSpace(_workspaceState.LastScenePath) &&
                File.Exists(_workspaceState.LastScenePath))
            {
                TryOpenScene(_workspaceState.LastScenePath);
            }
            RestoreWorkspaceSelection(session);
        }

        foreach (var warning in _stateStore.LoadWarnings)
            _logs.Warning("State", warning);

        _logs.Info("Editor", "Dreambit Editor shell initialized.");
        if (_project is not null)
        {
            _logs.Info(
                "Project",
                $"Opened '{_project.Metadata.Name}' with Dreambit SDK {_project.Metadata.Sdk.Version}.");
        }

        LogSink.EntryLogged += OnEngineLogEntry;
    }

    public void Draw()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        UpdateProjectCreation();
        UpdateProjectUpgrade();
        DrawDockHost();

        if (_project is null)
        {
            _projectLauncher.Draw(
                TryLaunchProjectFromLauncher,
                BeginCreateProject,
                _projectCreationStatus);
        }
        else
        {
            _projectManager.CurrentSession!.Assets.Update();
            _projectManager.CurrentSession.AssetBaking.Update();
            _projectManager.CurrentSession.GameCode.Update();
            _projectManager.CurrentSession.Blueprints.Update();
            _projectManager.CurrentSession.Scenes.Update(
                _workspaceState.AutoSave,
                TimeSpan.FromSeconds(
                    Math.Clamp(_workspaceState.AutoSaveDelaySeconds, 0.25, 60)));
            _projectManager.CurrentSession.AssetEditing.Update(
                _workspaceState.AutoSave,
                TimeSpan.FromSeconds(
                    Math.Clamp(_workspaceState.AutoSaveDelaySeconds, 0.25, 60)),
                ImGui.IsAnyItemActive());
            _panels.DrawPanels();
            CaptureWorkspaceSelection(_projectManager.CurrentSession);
        }

        DrawOpenProjectPopup();
        DrawProjectUpgradePopup();
        DrawAboutPopup();
        DrawScenePopups();
        HandleShortcuts();

        if (_exitAfterProjectLaunch)
            _requestExit();
    }

    public void CaptureWindowBounds(int x, int y, int width, int height)
    {
        _workspaceState.WindowWidth = Math.Clamp(width, 800, 7680);
        _workspaceState.WindowHeight = Math.Clamp(height, 600, 4320);
        _workspaceState.WindowX = x;
        _workspaceState.WindowY = y;
        _workspaceState.HasWindowPosition = true;
        _globalState.WindowX = x;
        _globalState.WindowY = y;
        _globalState.HasWindowPosition = true;
    }

    private void DrawDockHost()
    {
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.WorkPos);
        ImGui.SetNextWindowSize(viewport.WorkSize);
        ImGui.SetNextWindowViewport(viewport.ID);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        var flags =
            ImGuiWindowFlags.MenuBar |
            ImGuiWindowFlags.NoDocking |
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoNavFocus;

        ImGui.Begin(DockHostName, flags);
        ImGui.PopStyleVar(3);

        DrawMainMenu();

        var dockspaceSize = ImGui.GetContentRegionAvail();
        dockspaceSize.Y = MathF.Max(1f, dockspaceSize.Y - StatusBarHeight);
        var dockspaceId = ImGui.GetID(DockspaceName);

        if (_rebuildDockLayout && _project is not null)
        {
            DefaultDockLayout.Rebuild(dockspaceId, dockspaceSize, _panels);
            _rebuildDockLayout = false;
        }

        ImGui.DockSpace(dockspaceId, dockspaceSize, ImGuiDockNodeFlags.None);
        DrawStatusBar();
        ImGui.End();
    }

    private void DrawMainMenu()
    {
        using var menuBar = EditorGui.MenuBar();
        if (!menuBar.IsOpen)
            return;

        using (var menu = EditorGui.Menu("File"))
        {
            if (menu.IsOpen)
            {
                using (EditorGui.Disabled(_project is null))
                {
                    if (EditorGui.MenuItem("New Scene", "Ctrl+N"))
                        _newScenePopupRequested = true;
                    if (EditorGui.MenuItem("New LDtk Scene..."))
                    {
                        _ldtkSearch = string.Empty;
                        _newLdtkImportOptions = new LDtkImportOptions();
                        _newLdtkScenePopupRequested = true;
                    }
                    if (EditorGui.MenuItem("New Tiled Scene..."))
                    {
                        _tiledSearch = string.Empty;
                        _newTiledImportOptions = new TiledImportOptions();
                        _newTiledScenePopupRequested = true;
                    }
                    if (EditorGui.MenuItem("Open Scene...", "Ctrl+Shift+O"))
                        _openScenePopupRequested = true;
                    if (EditorGui.MenuItem("Save", "Ctrl+S"))
                        SaveCurrentDocument();
                    if (EditorGui.MenuItem("Save As...", "Ctrl+Shift+S"))
                        RequestSaveSceneAs();
                }
                EditorGui.Separator();
                if (EditorGui.MenuItem("Open Project...", "Ctrl+O"))
                    _openProjectPopupRequested = true;
                if (_project is not null && EditorGui.MenuItem("Close Project"))
                    _requestExit();
                EditorGui.Separator();
                if (EditorGui.MenuItem("Exit"))
                    _requestExit();
            }
        }

        using (var menu = EditorGui.Menu("Edit"))
        {
            if (menu.IsOpen)
            {
                var undo = _projectManager.CurrentSession?.Documents.Undo;
                using (EditorGui.Disabled(undo?.CanUndo != true))
                    if (EditorGui.MenuItem(
                            undo?.UndoName is { } undoName ? $"Undo {undoName}" : "Undo",
                            "Ctrl+Z"))
                        TryChangeHistory(redo: false);
                using (EditorGui.Disabled(undo?.CanRedo != true))
                    if (EditorGui.MenuItem(
                            undo?.RedoName is { } redoName ? $"Redo {redoName}" : "Redo",
                            "Ctrl+Y"))
                        TryChangeHistory(redo: true);
                EditorGui.Separator();
                var autoSave = _workspaceState.AutoSave;
                if (EditorGui.MenuItem("Auto Save", ref autoSave))
                    _workspaceState.AutoSave = autoSave;
            }
        }

        using (var menu = EditorGui.Menu("Assets"))
        {
            if (menu.IsOpen)
            {
                using (EditorGui.Disabled(_project is null))
                {
                    using var create = EditorGui.Menu("Create");
                    if (create.IsOpen)
                    {
                        var projectPanel = _project is null
                            ? null
                            : (ProjectPanel)_panels.GetRequired(EditorPanelIds.Project);
                        if (EditorGui.MenuItem("Entity Blueprint"))
                            projectPanel!.RequestCreateAsset(typeof(EntityBlueprint));
                        using var assets = EditorGui.Menu("Dreambit Asset");
                        if (assets.IsOpen)
                            foreach (var type in _projectManager.CurrentSession!.EditorTypes.AssetTypes
                                         .Where(type => type != typeof(EntityBlueprint) &&
                                                        AssetTypeClassifier.CanCreateAsset(type)))
                                if (EditorGui.MenuItem(type.Name))
                                    projectPanel!.RequestCreateAsset(type);
                    }
                }
                if (_project is not null && EditorGui.MenuItem("Update Blobs"))
                    _projectManager.CurrentSession!.AssetBaking.RequestBake(false);
                if (_project is not null && EditorGui.MenuItem("Rebuild All Blobs"))
                    _projectManager.CurrentSession!.AssetBaking.RequestBake(true);
            }
        }

        using (var menu = EditorGui.Menu("Entity"))
        {
            if (menu.IsOpen)
            {
                var session = _projectManager.CurrentSession;
                var document = session is null || session.Documents.IsAsset
                    ? null
                    : session.Documents.Current;
                using (EditorGui.Disabled(document is null))
                {
                    if (EditorGui.MenuItem("Create Empty"))
                        TryEditActiveDocument(
                            document!,
                            () => document!.CreateEmpty(
                                "Entity",
                                session!.Documents.IsBlueprint ? session.Blueprints.Root : null),
                            "Could not create the entity.");
                    if (EditorGui.MenuItem("Create From Blueprint"))
                    {
                        _blueprintSearch = string.Empty;
                        _createFromBlueprintPopupRequested = true;
                    }
                }
            }
        }

        using (var menu = EditorGui.Menu("Window"))
        {
            if (menu.IsOpen)
            {
                if (_project is null)
                    EditorGui.MutedText("Open a project to show editor panels.");
                else
                {
                    _panels.DrawWindowMenu();
                    EditorGui.Separator();
                    if (EditorGui.MenuItem("Reset Layout"))
                        _rebuildDockLayout = true;
                }
            }
        }

        using (var menu = EditorGui.Menu("Build"))
        {
            if (menu.IsOpen)
            {
                if (_project is not null && EditorGui.MenuItem("Build Game"))
                    _projectManager.CurrentSession!.GameCode.RequestBuild(false, true);
                if (_project is not null && EditorGui.MenuItem("Rebuild Game"))
                    _projectManager.CurrentSession!.GameCode.RequestBuild(true, true);
                EditorGui.Separator();
                if (_project is not null && EditorGui.MenuItem("Bake Pak"))
                    _projectManager.CurrentSession!.AssetBaking.RequestPakBake();
            }
        }

        using (var menu = EditorGui.Menu("Help"))
        {
            if (menu.IsOpen && EditorGui.MenuItem("About Dreambit Editor"))
                _showAbout = true;
        }
    }

    private void DrawStatusBar()
    {
        EditorGui.Separator();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3f);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 8f);
        var projectStatus = _project is null
            ? "No project open"
            : BuildProjectStatus();
        EditorGui.MutedText(projectStatus);

        var status = _projectCreationStatus.IsRunning
            ? "Creating project..."
            : _projectManager.CurrentSession?.GameCode.IsRunning == true
                ? _projectManager.CurrentSession.GameCode.Status.Message
                : _projectManager.CurrentSession?.AssetBaking.Status.Message ?? "Ready";
        var statusWidth = ImGui.CalcTextSize(status).X;
        EditorGui.Inline(MathF.Max(0f, ImGui.GetWindowWidth() - statusWidth - 16f));
        EditorGui.MutedText(status);
    }

    private void HandleShortcuts()
    {
        var io = ImGui.GetIO();
        if (!io.KeyCtrl)
            return;

        // Save remains global so a focused text field can be committed without first
        // changing focus. Navigation and history shortcuts must not also act on the
        // document while ImGui owns keyboard text input.
        if (ImGui.IsKeyPressed(ImGuiKey.S))
        {
            if (_project is not null && io.KeyShift)
                RequestSaveSceneAs();
            else if (_project is not null)
                SaveCurrentDocument();
        }

        if (!ShouldHandleDocumentShortcut(io.WantTextInput))
            return;
        if (io.KeyShift && ImGui.IsKeyPressed(ImGuiKey.O) && _project is not null)
            _openScenePopupRequested = true;
        else if (ImGui.IsKeyPressed(ImGuiKey.O))
            _openProjectPopupRequested = true;
        if (_project is null)
            return;
        if (ImGui.IsKeyPressed(ImGuiKey.N))
            _newScenePopupRequested = true;
        if (ImGui.IsKeyPressed(ImGuiKey.Z))
            TryChangeHistory(redo: false);
        if (ImGui.IsKeyPressed(ImGuiKey.Y))
            TryChangeHistory(redo: true);
    }

    internal static bool ShouldHandleDocumentShortcut(bool wantTextInput) => !wantTextInput;

    private void TryChangeHistory(bool redo)
    {
        var undo = _projectManager.CurrentSession?.Documents.Undo;
        if (undo is null)
            return;

        try
        {
            if (redo)
                undo.Redo();
            else
                undo.Undo();
        }
        catch (Exception exception)
        {
            _logs.Error(
                "Undo",
                redo ? "Could not redo the editor change." : "Could not undo the editor change.",
                exception);
        }
    }

    private string BuildProjectStatus()
    {
        var project = $"{_project!.Metadata.Name}  |  SDK {_project.Metadata.Sdk.Version}";
        var session = _projectManager.CurrentSession!;
        var assetDocument = session.Documents.AssetDocument;
        if (assetDocument is not null)
            return $"{project}  |  {assetDocument.Asset.Name}{(assetDocument.IsDirty ? " *" : string.Empty)}";
        var document = session.Documents.ActiveKind == EditorDocumentKind.Scene
            ? session.Scenes.Current
            : null;
        return document is null
            ? project
            : $"{project}  |  {document.DisplayName}{(document.IsDirty ? " *" : string.Empty)}";
    }

    private void DrawScenePopups()
    {
        if (_newScenePopupRequested)
        {
            EditorGui.OpenPopup("New Scene##Dreambit.Editor");
            _newScenePopupRequested = false;
        }
        if (_newLdtkScenePopupRequested)
        {
            EditorGui.OpenPopup("New LDtk Scene##Dreambit.Editor");
            _newLdtkScenePopupRequested = false;
        }
        if (_newTiledScenePopupRequested)
        {
            EditorGui.OpenPopup("New Tiled Scene##Dreambit.Editor");
            _newTiledScenePopupRequested = false;
        }
        if (_openScenePopupRequested)
        {
            EditorGui.OpenPopup("Open Scene##Dreambit.Editor");
            _openScenePopupRequested = false;
        }
        if (_saveSceneAsPopupRequested)
        {
            EditorGui.OpenPopup("Save Scene As##Dreambit.Editor");
            _saveSceneAsPopupRequested = false;
        }

        DrawNewScenePopup();
        DrawNewLDtkScenePopup();
        DrawNewTiledScenePopup();
        DrawCreateFromBlueprintPopup();
        DrawScenePathPopup("Open Scene##Dreambit.Editor", "Open", TryOpenScene);
        DrawScenePathPopup("Save Scene As##Dreambit.Editor", "Save", TrySaveSceneAs);
    }

    private void DrawNewScenePopup()
    {
        using var popup = EditorGui.Modal("New Scene##Dreambit.Editor");
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
            try
            {
                _projectManager.CurrentSession!.Scenes.New(_newSceneName.Trim());
                _projectManager.CurrentSession.Documents.ActivateScene();
                _sceneOperationError = null;
                EditorGui.ClosePopup();
            }
            catch (Exception exception)
            {
                _sceneOperationError = exception.Message;
                _logs.Error("Scene", "Could not create the scene.", exception);
            }
        }
        if (!string.IsNullOrWhiteSpace(_sceneOperationError))
            EditorGui.Error(_sceneOperationError);
        EditorGui.Inline();
        if (EditorGui.Button("NewScene.Cancel", "Cancel", new Vector2(90f, 0f)))
            EditorGui.ClosePopup();
    }

    private void DrawNewLDtkScenePopup()
    {
        using var popup = EditorGui.Modal("New LDtk Scene##Dreambit.Editor");
        if (!popup.IsOpen)
            return;

        var session = _projectManager.CurrentSession!;
        EditorGui.WrappedText(
            "Choose an LDtk project/world. Its tilemap stays linked and entities placed " +
            "in Dreambit are preserved when LDtk is reimported.");
        ImportOptionsEditorGui.Draw(_newLdtkImportOptions, "NewLDtk");
        EditorGui.Separator();
        EditorGui.SearchInput("NewLDtk.Search", "Search LDtk projects", ref _ldtkSearch);
        using (var results = EditorGui.Child(
                   "##LDtkResults",
                   new Vector2(520f, 300f),
                   ImGuiChildFlags.Borders))
        {
            if (results.IsVisible)
            {
                var projects = session.Assets.GetSnapshot().Assets
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
                        foreach (var world in session.Scenes.GetLDtkWorldChoices(asset))
                        {
                            var choiceId = $"{asset.Id.Value:N}-{world.WorldIid:N}";
                            if (!EditorGui.Selectable(
                                    choiceId,
                                    $"{asset.RelativePath}  /  {world.DisplayName}"))
                                continue;
                            if (!session.AssetEditing.Clear())
                            {
                                _sceneOperationError =
                                    "Could not create the LDtk scene because the current asset could not be saved.";
                                continue;
                            }
                            session.Scenes.NewFromLDtk(
                                asset,
                                world.WorldIid,
                                world.DisplayName,
                                _newLdtkImportOptions);
                            session.Documents.ActivateScene();
                            _sceneOperationError = null;
                            EditorGui.ClosePopup();
                        }
                    }
                    catch (Exception exception)
                    {
                        EditorGui.Error($"{asset.RelativePath}: {exception.Message}");
                    }
                }
            }
        }
        if (!string.IsNullOrWhiteSpace(_sceneOperationError))
            EditorGui.Error(_sceneOperationError);
        if (EditorGui.Button("NewLDtk.Cancel", "Cancel", new Vector2(90f, 0f)))
            EditorGui.ClosePopup();
    }

    private void DrawNewTiledScenePopup()
    {
        using var popup = EditorGui.Modal("New Tiled Scene##Dreambit.Editor");
        if (!popup.IsOpen)
            return;

        var session = _projectManager.CurrentSession!;
        EditorGui.WrappedText(
            "Choose a Tiled TMX map. Its tile layers stay linked while entities placed in " +
            "Dreambit are preserved on reimport. Object and image layers are ignored.");
        ImportOptionsEditorGui.Draw(_newTiledImportOptions, "NewTiled");
        EditorGui.Separator();
        EditorGui.SearchInput("NewTiled.Search", "Search TMX maps", ref _tiledSearch);
        using (var results = EditorGui.Child(
                   "##TiledResults",
                   new Vector2(520f, 300f),
                   ImGuiChildFlags.Borders))
        {
            if (results.IsVisible)
            {
                var maps = session.Assets.GetSnapshot().Assets
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
                    try
                    {
                        if (!session.AssetEditing.Clear())
                        {
                            _sceneOperationError =
                                "Could not create the Tiled scene because the current asset could not be saved.";
                            continue;
                        }
                        session.Scenes.NewFromTiled(asset, _newTiledImportOptions);
                        session.Documents.ActivateScene();
                        _sceneOperationError = null;
                        EditorGui.ClosePopup();
                    }
                    catch (Exception exception)
                    {
                        _sceneOperationError =
                            $"Could not create a Tiled scene from '{asset.RelativePath}'. {exception.Message}";
                        _logs.Error(
                            "Tiled",
                            $"Could not create a Tiled scene from '{asset.RelativePath}'.",
                            exception);
                        EditorGui.Error($"{asset.RelativePath}: {exception.Message}");
                    }
                }
            }
        }
        if (!string.IsNullOrWhiteSpace(_sceneOperationError))
            EditorGui.Error(_sceneOperationError);
        if (EditorGui.Button("NewTiled.Cancel", "Cancel", new Vector2(90f, 0f)))
            EditorGui.ClosePopup();
    }

    private void DrawCreateFromBlueprintPopup()
    {
        if (_createFromBlueprintPopupRequested)
        {
            EditorGui.OpenPopup("Create From Blueprint##Dreambit.Editor");
            _createFromBlueprintPopupRequested = false;
        }
        using var popup = EditorGui.Modal("Create From Blueprint##Dreambit.Editor");
        if (!popup.IsOpen)
            return;

        var session = _projectManager.CurrentSession!;
        var document = session.Documents.IsAsset
            ? null
            : session.Documents.Current;
        EditorGui.SearchInput("CreateBlueprint.Search", "Search Blueprints", ref _blueprintSearch);
        using (var results = EditorGui.Child(
                   "##BlueprintResults",
                   new Vector2(460f, 300f),
                   ImGuiChildFlags.Borders))
        {
            if (results.IsVisible)
            {
                var blueprints = session.Assets.GetSnapshot().Assets
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
                        continue;
                    try
                    {
                        using var source = session.BlueprintSources.Load(blueprint);
                        document!.InstantiateBlueprint(
                            source,
                            parent: session.Documents.IsBlueprint ? session.Blueprints.Root : null);
                        _sceneOperationError = null;
                        EditorGui.ClosePopup();
                    }
                    catch (Exception exception)
                    {
                        _sceneOperationError = exception.Message;
                    }
                }
            }
        }
        if (!string.IsNullOrWhiteSpace(_sceneOperationError))
            EditorGui.Error(_sceneOperationError);
        if (EditorGui.Button("CreateBlueprint.Cancel", "Cancel", new Vector2(90f, 0f)))
            EditorGui.ClosePopup();
    }

    private void TryEditActiveDocument(
        SceneDocument document,
        Action mutation,
        string failureMessage)
    {
        try
        {
            mutation();
            _sceneOperationError = null;
        }
        catch (Exception exception)
        {
            _sceneOperationError = exception.Message;
            _logs.Error(document.Name, failureMessage, exception);
        }
    }

    private void DrawScenePathPopup(string popupName, string action, Func<string, bool> execute)
    {
        using var popup = EditorGui.Modal(popupName);
        if (!popup.IsOpen)
            return;
        EditorGui.MutedText("Path is relative to the project's raw Assets folder.");
        var submit = EditorGui.Property(
            $"{popupName}.Path",
            "Path",
            ref _scenePath,
            maxLength: 1024,
            commitOnEnter: true);
        if (!string.IsNullOrWhiteSpace(_sceneOperationError))
            EditorGui.Error(_sceneOperationError);
        if ((submit || EditorGui.Button(
                 $"{popupName}.Submit",
                 action,
                 new Vector2(90f, 0f),
                 primary: true)) && execute(_scenePath))
            EditorGui.ClosePopup();
        EditorGui.Inline();
        if (EditorGui.Button($"{popupName}.Cancel", "Cancel", new Vector2(90f, 0f)))
            EditorGui.ClosePopup();
    }

    private bool TryOpenScene(string path)
    {
        try
        {
            _projectManager.CurrentSession!.Scenes.Open(path);
            _projectManager.CurrentSession.Documents.ActivateScene();
            _workspaceState.LastScenePath = _projectManager.CurrentSession.Scenes.Current!.Path;
            if (string.Equals(_workspaceState.LastSelectionKind, "entity", StringComparison.OrdinalIgnoreCase))
            {
                _projectManager.CurrentSession.Scenes.Selection.Restore(
                    _workspaceState.LastSelectedEntityIds);
                _projectManager.CurrentSession.Scenes.Selection.RemoveMissing(
                    _projectManager.CurrentSession.Scenes.Current.Scene);
            }
            _sceneOperationError = null;
            _logs.Info("Scene", $"Opened '{_projectManager.CurrentSession.Scenes.Current.DisplayName}'.");
            return true;
        }
        catch (Exception exception)
        {
            _sceneOperationError = exception.Message;
            _logs.Error("Scene", "Could not open scene.", exception);
            return false;
        }
    }

    private void RestoreWorkspaceSelection(DreambitProjectSession session)
    {
        if (!string.Equals(_workspaceState.LastSelectionKind, "asset", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(_workspaceState.LastSelectedAssetPath))
            return;
        if (session.Assets.TryGetAsset(_workspaceState.LastSelectedAssetPath, out var asset))
        {
            if (session.AssetEditing.Select(asset))
                session.Documents.ActivateAsset();
        }
    }

    private void CaptureWorkspaceSelection(DreambitProjectSession session)
    {
        if (session.Documents.ActiveKind is EditorDocumentKind.Asset or EditorDocumentKind.Blueprint)
        {
            if (session.AssetEditing.Selected is { } asset)
            {
                _workspaceState.LastSelectedAssetPath = asset.RelativePath;
                _workspaceState.LastSelectedAssetIsFolder = false;
                _workspaceState.LastSelectionKind = "asset";
            }
            return;
        }

        // Scene focus owns the persisted selection even when that selection is empty.
        // Otherwise a retained asset document can remain the apparent startup focus
        // after the user deliberately returned to the scene and deselected everything.
        _workspaceState.LastSelectedEntityIds = session.Scenes.Selection.EntityIds.ToList();
        _workspaceState.LastSelectionKind = "entity";
    }

    private void SaveCurrentScene()
    {
        var document = _projectManager.CurrentSession?.Scenes.Current;
        if (document is null)
            return;
        if (document.Path is null)
        {
            RequestSaveSceneAs();
            return;
        }
        TrySaveSceneAs(document.Path);
    }

    private void SaveCurrentDocument()
    {
        var session = _projectManager.CurrentSession;
        if (session?.Documents.AssetDocument is { } assetDocument)
        {
            try
            {
                session.AssetEditing.Save();
                _logs.Info("Assets", $"Saved '{assetDocument.Asset.RelativePath}'.");
            }
            catch (Exception exception)
            {
                _logs.Error("Assets", "Could not save asset.", exception);
            }
            return;
        }
        if (session?.Documents.ActiveKind == EditorDocumentKind.Scene)
            SaveCurrentScene();
    }

    private void RequestSaveSceneAs()
    {
        var session = _projectManager.CurrentSession;
        if (session?.Documents.ActiveKind != EditorDocumentKind.Scene)
            return;
        var document = session.Scenes.Current;
        if (document is null)
            return;
        _scenePath = document.Path ??
                     $"Scenes/{document.DisplayName}{DreambitAssetFileExtensions.SceneBlueprint}";
        _saveSceneAsPopupRequested = true;
    }

    private bool TrySaveSceneAs(string path)
    {
        try
        {
            _projectManager.CurrentSession!.Scenes.Save(path);
            var document = _projectManager.CurrentSession.Scenes.Current!;
            _workspaceState.LastScenePath = document.Path;
            _sceneOperationError = null;
            _logs.Info("Scene", $"Saved '{document.DisplayName}'.");
            return true;
        }
        catch (Exception exception)
        {
            _sceneOperationError = exception.Message;
            _logs.Error("Scene", "Could not save scene.", exception);
            return false;
        }
    }

    private void DrawOpenProjectPopup()
    {
        if (_openProjectPopupRequested)
        {
            EditorGui.OpenPopup("Open Project##Dreambit.Editor.OpenProject");
            _openProjectPopupRequested = false;
        }

        var isOpen = true;
        using var popup = EditorGui.Modal(
            "Open Project##Dreambit.Editor.OpenProject",
            ref isOpen);
        if (!popup.IsOpen)
            return;

        EditorGui.WrappedText("Open a project in a new Dreambit Editor process.");
        EditorGui.Property(
            "OpenProject.Path",
            "Project",
            ref _openProjectPath,
            maxLength: 1_024,
            hint: "Project directory");

        if (!string.IsNullOrWhiteSpace(_openProjectError))
            EditorGui.Error(_openProjectError);

        if (EditorGui.Button(
                "OpenProject.Submit",
                "Open",
                new Vector2(90f, 0f),
                primary: true) &&
            TryLaunchProject(_openProjectPath))
        {
            _openProjectError = null;
            EditorGui.ClosePopup();
        }

        EditorGui.Inline();
        if (EditorGui.Button("OpenProject.Cancel", "Cancel", new Vector2(90f, 0f)))
            EditorGui.ClosePopup();
    }

    private void DrawAboutPopup()
    {
        if (_showAbout)
        {
            EditorGui.OpenPopup("About Dreambit Editor");
            _showAbout = false;
        }

        using var popup = EditorGui.Modal("About Dreambit Editor");
        if (!popup.IsOpen)
            return;

        EditorGui.Header("Dreambit Editor", "MonoGame 3.8.5 / DesktopVK / ImGui.NET");
        EditorGui.Space();
        EditorGui.WrappedText("A focused visual authoring environment for DreambitEngine.");
        EditorGui.Space();
        if (EditorGui.Button("About.Close", "Close", new Vector2(90f, 0f), primary: true))
            EditorGui.ClosePopup();
    }

    private bool TryLaunchProjectFromLauncher(string projectPath)
    {
        if (!TryLaunchProject(projectPath))
            return false;

        _exitAfterProjectLaunch = true;
        return true;
    }

    private bool TryLaunchProject(string projectPath)
    {
        if (QueueProjectUpgradeIfNeeded(projectPath))
            return false;

        var validation = _projectManager.Validate(projectPath);
        if (!validation.IsValid)
        {
            var validationError = validation.ErrorSummary;
            _projectLauncher.SetError(validationError ?? "The project path is invalid.");
            _openProjectError = validationError ?? "The project path is invalid.";
            LogProjectDiagnostics(validation.Diagnostics);
            return false;
        }

        var project = validation.Project!;
        var projectLockPath = EditorPaths.CreateProjectLockPath(project.RootDirectory);
        if (!ProjectInstanceLease.IsAvailable(projectLockPath))
        {
            var alreadyOpenError =
                $"The project '{project.Metadata.Name}' is already open in another Editor process.";
            _projectLauncher.SetError(alreadyOpenError);
            _openProjectError = alreadyOpenError;
            _logs.Warning("Project", alreadyOpenError);
            return false;
        }

        CaptureCurrentWindowPlacement();
        TryPersistGlobalState();
        if (!ProjectProcessLauncher.TryLaunch(project.RootDirectory, out var launchError))
        {
            _projectLauncher.SetError(launchError ?? "Could not open the project.");
            _openProjectError = launchError ?? "Could not open the project.";
            _logs.Error("Project", launchError ?? "Could not open the project.");
            return false;
        }

        RecordRecentProject(project);
        TryPersistGlobalState();
        _openProjectError = null;
        _logs.Info("Project", $"Launched project '{project.Metadata.Name}'.");
        return true;
    }

    private bool QueueProjectUpgradeIfNeeded(string projectPath)
    {
        if (!_projectUpgradeService.TryGetUpgradeCandidate(projectPath, out var candidate))
            return false;

        if (_projectUpgradeTask is not null)
            return true;

        _projectUpgradeCandidate = candidate;
        _projectUpgradeMessage = null;
        _projectUpgradeIsError = false;
        _projectUpgradePopupRequested = true;
        return true;
    }

    private void DrawProjectUpgradePopup()
    {
        if (_projectUpgradePopupRequested)
        {
            EditorGui.OpenPopup("Update Dreambit Project##Dreambit.Editor.ProjectUpdate");
            _projectUpgradePopupRequested = false;
        }

        using var popup = EditorGui.Modal(
            "Update Dreambit Project##Dreambit.Editor.ProjectUpdate");
        if (!popup.IsOpen)
            return;

        var candidate = _projectUpgradeCandidate;
        if (candidate is null)
        {
            EditorGui.ClosePopup();
            return;
        }

        EditorGui.WrappedText(
            $"'{candidate.ProjectName}' uses Dreambit SDK {candidate.CurrentVersion}, but this " +
            $"Editor provides {DreambitSdkConstants.CurrentVersion}.");
        EditorGui.Space();
        EditorGui.WrappedText(
            "Would you like Dreambit to update the project and restore its matching packages before opening it?");

        if (!string.IsNullOrWhiteSpace(_projectUpgradeMessage))
        {
            EditorGui.Space();
            EditorGui.Message(
                _projectUpgradeIsError
                    ? EditorGuiMessageKind.Error
                    : EditorGuiMessageKind.Success,
                _projectUpgradeMessage);
        }

        var updating = _projectUpgradeTask is not null;
        if (updating)
        {
            EditorGui.Space();
            EditorGui.MutedText("Updating project and restoring packages...");
        }
        else if (EditorGui.Button(
                     "ProjectUpdate.Confirm",
                     "Update and Open",
                     new Vector2(130f, 0f),
                     primary: true))
        {
            _projectUpgradeMessage = "Updating project and restoring packages...";
            _projectUpgradeIsError = false;
            _projectUpgradeTask = _projectUpgradeService.UpgradeAsync(
                candidate,
                _projectUpgradeLifetime.Token);
        }

        EditorGui.Inline();
        if (!updating && EditorGui.Button(
                "ProjectUpdate.Cancel",
                "Not Now",
                new Vector2(90f, 0f)))
        {
            _projectUpgradeCandidate = null;
            _projectUpgradeMessage = null;
            EditorGui.ClosePopup();
        }

        if (_closeProjectUpgradePopup)
        {
            _closeProjectUpgradePopup = false;
            EditorGui.ClosePopup();
        }
    }

    private void CaptureCurrentWindowPlacement()
    {
        var window = Core.Instance.Window;
        var bounds = window.ClientBounds;
        var position = window.Position;
        CaptureWindowBounds(position.X, position.Y, bounds.Width, bounds.Height);
        if (!_stateStore.TrySaveWorkspaceState(_workspaceState, out var error))
            _logs.Warning("State", error ?? "Could not save the current window placement.");
    }

    private bool BeginCreateProject(CreateProjectRequest request)
    {
        if (_projectCreationTask is not null)
            return false;

        if (!request.TryValidate(out _, out var error))
        {
            _projectCreationStatus = new ProjectCreationStatus(false, error, true);
            return false;
        }

        _projectCreationStatus = new ProjectCreationStatus(
            true,
            $"Installing Dreambit SDK {request.SdkVersion} and creating '{request.Name}'...");
        _projectCreationTask = _projectCreationService.CreateAsync(
            request,
            _projectCreationLifetime.Token);
        return true;
    }

    private void UpdateProjectCreation()
    {
        if (_projectCreationTask is not { IsCompleted: true } task)
            return;

        ProjectCreationResult result;
        try
        {
            result = task.GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            _logs.Error(
                "Project",
                "Project creation failed unexpectedly.",
                exception);

            result = new ProjectCreationResult(
                false,
                null,
                exception.Message);
        }

        _projectCreationTask = null;

        if (!result.Succeeded ||
            string.IsNullOrWhiteSpace(result.ProjectRoot))
        {
            _projectCreationStatus = new ProjectCreationStatus(
                false,
                result.Message,
                true);

            return;
        }

        if (!TryLaunchProjectFromLauncher(result.ProjectRoot))
        {
            _projectCreationStatus = new ProjectCreationStatus(
                false,
                $"{result.Message} The project was created, but the Editor process could not be launched.",
                true);

            return;
        }

        _projectCreationStatus = new ProjectCreationStatus(
            false,
            $"{result.Message} The project opened in a new Editor process.");
    }

    private void UpdateProjectUpgrade()
    {
        if (_projectUpgradeTask is not { IsCompleted: true } task)
            return;

        ProjectUpgradeResult result;
        try
        {
            result = task.GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            _logs.Error("Project", "Project update failed unexpectedly.", exception);
            result = new ProjectUpgradeResult(false, exception.Message);
        }

        _projectUpgradeTask = null;
        if (!result.Succeeded || _projectUpgradeCandidate is null)
        {
            _projectUpgradeMessage = result.Message;
            _projectUpgradeIsError = true;
            _projectLauncher.SetError(result.Message);
            return;
        }

        var projectRoot = _projectUpgradeCandidate.ProjectRoot;
        _logs.Info("Project", result.Message);
        _projectUpgradeCandidate = null;
        _projectUpgradeMessage = null;
        _projectUpgradeIsError = false;
        _closeProjectUpgradePopup = true;

        if (!TryLaunchProject(projectRoot))
        {
            var error = _openProjectError ?? "The project was updated, but could not be opened.";
            _projectUpgradeMessage = error;
            _projectUpgradeIsError = true;
            _projectLauncher.SetError(error);
            _closeProjectUpgradePopup = false;
            _projectUpgradeCandidate = new ProjectUpgradeCandidate(
                projectRoot,
                Path.GetFileName(projectRoot),
                DreambitSdkConstants.CurrentVersion);
            _projectUpgradePopupRequested = true;
            return;
        }

        _exitAfterProjectLaunch = true;
    }

    private void RecordRecentProject(DreambitProjectDefinition project)
    {
        var comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        _globalState.RecentProjects.RemoveAll(recent =>
            comparer.Equals(recent.Path, project.RootDirectory));
        _globalState.RecentProjects.Insert(0, new RecentProjectState
        {
            Path = project.RootDirectory,
            Name = project.Metadata.Name,
            SdkVersion = project.Metadata.Sdk.Version,
            LastOpenedUtc = DateTimeOffset.UtcNow
        });
        if (_globalState.RecentProjects.Count > 20)
            _globalState.RecentProjects.RemoveRange(
                20,
                _globalState.RecentProjects.Count - 20);
        _globalState.LastProjectPath = project.RootDirectory;
    }

    private void LogProjectDiagnostics(IEnumerable<ProjectDiagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            var message = diagnostic.Path is null
                ? $"{diagnostic.Code}: {diagnostic.Message}"
                : $"{diagnostic.Code}: {diagnostic.Message} ({diagnostic.Path})";
            if (diagnostic.Severity == ProjectDiagnosticSeverity.Error)
                _logs.Error("Project", message);
            else
                _logs.Warning("Project", message);
        }
    }

    private void LogAssetDiagnostic(AssetDatabaseDiagnostic diagnostic)
    {
        var message = diagnostic.Path is null
            ? diagnostic.Message
            : $"{diagnostic.Message} ({diagnostic.Path})";
        switch (diagnostic.Severity)
        {
            case AssetDatabaseDiagnosticSeverity.Information:
                _logs.Info("Assets", message);
                break;
            case AssetDatabaseDiagnosticSeverity.Warning:
                _logs.Warning("Assets", message);
                break;
            case AssetDatabaseDiagnosticSeverity.Error:
                _logs.Error("Assets", message, diagnostic.Exception);
                break;
        }
    }

    private void LogAssetBake(AssetBakeMessage message)
    {
        switch (message.Severity)
        {
            case AssetBakeMessageSeverity.Information:
                _logs.Info("Asset Baker", message.Message);
                break;
            case AssetBakeMessageSeverity.Warning:
                _logs.Warning("Asset Baker", message.Message);
                break;
            case AssetBakeMessageSeverity.Error:
                _logs.Error("Asset Baker", message.Message, message.Exception);
                break;
        }
    }

    private void LogGameCode(GameCodeMessage message)
    {
        switch (message.Severity)
        {
            case GameCodeMessageSeverity.Information:
                _logs.Info("Game Build", message.Message);
                break;
            case GameCodeMessageSeverity.Warning:
                _logs.Warning("Game Build", message.Message);
                break;
            case GameCodeMessageSeverity.Error:
                _logs.Error("Game Build", message.Message, message.Exception);
                break;
        }
    }

    private void LogSceneError(string message, Exception? exception) =>
        _logs.Error("Scene", message, exception);

    private void OnEngineLogEntry(LogEntry entry)
    {
        if (entry.Level != LogLevel.Error)
            return;

        var message = entry.Message;
        if (entry.Args is { Length: > 0 } args)
        {
            try
            {
                message = string.Format(CultureInfo.InvariantCulture, entry.Message, args);
            }
            catch (FormatException)
            {
                message = entry.Message + " | " + string.Join(", ", args);
            }
        }

        _logs.Error(
            string.IsNullOrWhiteSpace(entry.Prefix) ? "Engine" : $"Engine/{entry.Prefix}",
            message);
    }

    private void TryPersistGlobalState()
    {
        if (!_stateStore.TrySaveGlobalState(_globalState, out var error))
            _logs.Warning("State", error ?? "Could not save the recent-project list.");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        LogSink.EntryLogged -= OnEngineLogEntry;

        foreach (var panel in _panels.Panels)
            _workspaceState.PanelVisibility[panel.Id] = panel.IsOpen;

        if (_projectManager.CurrentSession?.Scenes.Current?.Path is { } scenePath)
            _workspaceState.LastScenePath = scenePath;

        RunShutdownStep(_panels.Dispose, "Could not dispose all editor panels.");
        RunShutdownStep(_icons.Dispose, "Could not dispose editor icons.");
        RunShutdownStep(_projectCreationLifetime.Cancel, "Could not cancel project creation.");
        RunShutdownStep(_projectCreationLifetime.Dispose, "Could not dispose project creation state.");
        RunShutdownStep(_projectUpgradeLifetime.Cancel, "Could not cancel project update.");
        RunShutdownStep(_projectUpgradeLifetime.Dispose, "Could not dispose project update state.");
        RunShutdownStep(_projectManager.Dispose, "Could not dispose the active project session.");

        if (!_stateStore.TrySaveGlobalState(_globalState, out var globalError))
            Console.Error.WriteLine(globalError);
        if (!_stateStore.TrySaveWorkspaceState(_workspaceState, out var workspaceError))
            Console.Error.WriteLine(workspaceError);
    }

    private void RunShutdownStep(Action action, string message)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            _logs.Error("Shutdown", message, exception);
            Console.Error.WriteLine($"{message} {exception}");
        }
    }
}
