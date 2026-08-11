using Dreambit.Editor.Infrastructure;
using Dreambit.Editor.Projects;

namespace Dreambit.Editor.Tests;

public sealed class DreambitProjectManagerTests : IDisposable
{
    private readonly string _settingsDirectory = Path.Combine(
        Path.GetTempPath(),
        "Dreambit.Editor.ProjectSessionTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void SessionOwnsLifetimeAndPreventsASecondProcessLease()
    {
        using var fixture = new DreambitProjectTestFixture();
        fixture.CreateValidProject();
        var paths = EditorPaths.Create(new EditorLaunchOptions(
            fixture.Root,
            _settingsDirectory,
            false));
        using var first = new DreambitProjectManager(paths);
        using var second = new DreambitProjectManager(paths);

        Assert.True(first.TryOpen(fixture.Root, out _, out var firstError), firstError);
        var lifetime = first.CurrentSession!.LifetimeToken;
        Assert.False(ProjectInstanceLease.IsAvailable(
            EditorPaths.CreateProjectLockPath(fixture.Root)));
        Assert.False(second.TryOpen(fixture.Root, out _, out var secondError));
        Assert.Contains("already open", secondError);

        first.Dispose();

        Assert.True(lifetime.IsCancellationRequested);
        Assert.True(ProjectInstanceLease.IsAvailable(
            EditorPaths.CreateProjectLockPath(fixture.Root)));
        Assert.True(second.TryOpen(fixture.Root, out _, out var retryError), retryError);
    }

    public void Dispose()
    {
        if (Directory.Exists(_settingsDirectory))
            Directory.Delete(_settingsDirectory, true);
    }
}
