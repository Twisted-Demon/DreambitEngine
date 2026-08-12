using System.Numerics;
using Dreambit.Editor.Persistence;
using Dreambit.Editor.Projects;
using ImGuiNET;

namespace Dreambit.Editor.UI;

internal sealed record ProjectCreationStatus(
    bool IsRunning,
    string? Message = null,
    bool IsError = false);

internal sealed class ProjectLauncherView
{
    private readonly EditorGlobalState _globalState;
    private string _projectPath;
    private string _projectName = "MyGame";
    private string _gameTitle = "My Game";
    private string _projectLocation;
    private string? _error;
    private bool _openCreatePopup;

    public ProjectLauncherView(EditorGlobalState globalState, string? initialError = null)
    {
        _globalState = globalState;
        _projectPath = globalState.LastProjectPath ?? string.Empty;
        _projectLocation = GetDefaultProjectLocation();
        _error = initialError;
    }

    public void Draw(
        Func<string, bool> launchProject,
        Func<CreateProjectRequest, bool> createProject,
        ProjectCreationStatus creationStatus)
    {
        var viewport = ImGui.GetMainViewport();
        var size = new Vector2(
            MathF.Min(760f, viewport.WorkSize.X - 40f),
            MathF.Min(560f, viewport.WorkSize.Y - 40f));
        size.X = MathF.Max(size.X, 440f);
        size.Y = MathF.Max(size.Y, 360f);

        ImGui.SetNextWindowPos(
            viewport.WorkPos + viewport.WorkSize * 0.5f,
            ImGuiCond.Always,
            new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(size, ImGuiCond.Always);

        var flags =
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoDocking;

        if (!ImGui.Begin("Dreambit Projects##Dreambit.Editor.ProjectLauncher", flags))
        {
            ImGui.End();
            return;
        }

        ImGui.Text("Dreambit Editor");
        ImGui.TextDisabled("Create and open portable Dreambit projects");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Open Existing Project");
        ImGui.SetNextItemWidth(-100f);
        ImGui.InputTextWithHint(
            "##ProjectPath",
            "Project directory containing .dreambit/project.json",
            ref _projectPath,
            1_024);
        ImGui.SameLine();
        if (ImGui.Button("Open", new Vector2(88f, 0f)))
            _error = launchProject(_projectPath) ? null : _error;

        DrawError(_error);

        ImGui.Spacing();
        ImGui.Text("Recent Projects");
        ImGui.Separator();

        if (_globalState.RecentProjects.Count == 0)
        {
            ImGui.TextDisabled("No recent projects yet.");
        }
        else
        {
            var childVisible = ImGui.BeginChild(
                "##RecentProjects",
                new Vector2(0f, -72f),
                ImGuiChildFlags.None);
            if (childVisible)
            {
                for (var i = 0; i < _globalState.RecentProjects.Count; i++)
                {
                    var project = _globalState.RecentProjects[i];

                    var displayName = string.IsNullOrWhiteSpace(project.Name)
                        ? Path.GetFileName(project.Path)
                        : project.Name;
                    var sdk = string.IsNullOrWhiteSpace(project.SdkVersion)
                        ? "Unknown SDK"
                        : $"SDK {project.SdkVersion}";
                    var missing = !Directory.Exists(project.Path);
                    var label = missing
                        ? $"{displayName}  (Missing)\n{project.Path}##Recent:{project.Path}"
                        : $"{displayName}  |  {sdk}\n{project.Path}##Recent:{project.Path}";

                    if (!ImGui.Selectable(label))
                        continue;

                    _projectPath = project.Path;
                    _error = launchProject(project.Path) ? null : _error;

                    // The launch callback may reorder RecentProjects by moving the
                    // selected project to the front. Do not inspect the collection
                    // again during this frame.
                    break;
                }
            }

            ImGui.EndChild();
        }

        ImGui.Spacing();
        if (ImGui.Button("Create Project", new Vector2(130f, 0f)))
            _openCreatePopup = true;

        ImGui.SameLine();
        ImGui.TextDisabled($"Dreambit SDK {DreambitSdkConstants.CurrentVersion} / DesktopVK");
        ImGui.End();

        DrawCreateProjectPopup(createProject, creationStatus);
    }

    public void SetError(string error)
    {
        _error = error;
    }

    private void DrawCreateProjectPopup(
        Func<CreateProjectRequest, bool> createProject,
        ProjectCreationStatus creationStatus)
    {
        if (_openCreatePopup)
        {
            ImGui.OpenPopup("Create Dreambit Project##Dreambit.Editor.CreateProject");
            _openCreatePopup = false;
        }

        var isOpen = true;
        if (!ImGui.BeginPopupModal(
                "Create Dreambit Project##Dreambit.Editor.CreateProject",
                ref isOpen,
                ImGuiWindowFlags.AlwaysAutoResize))
        {
            return;
        }

        DrawInput("Project Name", "##CreateProjectName", ref _projectName);
        DrawInput("Game Title", "##CreateGameTitle", ref _gameTitle);
        DrawInput("Location", "##CreateProjectLocation", ref _projectLocation, 1_024);

        ImGui.Text("Target Renderer");
        ImGui.SameLine(150f);
        ImGui.TextDisabled("DesktopVK");
        ImGui.Text("Dreambit SDK");
        ImGui.SameLine(150f);
        ImGui.TextDisabled(DreambitSdkConstants.CurrentVersion);
        ImGui.Spacing();
        ImGui.TextDisabled("The matching SDK packages are installed into the Editor cache on first use.");

        if (!string.IsNullOrWhiteSpace(creationStatus.Message))
        {
            ImGui.Spacing();
            if (creationStatus.IsError)
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.96f, 0.34f, 0.36f, 1f));
            else
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.38f, 0.78f, 0.52f, 1f));
            ImGui.TextWrapped(creationStatus.Message);
            ImGui.PopStyleColor();
        }

        ImGui.Spacing();
        ImGui.BeginDisabled(creationStatus.IsRunning);
        if (ImGui.Button("Create", new Vector2(96f, 0f)))
        {
            createProject(new CreateProjectRequest(
                _projectName,
                _projectLocation,
                _gameTitle,
                "DesktopVK",
                DreambitSdkConstants.CurrentVersion));
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(96f, 0f)))
            ImGui.CloseCurrentPopup();
        ImGui.EndDisabled();

        if (creationStatus.IsRunning)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("Creating project...");
        }

        ImGui.EndPopup();
    }

    private static void DrawInput(
        string label,
        string id,
        ref string value,
        uint capacity = 256)
    {
        ImGui.Text(label);
        ImGui.SameLine(150f);
        ImGui.SetNextItemWidth(440f);
        ImGui.InputText(id, ref value, capacity);
    }

    private static void DrawError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return;

        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.96f, 0.34f, 0.36f, 1f));
        ImGui.TextWrapped(error);
        ImGui.PopStyleColor();
    }

    private static string GetDefaultProjectLocation()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return string.IsNullOrWhiteSpace(documents)
            ? Directory.GetCurrentDirectory()
            : documents;
    }
}
