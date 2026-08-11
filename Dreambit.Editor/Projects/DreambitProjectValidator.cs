namespace Dreambit.Editor.Projects;

internal sealed class DreambitProjectValidator
{
    private readonly DreambitProjectMetadataStore _metadataStore;

    public DreambitProjectValidator(DreambitProjectMetadataStore? metadataStore = null)
    {
        _metadataStore = metadataStore ?? new DreambitProjectMetadataStore();
    }

    public ProjectValidationResult Validate(string projectRoot)
    {
        var diagnostics = new List<ProjectDiagnostic>();
        if (!ProjectProcessLauncher.TryNormalizeProjectPath(
                projectRoot,
                out var normalizedRoot,
                out var pathError))
        {
            diagnostics.Add(new ProjectDiagnostic(
                ProjectDiagnosticSeverity.Error,
                "DBP001",
                pathError ?? "The project directory is invalid.",
                projectRoot));
            return new ProjectValidationResult(null, null, diagnostics);
        }

        var metadataPath = Path.Combine(
            normalizedRoot,
            DreambitProjectMetadata.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(metadataPath))
        {
            diagnostics.Add(new ProjectDiagnostic(
                ProjectDiagnosticSeverity.Error,
                "DBP002",
                $"'{normalizedRoot}' is not a Dreambit project because " +
                $"'{DreambitProjectMetadata.RelativePath}' is missing.",
                metadataPath));
            return new ProjectValidationResult(normalizedRoot, null, diagnostics);
        }

        if (!_metadataStore.TryLoad(metadataPath, out var metadata, out var loadDiagnostic))
        {
            diagnostics.Add(loadDiagnostic!);
            return new ProjectValidationResult(normalizedRoot, null, diagnostics);
        }

        ValidateMetadata(metadata!, metadataPath, diagnostics);

        var solutionPath = ResolveRequiredPath(
            normalizedRoot,
            metadata!.Solution,
            "solution",
            ".sln",
            false,
            diagnostics);
        var gameProjectPath = ResolveRequiredPath(
            normalizedRoot,
            metadata.GameProject,
            "game project",
            ".csproj",
            false,
            diagnostics);
        var contentProjectPath = ResolveRequiredPath(
            normalizedRoot,
            metadata.ContentProject,
            "Content project",
            ".csproj",
            false,
            diagnostics);
        var contentRootPath = ResolveRequiredPath(
            normalizedRoot,
            metadata.ContentRoot,
            "Content root",
            null,
            true,
            diagnostics);
        var launcherProjectPath = ResolveRequiredPath(
            normalizedRoot,
            metadata.LauncherProject,
            "launcher project",
            ".csproj",
            false,
            diagnostics);

        if (diagnostics.Any(static diagnostic =>
                diagnostic.Severity == ProjectDiagnosticSeverity.Error))
        {
            return new ProjectValidationResult(normalizedRoot, null, diagnostics);
        }

        var definition = new DreambitProjectDefinition(
            normalizedRoot,
            metadataPath,
            metadata,
            solutionPath!,
            gameProjectPath!,
            contentProjectPath!,
            contentRootPath!,
            launcherProjectPath!);
        return new ProjectValidationResult(normalizedRoot, definition, diagnostics);
    }

    private static void ValidateMetadata(
        DreambitProjectMetadata metadata,
        string metadataPath,
        ICollection<ProjectDiagnostic> diagnostics)
    {
        if (metadata.SchemaVersion != DreambitProjectMetadata.CurrentSchemaVersion)
        {
            diagnostics.Add(Error(
                "DBP004",
                $"Unsupported Dreambit project schema version {metadata.SchemaVersion}. " +
                $"This Editor supports version {DreambitProjectMetadata.CurrentSchemaVersion}.",
                metadataPath));
        }

        if (metadata.ProjectId == Guid.Empty)
            diagnostics.Add(Error("DBP005", "Project ID must be a non-empty GUID.", metadataPath));

        if (string.IsNullOrWhiteSpace(metadata.Name) ||
            metadata.Name.Length > 128 ||
            metadata.Name.Any(char.IsControl))
        {
            diagnostics.Add(Error(
                "DBP006",
                "Project name must be non-empty, at most 128 characters, and contain no control characters.",
                metadataPath));
        }

        if (!string.Equals(metadata.TargetRenderer, "DesktopVK", StringComparison.Ordinal))
        {
            diagnostics.Add(Error(
                "DBP007",
                $"Target renderer '{metadata.TargetRenderer}' is not supported by this Editor. " +
                "Milestone 2 supports DesktopVK.",
                metadataPath));
        }

        if (metadata.Sdk is null || !DreambitSdkVersion.IsValid(metadata.Sdk.Version))
        {
            diagnostics.Add(Error(
                "DBP008",
                "Dreambit SDK version must be a portable NuGet-compatible version string.",
                metadataPath));
        }
        else if (!string.Equals(
                     metadata.Sdk.Version,
                     DreambitSdkConstants.CurrentVersion,
                     StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(Error(
                "DBP011",
                $"This project requires Dreambit SDK {metadata.Sdk.Version}, but this Editor " +
                $"provides SDK {DreambitSdkConstants.CurrentVersion}.",
                metadataPath));
        }
    }

    private static string? ResolveRequiredPath(
        string projectRoot,
        string relativePath,
        string description,
        string? requiredExtension,
        bool directory,
        ICollection<ProjectDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            diagnostics.Add(Error(
                "DBP009",
                $"The {description} path must be a project-relative path.",
                relativePath));
            return null;
        }

        string resolvedPath;
        try
        {
            resolvedPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            diagnostics.Add(Error(
                "DBP009",
                $"The {description} path is invalid. {exception.Message}",
                relativePath));
            return null;
        }

        var rootWithSeparator = projectRoot.EndsWith(Path.DirectorySeparatorChar)
            ? projectRoot
            : projectRoot + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!resolvedPath.StartsWith(rootWithSeparator, comparison))
        {
            diagnostics.Add(Error(
                "DBP009",
                $"The {description} path escapes the project directory.",
                relativePath));
            return null;
        }

        if (requiredExtension is not null &&
            !string.Equals(
                Path.GetExtension(resolvedPath),
                requiredExtension,
                StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(Error(
                "DBP009",
                $"The {description} path must end with '{requiredExtension}'.",
                relativePath));
        }

        var exists = directory
            ? Directory.Exists(resolvedPath)
            : File.Exists(resolvedPath);
        if (!exists)
        {
            diagnostics.Add(Error(
                "DBP010",
                $"The configured {description} does not exist.",
                resolvedPath));
        }

        return resolvedPath;
    }

    private static ProjectDiagnostic Error(string code, string message, string? path) =>
        new(ProjectDiagnosticSeverity.Error, code, message, path);
}
