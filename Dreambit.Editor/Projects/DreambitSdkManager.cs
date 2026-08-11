using System.Text.Json;
using Dreambit.Editor.Infrastructure;
using Dreambit.Editor.Logging;

namespace Dreambit.Editor.Projects;

internal sealed class DreambitSdkManager
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly EditorPaths _paths;
    private readonly IProcessRunner _processRunner;
    private readonly EditorLogService _logs;
    private readonly SemaphoreSlim _installationGate = new(1, 1);

    public DreambitSdkManager(
        EditorPaths paths,
        EditorLogService logs,
        IProcessRunner? processRunner = null)
    {
        _paths = paths;
        _logs = logs;
        _processRunner = processRunner ?? new ProcessRunner();
    }

    public async Task<DreambitSdkInstallation> EnsureInstalledAsync(
        string version,
        CancellationToken cancellationToken)
    {
        if (!DreambitSdkVersion.IsValid(version))
            throw new ArgumentException("Dreambit SDK version is invalid.", nameof(version));

        await _installationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (TryGetInstallation(version, out var existing))
                return existing!;

            var sdkRoot = Path.Combine(_paths.SdkRootDirectory, version);
            var packagesDirectory = Path.Combine(sdkRoot, "packages");
            Directory.CreateDirectory(packagesDirectory);

            var sourceKind = TryInstallBundledPackages(version, packagesDirectory)
                ? DreambitSdkSourceKind.Bundled
                : await PackDevelopmentSdkAsync(
                        version,
                        packagesDirectory,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (!TryCreateInstallation(version, sourceKind, out var installation))
            {
                throw new InvalidOperationException(
                    $"Dreambit SDK {version} did not produce the required coordinated packages.");
            }

            WriteManifest(installation!);
            return installation!;
        }
        finally
        {
            _installationGate.Release();
        }
    }

    public async Task EnsureTemplateInstalledAsync(
        DreambitSdkInstallation installation,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(installation.TemplateHiveDirectory);
        _logs.Info("SDK", $"Preparing Dreambit project template {installation.Version}.");
        var result = await _processRunner.RunAsync(
                new ProcessCommand(
                    "dotnet",
                    [
                        "new",
                        "--debug:custom-hive",
                        installation.TemplateHiveDirectory,
                        "install",
                        installation.TemplatePackagePath,
                        "--force"
                    ],
                    installation.RootDirectory),
                line => LogProcessOutput("Template", line),
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not install Dreambit project template {installation.Version}. " +
                $"dotnet new exited with code {result.ExitCode}." +
                FormatFailureDetails(result));
        }
    }

    internal bool TryGetInstallation(
        string version,
        out DreambitSdkInstallation? installation) =>
        TryCreateInstallation(version, null, out installation);

    private bool TryCreateInstallation(
        string version,
        DreambitSdkSourceKind? sourceKind,
        out DreambitSdkInstallation? installation)
    {
        var root = Path.Combine(_paths.SdkRootDirectory, version);
        var packages = Path.Combine(root, "packages");
        foreach (var packageId in DreambitSdkConstants.RequiredPackageIds)
        {
            if (!File.Exists(GetPackagePath(packages, packageId, version)))
            {
                installation = null;
                return false;
            }
        }

        installation = new DreambitSdkInstallation(
            version,
            root,
            packages,
            Path.Combine(root, "template-hive"),
            GetPackagePath(
                packages,
                DreambitSdkConstants.TemplatePackageId,
                version),
            sourceKind ?? ReadSourceKind(root));
        return true;
    }

    private bool TryInstallBundledPackages(string version, string destination)
    {
        var bundledPackages = Path.Combine(
            AppContext.BaseDirectory,
            "SDK",
            version,
            "packages");
        foreach (var packageId in DreambitSdkConstants.RequiredPackageIds)
        {
            if (!File.Exists(GetPackagePath(bundledPackages, packageId, version)))
                return false;
        }

        foreach (var package in Directory.EnumerateFiles(bundledPackages, "*.nupkg"))
            File.Copy(package, Path.Combine(destination, Path.GetFileName(package)), true);

        _logs.Info("SDK", $"Installed bundled Dreambit SDK {version}.");
        return true;
    }

    private async Task<DreambitSdkSourceKind> PackDevelopmentSdkAsync(
        string version,
        string destination,
        CancellationToken cancellationToken)
    {
        var sourceRoot = FindDevelopmentSourceRoot();
        if (sourceRoot is null)
        {
            throw new InvalidOperationException(
                $"Dreambit SDK {version} is not installed and this Editor does not contain " +
                "a bundled SDK. Install the matching SDK package set before creating a project.");
        }

        _logs.Info("SDK", $"Packaging development Dreambit SDK {version}.");
        var projects = new[]
        {
            Path.Combine(sourceRoot, "DreambitEngine", "DreambitEngine.csproj"),
            Path.Combine(sourceRoot, "DreambitEngine.Build", "DreambitEngine.Build.csproj"),
            Path.Combine(sourceRoot, "DreambitEngine.Templates", "DreambitEngine.Templates.csproj")
        };

        foreach (var project in projects)
        {
            var result = await _processRunner.RunAsync(
                    new ProcessCommand(
                        "dotnet",
                        [
                            "pack",
                            project,
                            "-c",
                            "Release",
                            $"-p:PackageVersion={version}",
                            "-o",
                            destination,
                            "--nologo"
                        ],
                        sourceRoot),
                    line => LogProcessOutput("SDK", line),
                    cancellationToken)
                .ConfigureAwait(false);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not package '{Path.GetFileNameWithoutExtension(project)}'. " +
                    $"dotnet pack exited with code {result.ExitCode}." +
                    FormatFailureDetails(result));
            }
        }

        return DreambitSdkSourceKind.DevelopmentSource;
    }

    private static string? FindDevelopmentSourceRoot()
    {
        var candidates = new[]
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        };

        foreach (var candidate in candidates)
        {
            var directory = new DirectoryInfo(candidate);
            for (var depth = 0; directory is not null && depth < 12; depth++)
            {
                if (File.Exists(Path.Combine(
                        directory.FullName,
                        "DreambitEngine",
                        "DreambitEngine.csproj")) &&
                    File.Exists(Path.Combine(
                        directory.FullName,
                        "DreambitEngine.Templates",
                        "DreambitEngine.Templates.csproj")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        return null;
    }

    private static string GetPackagePath(string directory, string packageId, string version) =>
        Path.Combine(directory, $"{packageId}.{version}.nupkg");

    private void WriteManifest(DreambitSdkInstallation installation)
    {
        var manifest = new DreambitSdkManifest
        {
            Version = installation.Version,
            SourceKind = installation.SourceKind.ToString(),
            InstalledAtUtc = DateTimeOffset.UtcNow,
            Packages = DreambitSdkConstants.RequiredPackageIds.ToList()
        };
        var path = Path.Combine(installation.RootDirectory, "sdk.json");
        var temporaryPath = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(manifest, SerializerOptions));
            File.Move(temporaryPath, path, true);
        }
        finally
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static DreambitSdkSourceKind ReadSourceKind(string root)
    {
        var path = Path.Combine(root, "sdk.json");
        try
        {
            if (File.Exists(path))
            {
                var manifest = JsonSerializer.Deserialize<DreambitSdkManifest>(
                    File.ReadAllText(path),
                    SerializerOptions);
                if (Enum.TryParse<DreambitSdkSourceKind>(
                        manifest?.SourceKind,
                        out var sourceKind))
                {
                    return sourceKind;
                }
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
        }

        return DreambitSdkSourceKind.Bundled;
    }

    private void LogProcessOutput(string category, string line)
    {
        if (!string.IsNullOrWhiteSpace(line))
            _logs.Info(category, line);
    }

    private static string FormatFailureDetails(ProcessRunResult result)
    {
        var details = result.Output
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .TakeLast(8)
            .ToArray();
        return details.Length == 0
            ? string.Empty
            : Environment.NewLine + string.Join(Environment.NewLine, details);
    }
}
