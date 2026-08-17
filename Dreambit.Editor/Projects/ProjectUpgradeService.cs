using Dreambit.Editor.Infrastructure;
using Dreambit.Editor.Logging;
using System.Text.RegularExpressions;

namespace Dreambit.Editor.Projects;

internal sealed record ProjectUpgradeCandidate(
    string ProjectRoot,
    string ProjectName,
    string CurrentVersion);

internal sealed record ProjectUpgradeResult(bool Succeeded, string Message);

internal sealed class ProjectUpgradeService
{
    private readonly DreambitSdkManager _sdkManager;
    private readonly DreambitProjectMetadataStore _metadataStore;
    private readonly IProcessRunner _processRunner;
    private readonly EditorLogService _logs;

    public ProjectUpgradeService(
        DreambitSdkManager sdkManager,
        EditorLogService logs,
        DreambitProjectMetadataStore? metadataStore = null,
        IProcessRunner? processRunner = null)
    {
        _sdkManager = sdkManager;
        _logs = logs;
        _metadataStore = metadataStore ?? new DreambitProjectMetadataStore();
        _processRunner = processRunner ?? new ProcessRunner();
    }

    public bool TryGetUpgradeCandidate(string projectRoot, out ProjectUpgradeCandidate? candidate)
    {
        candidate = null;
        if (!ProjectProcessLauncher.TryNormalizeProjectPath(projectRoot, out var normalizedRoot, out _))
            return false;

        var metadataPath = Path.Combine(
            normalizedRoot,
            DreambitProjectMetadata.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!_metadataStore.TryLoad(metadataPath, out var metadata, out _) ||
            metadata?.Sdk is null ||
            !DreambitSdkVersion.TryCompare(
                metadata.Sdk.Version,
                DreambitSdkConstants.CurrentVersion,
                out var comparison) ||
            comparison >= 0)
        {
            return false;
        }

        candidate = new ProjectUpgradeCandidate(
            normalizedRoot,
            metadata.Name,
            metadata.Sdk.Version);
        return true;
    }

