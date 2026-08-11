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

    [Fact]
    public async Task RepairsDuplicateTemplateRegistrationsAndThenReusesTheCleanHive()
    {
        var paths = EditorPaths.Create(new EditorLaunchOptions(
            null,
            _settingsDirectory,
            false));
        var packages = Path.Combine(
            paths.SdkRootDirectory,
            DreambitSdkConstants.CurrentVersion,
            "packages");
        Directory.CreateDirectory(packages);
        foreach (var packageId in DreambitSdkConstants.RequiredPackageIds)
        {
            File.WriteAllText(
                Path.Combine(packages, $"{packageId}.{DreambitSdkConstants.CurrentVersion}.nupkg"),
                packageId);
        }

        var runner = new TemplateInstallProcessRunner();
        var manager = new DreambitSdkManager(paths, new EditorLogService(), runner);
        Assert.True(manager.TryGetInstallation(
            DreambitSdkConstants.CurrentVersion,
            out var installation));
        Directory.CreateDirectory(installation!.TemplateHiveDirectory);
        var stalePath = Path.Combine(installation.TemplateHiveDirectory, "stale-cache.txt");
        File.WriteAllText(stalePath, "corrupt");
        File.WriteAllText(
            Path.Combine(installation.TemplateHiveDirectory, "packages.json"),
            """{"Packages":[{"Details":{"PackageId":"DreambitEngine.Templates"}},{"Details":{"PackageId":"DreambitEngine.Templates"}}]}""");

        await manager.EnsureTemplateInstalledAsync(installation, CancellationToken.None);
        await manager.EnsureTemplateInstalledAsync(installation, CancellationToken.None);

        Assert.Single(runner.Commands);
        Assert.False(File.Exists(stalePath));
        Assert.Single(Directory.EnumerateFiles(
            installation.TemplateHiveDirectory,
            ".dreambit-template.*"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_settingsDirectory))
            Directory.Delete(_settingsDirectory, true);
    }

    private sealed class TemplateInstallProcessRunner : IProcessRunner
    {
        public List<ProcessCommand> Commands { get; } = [];

        public Task<ProcessRunResult> RunAsync(
            ProcessCommand command,
            Action<string>? output,
            CancellationToken cancellationToken)
        {
            Commands.Add(command);
            var hiveOption = command.Arguments.ToList().IndexOf("--debug:custom-hive");
            Assert.InRange(hiveOption, 0, command.Arguments.Count - 2);
            var hive = command.Arguments[hiveOption + 1];
            Directory.CreateDirectory(hive);
            File.WriteAllText(
                Path.Combine(hive, "packages.json"),
                """{"Packages":[{"Details":{"PackageId":"DreambitEngine.Templates"}}]}""");
            return Task.FromResult(new ProcessRunResult(0, ["installed"]));
        }
    }
}
