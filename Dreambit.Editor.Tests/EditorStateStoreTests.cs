using Dreambit.Editor.Infrastructure;
using Dreambit.Editor.Persistence;

namespace Dreambit.Editor.Tests;

public sealed class EditorStateStoreTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(
        Path.GetTempPath(),
        "Dreambit.Editor.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void RoundTripsGlobalAndWorkspaceState()
    {
        var paths = CreatePaths();
        var store = new EditorStateStore(paths);
        var global = new EditorGlobalState
        {
            LastProjectPath = "C:/Games/Example",
            RecentProjects =
            [
                new RecentProjectState
                {
                    Path = "C:/Games/Example",
                    Name = "Example",
                    SdkVersion = "0.1.4",
                    LastOpenedUtc = DateTimeOffset.Parse("2026-08-11T12:00:00Z")
                }
            ]
        };
        var workspace = new EditorWorkspaceState
        {
            WindowWidth = 1720,
            WindowHeight = 980,
            ProjectBrowserFolder = "characters/player",
            LastSelectedAssetPath = "characters/player/player.blueprint.json",
            LastSelectionKind = "entity",
            LastSelectedEntityIds = [Guid.Parse("b7ca9d2b-17ca-4b88-b57c-65df588355b5")],
            HierarchyExpandedEntityIds =
            [
                Guid.Parse("82e5641f-bce0-492b-b835-5ed4b2ff8df2")
            ],
            PanelVisibility = new Dictionary<string, bool>
            {
                ["Console"] = false
            }
        };

        Assert.True(store.TrySaveGlobalState(global, out var globalError), globalError);
        Assert.True(store.TrySaveWorkspaceState(workspace, out var workspaceError), workspaceError);

        var reloadedStore = new EditorStateStore(paths);
        var reloadedGlobal = reloadedStore.LoadGlobalState();
        var reloadedWorkspace = reloadedStore.LoadWorkspaceState();

        Assert.Equal(global.LastProjectPath, reloadedGlobal.LastProjectPath);
        var recent = Assert.Single(reloadedGlobal.RecentProjects);
        Assert.Equal("C:/Games/Example", recent.Path);
        Assert.Equal("Example", recent.Name);
        Assert.Equal("0.1.4", recent.SdkVersion);
        Assert.Equal(1720, reloadedWorkspace.WindowWidth);
        Assert.Equal(980, reloadedWorkspace.WindowHeight);
        Assert.Equal("characters/player", reloadedWorkspace.ProjectBrowserFolder);
        Assert.Equal("characters/player/player.blueprint.json", reloadedWorkspace.LastSelectedAssetPath);
        Assert.Equal("entity", reloadedWorkspace.LastSelectionKind);
        Assert.Single(reloadedWorkspace.LastSelectedEntityIds);
        Assert.Single(reloadedWorkspace.HierarchyExpandedEntityIds);
        Assert.False(reloadedWorkspace.PanelVisibility["Console"]);
        Assert.Empty(reloadedStore.LoadWarnings);
    }

    [Fact]
    public void CorruptStateFallsBackWithoutDestroyingTheFile()
    {
        var paths = CreatePaths();
        Directory.CreateDirectory(Path.GetDirectoryName(paths.GlobalStatePath)!);
        File.WriteAllText(paths.GlobalStatePath, "{ definitely not json }");

        var store = new EditorStateStore(paths);
        var state = store.LoadGlobalState();

        Assert.Empty(state.RecentProjects);
        Assert.Single(store.LoadWarnings);
        Assert.True(File.Exists(paths.GlobalStatePath));
    }

    [Fact]
    public void MigratesVersionOneRecentProjectPaths()
    {
        var paths = CreatePaths();
        Directory.CreateDirectory(Path.GetDirectoryName(paths.GlobalStatePath)!);
        File.WriteAllText(
            paths.GlobalStatePath,
            """
            {
              "version": 1,
              "lastProjectPath": "C:/Games/Legacy",
              "recentProjectPaths": ["C:/Games/Legacy", "C:/Games/Legacy"]
            }
            """);

        var state = new EditorStateStore(paths).LoadGlobalState();

        Assert.Equal(EditorGlobalState.CurrentVersion, state.Version);
        var recent = Assert.Single(state.RecentProjects);
        Assert.Equal("C:/Games/Legacy", recent.Path);
        Assert.Equal("Legacy", recent.Name);
        Assert.Null(state.RecentProjectPaths);
    }

    [Fact]
    public void WorkspaceDimensionsAreClampedOnLoad()
    {
        var paths = CreatePaths();
        var store = new EditorStateStore(paths);
        Assert.True(store.TrySaveWorkspaceState(
            new EditorWorkspaceState
            {
                WindowWidth = 1,
                WindowHeight = int.MaxValue
            },
            out var error), error);

        var state = new EditorStateStore(paths).LoadWorkspaceState();

        Assert.Equal(800, state.WindowWidth);
        Assert.Equal(4320, state.WindowHeight);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, true);
    }

    private EditorPaths CreatePaths() => EditorPaths.Create(
        new EditorLaunchOptions(null, _testDirectory, false));
}