    public async Task<ProjectUpgradeResult> UpgradeAsync(
        ProjectUpgradeCandidate candidate,
        CancellationToken cancellationToken)
    {
        try
        {
            _logs.Info(
                "Project",
                $"Preparing Dreambit SDK {DreambitSdkConstants.CurrentVersion} to update " +
                $"'{candidate.ProjectName}'.");
            var sdk = await _sdkManager.EnsureInstalledAsync(
                    DreambitSdkConstants.CurrentVersion,
                    cancellationToken)
                .ConfigureAwait(false);
            var updateScript = Path.Combine(
                candidate.ProjectRoot,
                "scripts",
                "update-dreambit.ps1");
            if (!File.Exists(updateScript))
            {
                return await UpgradeLegacyProjectAsync(sdk, candidate, cancellationToken)
                    .ConfigureAwait(false);
            }

            var shell = OperatingSystem.IsWindows() ? "powershell" : "pwsh";
            var result = await _processRunner.RunAsync(
                    new ProcessCommand(
                        shell,
                        [
                            "-NoProfile",
                            "-ExecutionPolicy",
                            "Bypass",
                            "-File",
                            updateScript,
                            "-SdkVersion",
                            DreambitSdkConstants.CurrentVersion,
                            "-PackageSource",
                            sdk.PackagesDirectory
                        ],
                        candidate.ProjectRoot),
                    line => LogProcessOutput(line),
                    cancellationToken)
                .ConfigureAwait(false);

            if (!result.Succeeded)
            {
                return new ProjectUpgradeResult(
                    false,
                    $"The update failed with exit code {result.ExitCode}. The project was restored " +
                    $"to its prior version." + FormatFailureDetails(result));
            }

            return new ProjectUpgradeResult(
                true,
                $"Updated '{candidate.ProjectName}' to Dreambit SDK " +
                $"{DreambitSdkConstants.CurrentVersion}.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new ProjectUpgradeResult(false, "Project update was cancelled.");
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _logs.Error("Project", "Project update failed.", exception);
            return new ProjectUpgradeResult(false, exception.Message);
        }
    }

    private void LogProcessOutput(string line)
    {
        if (!string.IsNullOrWhiteSpace(line))
            _logs.Info("Update", line);
    }

    private async Task<ProjectUpgradeResult> UpgradeLegacyProjectAsync(
        DreambitSdkInstallation sdk,
        ProjectUpgradeCandidate candidate,
        CancellationToken cancellationToken)
    {
        var packageVersionsPath = Path.Combine(candidate.ProjectRoot, "Directory.Packages.props");
        var metadataPath = Path.Combine(
            candidate.ProjectRoot,
            DreambitProjectMetadata.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(packageVersionsPath) || !File.Exists(metadataPath))
        {
            return new ProjectUpgradeResult(
                false,
                "This older project has no updater and is missing files required for an automatic update.");
        }

        var originalPackageVersions = await File.ReadAllTextAsync(
                packageVersionsPath,
                cancellationToken)
            .ConfigureAwait(false);
        var originalMetadata = await File.ReadAllTextAsync(metadataPath, cancellationToken)
            .ConfigureAwait(false);
        var updatedSuccessfully = false;

        try
        {
            var updatedPackageVersions = originalPackageVersions;
            foreach (var packageId in new[]
                     {
                         DreambitSdkConstants.RuntimePackageId,
                         DreambitSdkConstants.EditorApiPackageId,
                         DreambitSdkConstants.BuildPackageId
                     })
            {
                var pattern = $"(<PackageVersion\\s+Include=\"{Regex.Escape(packageId)}\"\\s+Version=\")[^\"]+(\"\\s*/?>)";
                if (!Regex.IsMatch(updatedPackageVersions, pattern))
                {
                    return new ProjectUpgradeResult(
                        false,
                        $"Directory.Packages.props does not contain a version entry for '{packageId}'.");
                }

                updatedPackageVersions = Regex.Replace(
                    updatedPackageVersions,
                    pattern,
                    "${1}" + DreambitSdkConstants.CurrentVersion + "${2}");
            }

            if (!_metadataStore.TryLoad(metadataPath, out var metadata, out var diagnostic) ||
                metadata?.Sdk is null)
            {
                return new ProjectUpgradeResult(
                    false,
                    diagnostic?.Message ?? "Could not read the project metadata needed for update.");
            }

            metadata.Sdk.Version = DreambitSdkConstants.CurrentVersion;
            await File.WriteAllTextAsync(
                    packageVersionsPath,
                    updatedPackageVersions,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!_metadataStore.TrySave(candidate.ProjectRoot, metadata, out var metadataError))
            {
                return new ProjectUpgradeResult(
                    false,
                    metadataError ?? "Could not update project metadata.");
            }

            var restoreResult = await _processRunner.RunAsync(
                    new ProcessCommand(
                        "dotnet",
                        [
                            "restore",
                            metadata.Solution,
                            "--nologo",
                            $"-p:RestoreAdditionalProjectSources={sdk.PackagesDirectory}"
                        ],
                        candidate.ProjectRoot),
                    LogProcessOutput,
                    cancellationToken)
                .ConfigureAwait(false);
            if (restoreResult.Succeeded)
            {
                updatedSuccessfully = true;
                return new ProjectUpgradeResult(
                    true,
                    $"Updated '{candidate.ProjectName}' to Dreambit SDK " +
                    $"{DreambitSdkConstants.CurrentVersion}.");
            }

            return new ProjectUpgradeResult(
                false,
                $"The update failed with exit code {restoreResult.ExitCode}. The project was " +
                $"restored to its prior version." + FormatFailureDetails(restoreResult));
        }
        finally
        {
            if (!updatedSuccessfully)
            {
                await File.WriteAllTextAsync(packageVersionsPath, originalPackageVersions, CancellationToken.None)
                    .ConfigureAwait(false);
                await File.WriteAllTextAsync(metadataPath, originalMetadata, CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }
    }

    private static string FormatFailureDetails(ProcessRunResult result)
    {
        var details = result.Output
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Where(static line => !line.TrimStart().StartsWith("at ", StringComparison.Ordinal))
            .TakeLast(12)
            .ToArray();
        return details.Length == 0
            ? string.Empty
            : Environment.NewLine + string.Join(Environment.NewLine, details);
    }
}
