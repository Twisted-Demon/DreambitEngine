using Dreambit.Editor.Projects;

namespace Dreambit.Editor.Tests;

public sealed class ProjectProcessLauncherTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(
        Path.GetTempPath(),
        "Dreambit.Editor.ProjectTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void NormalizesAnExistingProjectDirectory()
    {
        Directory.CreateDirectory(_testDirectory);

        var valid = ProjectProcessLauncher.TryNormalizeProjectPath(
            _testDirectory + Path.DirectorySeparatorChar,
            out var normalizedPath,
            out var error);

        Assert.True(valid, error);
        Assert.Equal(
            Path.GetFullPath(_testDirectory).TrimEnd(Path.DirectorySeparatorChar),
            normalizedPath);
    }

    [Fact]
    public void RejectsAMissingProjectDirectory()
    {
        var valid = ProjectProcessLauncher.TryNormalizeProjectPath(
            _testDirectory,
            out _,
            out var error);

        Assert.False(valid);
        Assert.Contains("does not exist", error);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, true);
    }
}
