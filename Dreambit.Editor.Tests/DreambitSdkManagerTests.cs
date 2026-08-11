using Dreambit.Editor.Infrastructure;
using Dreambit.Editor.Logging;
using Dreambit.Editor.Projects;

namespace Dreambit.Editor.Tests;

public sealed class DreambitSdkManagerTests : IDisposable
{
    private readonly string _settingsDirectory = Path.Combine(
        Path.GetTempPath(),
        "Dreambit.Editor.SdkTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void DiscoversOnlyCompleteCoordinatedSdkInstallations()
    {
        var paths = EditorPaths.Create(new EditorLaunchOptions(
            null,
            _settingsDirectory,
            false));
        var manager = new DreambitSdkManager(paths, new EditorLogService());
        var packages = Path.Combine(
            paths.SdkRootDirectory,
            DreambitSdkConstants.CurrentVersion,
            "packages");
        Directory.CreateDirectory(packages);

        foreach (var packageId in DreambitSdkConstants.RequiredPackageIds)
        {
            File.WriteAllBytes(
                Path.Combine(
                    packages,
                    $"{packageId}.{DreambitSdkConstants.CurrentVersion}.nupkg"),
                []);
        }

        Assert.True(manager.TryGetInstallation(
            DreambitSdkConstants.CurrentVersion,
            out var installation));
        Assert.Equal(packages, installation!.PackagesDirectory);

        File.Delete(Path.Combine(
            packages,
            $"{DreambitSdkConstants.BuildPackageId}.{DreambitSdkConstants.CurrentVersion}.nupkg"));
        Assert.False(manager.TryGetInstallation(
            DreambitSdkConstants.CurrentVersion,
            out _));
    }

    public void Dispose()
    {
        if (Directory.Exists(_settingsDirectory))
            Directory.Delete(_settingsDirectory, true);
    }
}
