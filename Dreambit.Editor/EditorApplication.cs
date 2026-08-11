using System.Numerics;
using Dreambit.Editor.Assets;
using Dreambit.Editor.Infrastructure;
using Dreambit.Editor.Logging;
using Dreambit.Editor.Persistence;
using Dreambit.Editor.Projects;
using Dreambit.Editor.UI;
using Dreambit.Editor.UI.Panels;
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
    private readonly CancellationTokenSource _projectCreationLifetime = new();

    private readonly DreambitProjectDefinition? _project;
    private string _openProjectPath = string.Empty;
    private string? _openProjectError;
    private bool _openProjectPopupRequested;
    private bool _showAbout;
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
        bool hasSavedLayout,
        Action requestExit)
    {
        _requestExit = requestExit;
        _stateStore = stateStore;
        _globalState = globalState;
        _workspaceState = workspaceState;
        _logs = new EditorLogService();
        _projectManager = new DreambitProjectManager(
            paths,
            reportAssetDiagnostic: LogAssetDiagnostic);
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
            _panels.Register(new HierarchyPanel());
            _panels.Register(new ScenePanel());
            _panels.Register(new InspectorPanel());
            _panels.Register(new ProjectPanel(
                _project,
                _projectManager.CurrentSession!.Assets,
                _logs,
                _dragDrop));
            _panels.Register(new ConsolePanel(_logs));
            _rebuildDockLayout = !hasSavedLayout;
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
            _panels.DrawPanels();
        }

        DrawOpenProjectPopup();
        DrawAboutPopup();
        HandleShortcuts();
    }

    public void CaptureWindowSize(int width, int height)
    {
        _workspaceState.WindowWidth = Math.Clamp(width, 800, 7680);
        _workspaceState.WindowHeight = Math.Clamp(height, 600, 4320);
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
            ImGui.BeginDisabled();
            ImGui.MenuItem("New Scene", "Ctrl+N");
            ImGui.MenuItem("Open Scene...", "Ctrl+Shift+O");
            ImGui.MenuItem("Save", "Ctrl+S");
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
            ImGui.BeginDisabled();
            ImGui.MenuItem("Undo", "Ctrl+Z");
            ImGui.MenuItem("Redo", "Ctrl+Y");
            ImGui.Separator();
            ImGui.MenuItem("Preferences...");
            ImGui.EndDisabled();
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Assets"))
        {
            ImGui.BeginDisabled();
            ImGui.MenuItem("Create");
            ImGui.MenuItem("Bake Selected");
            ImGui.EndDisabled();
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Entity"))
        {
            ImGui.BeginDisabled();
            ImGui.MenuItem("Create Empty");
            ImGui.MenuItem("Create From Blueprint");
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
            ImGui.BeginDisabled();
            ImGui.MenuItem("Build Game");
            ImGui.MenuItem("Bake Assets");
            ImGui.MenuItem("Rebuild All Assets");
            ImGui.EndDisabled();
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
            : $"{_project.Metadata.Name}  |  SDK {_project.Metadata.Sdk.Version}";
        ImGui.TextDisabled(projectStatus);

        var status = _projectCreationStatus.IsRunning ? "Creating project..." : "Ready";
        var statusWidth = ImGui.CalcTextSize(status).X;
        ImGui.SameLine(MathF.Max(0f, ImGui.GetWindowWidth() - statusWidth - 16f));
        ImGui.TextDisabled(status);
    }

    private void HandleShortcuts()
    {
        var io = ImGui.GetIO();
        if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.O))
            _openProjectPopupRequested = true;
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

        _panels.Dispose();
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
