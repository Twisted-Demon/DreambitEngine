using System.Numerics;
using Dreambit.Editor.Assets;
using Dreambit.Editor.Compilation;
using Dreambit.Editor.Infrastructure;
using Dreambit.Editor.Graphics;
using Dreambit.Editor.Logging;
using Dreambit.Editor.Persistence;
using Dreambit.Editor.Projects;
using Dreambit.Editor.Scenes;
using Dreambit.Editor.UI;
using Dreambit.Editor.UI.Panels;
using Dreambit.LDtk;
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
    private readonly EditorDragDropService _dragDrop = new();
    private readonly EditorIconService _icons;
    private readonly CancellationTokenSource _projectCreationLifetime = new();

    private readonly DreambitProjectDefinition? _project;
    private string _openProjectPath = string.Empty;
    private string? _openProjectError;
    private bool _openProjectPopupRequested;
    private bool _showAbout;
    private bool _newScenePopupRequested;
    private bool _newLdtkScenePopupRequested;
    private bool _openScenePopupRequested;
    private bool _saveSceneAsPopupRequested;
    private bool _createFromBlueprintPopupRequested;
    private string _newSceneName = "Untitled";
    private string _scenePath = string.Empty;
    private string _blueprintSearch = string.Empty;
    private string _ldtkSearch = string.Empty;
    private LDtkImportOptions _newLdtkImportOptions = new();
    private string? _sceneOperationError;
    private bool _rebuildDockLayout;
    private bool _disposed;
    private Task<ProjectCreationResult>? _projectCreationTask;
    private ProjectCreationStatus _projectCreationStatus = new(false);

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
        _logs = new EditorLogService();
        _icons = new EditorIconService(Core.Instance.GraphicsDevice, imGuiRenderer);
        _projectManager = new DreambitProjectManager(
            paths,
            reportAssetDiagnostic: LogAssetDiagnostic,
            reportAssetBake: LogAssetBake,
            reportGameCode: LogGameCode,
            reportSceneError: LogSceneError);
        var sdkManager = new DreambitSdkManager(paths, _logs);
        _projectCreationService = new ProjectCreationService(sdkManager, _logs);

        string? projectError = null;
        if (!string.IsNullOrWhiteSpace(options.ProjectPath))
        {
            if (_projectManager.TryOpen(
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
            var blueprintEditing = new BlueprintEditingService(
                session.Assets,
                session.AssetEditing,
                session.GameCode.Assemblies,
                LogSceneError);
            var documentContext = new EditorDocumentContext(session.Scenes, blueprintEditing);
            var blueprintDockLayoutMissing =
                !_workspaceState.PanelVisibility.ContainsKey(EditorPanelIds.Blueprint);
            _panels.Register(new HierarchyPanel(
                documentContext,
                _dragDrop,
                session.Assets,
                _workspaceState,
                _icons));
            _panels.Register(new ScenePanel(
                session.Scenes,
                documentContext,
                session.Scenes.Selection,
                _workspaceState,
                new SceneViewportRenderer(Core.Instance.GraphicsDevice, imGuiRenderer),
                _dragDrop,
                session.Assets,
                _icons));
            var blueprintView = new BlueprintViewPanel(
                session.Assets,
                session.AssetEditing,
                blueprintEditing,
                documentContext,
                _workspaceState,
                new SceneViewportRenderer(Core.Instance.GraphicsDevice, imGuiRenderer),
                _icons);
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
            _panels.Register(new ProjectPanel(
                _project,
                _projectManager.CurrentSession!.Assets,
                _logs,
                _dragDrop,
                session.AssetEditing,
                session.Scenes,
                session.EditorTypes,
                _workspaceState,
                _icons,
                blueprintView.Open));
            _panels.Register(new ConsolePanel(_logs));
            _panels.Register(new BuildPanel(_projectManager.CurrentSession.GameCode, _icons));
            _rebuildDockLayout = !imGuiRenderer.HasSavedLayout || blueprintDockLayoutMissing;

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
    }

    public void Draw()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        UpdateProjectCreation();
        DrawDockHost();

        if (_project is null)
        {
            _projectLauncher.Draw(
                TryLaunchProject,
                BeginCreateProject,
                _projectCreationStatus);
        }
        else
        {
            _projectManager.CurrentSession!.Assets.Update();
            _projectManager.CurrentSession.AssetBaking.Update();
            _projectManager.CurrentSession.GameCode.Update();
            _projectManager.CurrentSession.Scenes.Update(
                _workspaceState.AutoSave,
                TimeSpan.FromSeconds(Math.Clamp(_workspaceState.AutoSaveDelaySeconds, 0.25, 60)));
            _projectManager.CurrentSession.AssetEditing.Update(
                _workspaceState.AutoSave,
                TimeSpan.FromSeconds(Math.Clamp(_workspaceState.AutoSaveDelaySeconds, 0.25, 60)));
            _panels.DrawPanels();
            CaptureWorkspaceSelection(_projectManager.CurrentSession);
        }

        DrawOpenProjectPopup();
        DrawAboutPopup();
        DrawScenePopups();
        HandleShortcuts();
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
        if (!ImGui.BeginMenuBar())
            return;

        if (ImGui.BeginMenu("File"))
        {
            ImGui.BeginDisabled(_project is null);
            if (ImGui.MenuItem("New Scene", "Ctrl+N"))
                _newScenePopupRequested = true;
            if (ImGui.MenuItem("New LDtk Scene..."))
            {
                _ldtkSearch = string.Empty;
                _newLdtkImportOptions = new LDtkImportOptions();
                _newLdtkScenePopupRequested = true;
            }
            if (ImGui.MenuItem("Open Scene...", "Ctrl+Shift+O"))
                _openScenePopupRequested = true;
            if (ImGui.MenuItem("Save", "Ctrl+S"))
                SaveCurrentDocument();
            if (ImGui.MenuItem("Save As...", "Ctrl+Shift+S"))
                RequestSaveSceneAs();
            ImGui.EndDisabled();
            ImGui.Separator();

            if (ImGui.MenuItem("Open Project...", "Ctrl+O"))
                _openProjectPopupRequested = true;

            if (_project is not null && ImGui.MenuItem("Close Project"))
                _requestExit();

            ImGui.Separator();
            if (ImGui.MenuItem("Exit"))
                _requestExit();
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Edit"))
        {
            var undo = _projectManager.CurrentSession?.AssetEditing.Current?.Undo ??
                       _projectManager.CurrentSession?.Scenes.Current?.Undo;
            ImGui.BeginDisabled(undo?.CanUndo != true);
            if (ImGui.MenuItem(undo?.UndoName is { } undoName ? $"Undo {undoName}" : "Undo", "Ctrl+Z"))
                undo!.Undo();
            ImGui.EndDisabled();
            ImGui.BeginDisabled(undo?.CanRedo != true);
            if (ImGui.MenuItem(undo?.RedoName is { } redoName ? $"Redo {redoName}" : "Redo", "Ctrl+Y"))
                undo!.Redo();
            ImGui.EndDisabled();
            ImGui.Separator();
            var autoSave = _workspaceState.AutoSave;
            if (ImGui.MenuItem("Auto Save", string.Empty, ref autoSave))
                _workspaceState.AutoSave = autoSave;
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Assets"))
        {
            ImGui.BeginDisabled(_project is null);
            if (ImGui.BeginMenu("Create"))
            {
                var projectPanel = _project is null
                    ? null
                    : (ProjectPanel)_panels.GetRequired(EditorPanelIds.Project);
                if (ImGui.MenuItem("Entity Blueprint"))
                    projectPanel!.RequestCreateAsset(typeof(EntityBlueprint));
                if (ImGui.BeginMenu("Dreambit Asset"))
                {
                    foreach (var type in _projectManager.CurrentSession!.EditorTypes.AssetTypes
                                 .Where(type => type != typeof(EntityBlueprint)))
                        if (ImGui.MenuItem(type.Name))
                            projectPanel!.RequestCreateAsset(type);
                    ImGui.EndMenu();
                }
                ImGui.EndMenu();
            }
            ImGui.EndDisabled();
            if (_project is not null && ImGui.MenuItem("Bake Changed"))
                _projectManager.CurrentSession!.AssetBaking.RequestBake(false);
            if (_project is not null && ImGui.MenuItem("Rebuild All"))
                _projectManager.CurrentSession!.AssetBaking.RequestBake(true);
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Entity"))
        {
            var document = _projectManager.CurrentSession?.Scenes.Current;
            ImGui.BeginDisabled(document is null);
            if (ImGui.MenuItem("Create Empty"))
                document!.CreateEmpty();
            if (ImGui.MenuItem("Create From Blueprint"))
            {
                _blueprintSearch = string.Empty;
                _createFromBlueprintPopupRequested = true;
            }
            ImGui.EndDisabled();
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Window"))
        {
            if (_project is null)
            {
                ImGui.TextDisabled("Open a project to show editor panels.");
            }
            else
            {
                _panels.DrawWindowMenu();
                ImGui.Separator();
                if (ImGui.MenuItem("Reset Layout"))
                    _rebuildDockLayout = true;
            }
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Build"))
        {
            if (_project is not null && ImGui.MenuItem("Build Game"))
                _projectManager.CurrentSession!.GameCode.RequestBuild(false, true);
            if (_project is not null && ImGui.MenuItem("Rebuild Game"))
                _projectManager.CurrentSession!.GameCode.RequestBuild(true, true);
            ImGui.Separator();
            if (_project is not null && ImGui.MenuItem("Bake Assets"))
                _projectManager.CurrentSession!.AssetBaking.RequestBake(false);
            if (_project is not null && ImGui.MenuItem("Rebuild All Assets"))
                _projectManager.CurrentSession!.AssetBaking.RequestBake(true);
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Help"))
        {
            if (ImGui.MenuItem("About Dreambit Editor"))
                _showAbout = true;
            ImGui.EndMenu();
        }

        ImGui.EndMenuBar();
    }

    private void DrawStatusBar()
    {
        ImGui.Separator();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3f);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 8f);
        var projectStatus = _project is null
            ? "No project open"
            : BuildProjectStatus();
        ImGui.TextDisabled(projectStatus);

        var status = _projectCreationStatus.IsRunning
            ? "Creating project..."
            : _projectManager.CurrentSession?.GameCode.IsRunning == true
                ? _projectManager.CurrentSession.GameCode.Status.Message
                : _projectManager.CurrentSession?.AssetBaking.Status.Message ?? "Ready";
        var statusWidth = ImGui.CalcTextSize(status).X;
        ImGui.SameLine(MathF.Max(0f, ImGui.GetWindowWidth() - statusWidth - 16f));
        ImGui.TextDisabled(status);
    }

    private void HandleShortcuts()
    {
        var io = ImGui.GetIO();
        if (io.KeyCtrl && io.KeyShift && ImGui.IsKeyPressed(ImGuiKey.O) && _project is not null)
            _openScenePopupRequested = true;
        else if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.O))
            _openProjectPopupRequested = true;
        if (_project is null || !io.KeyCtrl)
            return;
        if (ImGui.IsKeyPressed(ImGuiKey.N))
            _newScenePopupRequested = true;
        if (ImGui.IsKeyPressed(ImGuiKey.S))
        {
            if (io.KeyShift)
                RequestSaveSceneAs();
            else
                SaveCurrentDocument();
        }
        var undo = _projectManager.CurrentSession?.AssetEditing.Current?.Undo ??
                   _projectManager.CurrentSession?.Scenes.Current?.Undo;
        if (ImGui.IsKeyPressed(ImGuiKey.Z))
            undo?.Undo();
        if (ImGui.IsKeyPressed(ImGuiKey.Y))
            undo?.Redo();
    }

    private string BuildProjectStatus()
    {
        var project = $"{_project!.Metadata.Name}  |  SDK {_project.Metadata.Sdk.Version}";
        var assetDocument = _projectManager.CurrentSession?.AssetEditing.Current;
        if (assetDocument is not null)
            return $"{project}  |  {assetDocument.Asset.Name}{(assetDocument.IsDirty ? " *" : string.Empty)}";
        var document = _projectManager.CurrentSession?.Scenes.Current;
        return document is null
            ? project
            : $"{project}  |  {document.DisplayName}{(document.IsDirty ? " *" : string.Empty)}";
    }

    private void DrawScenePopups()
    {
        if (_newScenePopupRequested)
        {
            ImGui.OpenPopup("New Scene##Dreambit.Editor");
            _newScenePopupRequested = false;
        }
        if (_newLdtkScenePopupRequested)
        {
            ImGui.OpenPopup("New LDtk Scene##Dreambit.Editor");
            _newLdtkScenePopupRequested = false;
        }
        if (_openScenePopupRequested)
        {
            ImGui.OpenPopup("Open Scene##Dreambit.Editor");
            _openScenePopupRequested = false;
        }
        if (_saveSceneAsPopupRequested)
        {
            ImGui.OpenPopup("Save Scene As##Dreambit.Editor");
            _saveSceneAsPopupRequested = false;
        }

        DrawNewScenePopup();
        DrawNewLDtkScenePopup();
        DrawCreateFromBlueprintPopup();
        DrawScenePathPopup("Open Scene##Dreambit.Editor", "Open", TryOpenScene);
        DrawScenePathPopup("Save Scene As##Dreambit.Editor", "Save", TrySaveSceneAs);
    }

    private void DrawNewScenePopup()
    {
        if (!ImGui.BeginPopupModal("New Scene##Dreambit.Editor", ImGuiWindowFlags.AlwaysAutoResize))
            return;
        ImGui.SetNextItemWidth(360f);
        ImGui.InputText("Name", ref _newSceneName, 128);
        if (ImGui.Button("Create", new Vector2(90f, 0f)) && !string.IsNullOrWhiteSpace(_newSceneName))
        {
            _projectManager.CurrentSession!.Scenes.New(_newSceneName.Trim());
            _sceneOperationError = null;
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(90f, 0f)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void DrawNewLDtkScenePopup()
    {
        if (!ImGui.BeginPopupModal(
                "New LDtk Scene##Dreambit.Editor",
                ImGuiWindowFlags.AlwaysAutoResize))
            return;

        var session = _projectManager.CurrentSession!;
        ImGui.TextWrapped(
            "Choose an LDtk project/world. Its tilemap stays linked and entities placed " +
            "in Dreambit are preserved when LDtk is reimported.");
        var pixelsPerUnit = _newLdtkImportOptions.PixelsPerUnit;
        var baseDrawLayer = _newLdtkImportOptions.BaseDrawLayer;
        var drawLayerStep = _newLdtkImportOptions.DrawLayerStep;
        var worldDepthStride = _newLdtkImportOptions.WorldDepthDrawLayerStride;
        var renderBackgroundColor = _newLdtkImportOptions.RenderLevelBackgroundColor;
        var renderBackgroundImage = _newLdtkImportOptions.RenderLevelBackgroundImage;
        var includeInvisibleLayers = _newLdtkImportOptions.IncludeInvisibleLayers;
        ImGui.SetNextItemWidth(240f);
        ImGui.DragFloat("Pixels Per Unit##NewLDtk", ref pixelsPerUnit, 0.1f, 0.001f, 100000f);
        ImGui.SetNextItemWidth(240f);
        ImGui.DragInt("Base Draw Layer##NewLDtk", ref baseDrawLayer, 1f);
        ImGui.SetNextItemWidth(240f);
        ImGui.DragInt("Draw Layer Step##NewLDtk", ref drawLayerStep, 1f, 1, 100000);
        ImGui.SetNextItemWidth(240f);
        ImGui.DragInt("World Depth Stride##NewLDtk", ref worldDepthStride, 1f, 1, int.MaxValue);
        ImGui.Checkbox("Render Background Color##NewLDtk", ref renderBackgroundColor);
        ImGui.Checkbox("Render Background Image##NewLDtk", ref renderBackgroundImage);
        ImGui.Checkbox("Include Invisible Layers##NewLDtk", ref includeInvisibleLayers);
        _newLdtkImportOptions.PixelsPerUnit = pixelsPerUnit;
        _newLdtkImportOptions.BaseDrawLayer = baseDrawLayer;
        _newLdtkImportOptions.DrawLayerStep = drawLayerStep;
        _newLdtkImportOptions.WorldDepthDrawLayerStride = worldDepthStride;
        _newLdtkImportOptions.RenderLevelBackgroundColor = renderBackgroundColor;
        _newLdtkImportOptions.RenderLevelBackgroundImage = renderBackgroundImage;
        _newLdtkImportOptions.IncludeInvisibleLayers = includeInvisibleLayers;
        ImGui.Separator();
        ImGui.SetNextItemWidth(520f);
        ImGui.InputTextWithHint("##LDtkSearch", "Search LDtk projects", ref _ldtkSearch, 256);
        ImGui.BeginChild("##LDtkResults", new Vector2(520f, 300f), ImGuiChildFlags.Borders);
        var projects = session.Assets.GetSnapshot().Assets
            .Where(asset => asset.Kind == AssetKind.Ldtk &&
                            asset.RelativePath.EndsWith(".ldtk", StringComparison.OrdinalIgnoreCase) &&
                            (string.IsNullOrWhiteSpace(_ldtkSearch) ||
                             asset.RelativePath.Contains(_ldtkSearch, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        if (projects.Length == 0)
            ImGui.TextDisabled("No matching .ldtk projects were found under Assets.");
        foreach (var asset in projects)
        {
            try
            {
                foreach (var world in session.Scenes.GetLDtkWorldChoices(asset))
                {
                    var label = $"{asset.RelativePath}  /  {world.DisplayName}##{asset.Id.Value:N}-{world.WorldIid:N}";
                    if (!ImGui.Selectable(label))
                        continue;
                    session.Scenes.NewFromLDtk(
                        asset,
                        world.WorldIid,
                        world.DisplayName,
                        _newLdtkImportOptions);
                    session.AssetEditing.Clear();
                    _sceneOperationError = null;
                    ImGui.CloseCurrentPopup();
                }
            }
            catch (Exception exception)
            {
                ImGui.TextColored(
                    new Vector4(0.96f, 0.34f, 0.36f, 1f),
                    $"{asset.RelativePath}: {exception.Message}");
            }
        }
        ImGui.EndChild();
        if (!string.IsNullOrWhiteSpace(_sceneOperationError))
            ImGui.TextColored(new Vector4(0.96f, 0.34f, 0.36f, 1f), _sceneOperationError);
        if (ImGui.Button("Cancel", new Vector2(90f, 0f)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void DrawCreateFromBlueprintPopup()
    {
        if (_createFromBlueprintPopupRequested)
        {
            ImGui.OpenPopup("Create From Blueprint##Dreambit.Editor");
            _createFromBlueprintPopupRequested = false;
        }
        if (!ImGui.BeginPopupModal(
                "Create From Blueprint##Dreambit.Editor",
                ImGuiWindowFlags.AlwaysAutoResize))
            return;

        var session = _projectManager.CurrentSession!;
        var document = session.Scenes.Current;
        ImGui.SetNextItemWidth(460f);
        ImGui.InputTextWithHint("##BlueprintSearch", "Search Blueprints", ref _blueprintSearch, 256);
        ImGui.BeginChild("##BlueprintResults", new Vector2(460f, 300f), ImGuiChildFlags.Borders);
        var blueprints = session.Assets.GetSnapshot().Assets
            .Where(asset => asset.Kind == AssetKind.Blueprint &&
                            (string.IsNullOrWhiteSpace(_blueprintSearch) ||
                             asset.RelativePath.Contains(_blueprintSearch, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        if (blueprints.Length == 0)
            ImGui.TextDisabled("No matching Entity Blueprints.");
        foreach (var blueprint in blueprints)
        {
            if (!ImGui.Selectable(blueprint.RelativePath))
                continue;
            try
            {
                var path = Path.Combine(
                    session.Assets.ContentRoot,
                    blueprint.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                var source = DreambitJson.Deserialize<EntityBlueprint>(File.ReadAllText(path))
                             ?? throw new InvalidDataException("Blueprint file is empty.");
                source.AssetId = blueprint.Id;
                source.AssetName = blueprint.LogicalAssetName;
                document!.InstantiateBlueprint(source);
                _sceneOperationError = null;
                ImGui.CloseCurrentPopup();
            }
            catch (Exception exception)
            {
                _sceneOperationError = exception.Message;
            }
        }
        ImGui.EndChild();
        if (!string.IsNullOrWhiteSpace(_sceneOperationError))
            ImGui.TextColored(new Vector4(0.96f, 0.34f, 0.36f, 1f), _sceneOperationError);
        if (ImGui.Button("Cancel", new Vector2(90f, 0f)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void DrawScenePathPopup(string popupName, string action, Func<string, bool> execute)
    {
        if (!ImGui.BeginPopupModal(popupName, ImGuiWindowFlags.AlwaysAutoResize))
            return;
        ImGui.TextDisabled("Path is relative to the project's raw Assets folder.");
        ImGui.SetNextItemWidth(520f);
        var submit = ImGui.InputText(
            "##ScenePath",
            ref _scenePath,
            1024,
            ImGuiInputTextFlags.EnterReturnsTrue);
        if (!string.IsNullOrWhiteSpace(_sceneOperationError))
            ImGui.TextColored(new Vector4(0.96f, 0.34f, 0.36f, 1f), _sceneOperationError);
        if ((submit || ImGui.Button(action, new Vector2(90f, 0f))) && execute(_scenePath))
            ImGui.CloseCurrentPopup();
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(90f, 0f)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private bool TryOpenScene(string path)
    {
        try
        {
            _projectManager.CurrentSession!.Scenes.Open(path);
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
            session.AssetEditing.Select(asset);
    }

    private void CaptureWorkspaceSelection(DreambitProjectSession session)
    {
        if (session.AssetEditing.Selected is { } asset)
        {
            _workspaceState.LastSelectedAssetPath = asset.RelativePath;
            _workspaceState.LastSelectedAssetIsFolder = false;
            _workspaceState.LastSelectionKind = "asset";
            return;
        }
        if (session.Scenes.Selection.EntityIds.Count == 0)
            return;
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
        if (_projectManager.CurrentSession?.AssetEditing.Current is { } assetDocument)
        {
            try
            {
                _projectManager.CurrentSession.AssetEditing.Save();
                _logs.Info("Assets", $"Saved '{assetDocument.Asset.RelativePath}'.");
            }
            catch (Exception exception)
            {
                _logs.Error("Assets", "Could not save asset.", exception);
            }
            return;
        }
        SaveCurrentScene();
    }

    private void RequestSaveSceneAs()
    {
        var document = _projectManager.CurrentSession?.Scenes.Current;
        if (document is null)
            return;
        _scenePath = document.Path ?? $"Scenes/{document.DisplayName}.scene.json";
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
            ImGui.OpenPopup("Open Project##Dreambit.Editor.OpenProject");
            _openProjectPopupRequested = false;
        }

        var isOpen = true;
        if (!ImGui.BeginPopupModal(
                "Open Project##Dreambit.Editor.OpenProject",
                ref isOpen,
                ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.Text("Open a project in a new Dreambit Editor process.");
        ImGui.SetNextItemWidth(520f);
        ImGui.InputTextWithHint(
            "##OpenProjectPath",
            "Project directory",
            ref _openProjectPath,
            1_024);

        if (!string.IsNullOrWhiteSpace(_openProjectError))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.96f, 0.34f, 0.36f, 1f));
            ImGui.TextWrapped(_openProjectError);
            ImGui.PopStyleColor();
        }

        if (ImGui.Button("Open", new Vector2(90f, 0f)) &&
            TryLaunchProject(_openProjectPath))
        {
            _openProjectError = null;
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(90f, 0f)))
            ImGui.CloseCurrentPopup();

        ImGui.EndPopup();
    }

    private void DrawAboutPopup()
    {
        if (_showAbout)
        {
            ImGui.OpenPopup("About Dreambit Editor");
            _showAbout = false;
        }

        if (!ImGui.BeginPopupModal(
                "About Dreambit Editor",
                ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.Text("Dreambit Editor");
        ImGui.TextDisabled("MonoGame 3.8.5 / DesktopVK / ImGui.NET");
        ImGui.Spacing();
        ImGui.TextWrapped("A focused visual authoring environment for DreambitEngine.");
        ImGui.Spacing();
        if (ImGui.Button("Close", new Vector2(90f, 0f)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private bool TryLaunchProject(string projectPath)
    {
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
            _logs.Error("Project", "Project creation failed unexpectedly.", exception);
            result = new ProjectCreationResult(false, null, exception.Message);
        }

        _projectCreationTask = null;
        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.ProjectRoot))
        {
            _projectCreationStatus = new ProjectCreationStatus(false, result.Message, true);
            return;
        }

        if (!TryLaunchProject(result.ProjectRoot))
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

    private void TryPersistGlobalState()
    {
        if (!_stateStore.TrySaveGlobalState(_globalState, out var error))
            _logs.Warning("State", error ?? "Could not save the recent-project list.");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var panel in _panels.Panels)
            _workspaceState.PanelVisibility[panel.Id] = panel.IsOpen;

        if (_projectManager.CurrentSession?.Scenes.Current?.Path is { } scenePath)
            _workspaceState.LastScenePath = scenePath;

        _panels.Dispose();
        _icons.Dispose();
        _projectCreationLifetime.Cancel();
        _projectCreationLifetime.Dispose();
        _projectManager.Dispose();

        if (!_stateStore.TrySaveGlobalState(_globalState, out var globalError))
            Console.Error.WriteLine(globalError);
        if (!_stateStore.TrySaveWorkspaceState(_workspaceState, out var workspaceError))
            Console.Error.WriteLine(workspaceError);

        _disposed = true;
    }
}
