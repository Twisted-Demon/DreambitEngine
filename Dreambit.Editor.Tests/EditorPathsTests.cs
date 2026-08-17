using Dreambit.Editor.Infrastructure;

namespace Dreambit.Editor.Tests;

public sealed class EditorPathsTests
{
    [Fact]
    public void ProjectScopeIsStable()
    {
        var projectPath = Path.Combine(Path.GetTempPath(), "Dreambit", "StableProject");

        var first = EditorPaths.CreateProjectScope(projectPath);
        var second = EditorPaths.CreateProjectScope(projectPath + Path.DirectorySeparatorChar);

        Assert.Equal(first, second);
        Assert.Equal(20, first.Length);
    }

    [Fact]
    public void ProjectScopesDoNotExposeAbsolutePaths()
    {
        var first = EditorPaths.CreateProjectScope(Path.Combine(Path.GetTempPath(), "ProjectA"));
        var second = EditorPaths.CreateProjectScope(Path.Combine(Path.GetTempPath(), "ProjectB"));

        Assert.NotEqual(first, second);
        Assert.DoesNotContain(Path.DirectorySeparatorChar, first);
        Assert.DoesNotContain(Path.AltDirectorySeparatorChar, first);
    }

    [Fact]
    public void ProjectLeasePathDoesNotDependOnTheStateOverride()
    {
        var project = Path.Combine(Path.GetTempPath(), "Dreambit", "LeaseProject");
        var first = EditorPaths.Create(new EditorLaunchOptions(
            project,
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            false));
        var second = EditorPaths.Create(new EditorLaunchOptions(
            project,
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            false));

        Assert.Equal(first.ProjectLockPath, second.ProjectLockPath);
        Assert.DoesNotContain(project, first.ProjectLockPath);
    }

    [Fact]
    public void CrashLogUsesTheConfiguredSettingsDirectory()
    {
        var settings = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var paths = EditorPaths.Create(new EditorLaunchOptions(null, settings, false));

        Assert.Equal(Path.Combine(Path.GetFullPath(settings), "crash.log"), paths.CrashLogPath);
    }

    [Fact]
    public void ErrorLogUsesTheConfiguredSettingsDirectory()
    {
        var settings = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var paths = EditorPaths.Create(new EditorLaunchOptions(null, settings, false));

        Assert.Equal(Path.Combine(Path.GetFullPath(settings), "errors.log"), paths.ErrorLogPath);
    }
}
