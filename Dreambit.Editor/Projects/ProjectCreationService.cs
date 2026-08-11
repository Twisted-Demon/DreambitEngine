using Dreambit.Editor.Logging;

namespace Dreambit.Editor.Projects;

internal sealed record ProjectCreationResult(
    bool Succeeded,
    string? ProjectRoot,
    string Message,
    ProjectValidationResult? Validation = null);

internal sealed class ProjectCreationService
{
    private readonly DreambitSdkManager _sdkManager;
    private readonly DreambitProjectValidator _validator;
    private readonly IProcessRunner _processRunner;
    private readonly EditorLogService _logs;

    public ProjectCreationService(
        DreambitSdkManager sdkManager,
        EditorLogService logs,
        DreambitProjectValidator? validator = null,
        IProcessRunner? processRunner = null)
    {
        _sdkManager = sdkManager;
        _logs = logs;
        _validator = validator ?? new DreambitProjectValidator();
        _processRunner = processRunner ?? new ProcessRunner();
    }

    public async Task<ProjectCreationResult> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.TryValidate(out var outputPath, out var requestError))
            return new ProjectCreationResult(false, null, requestError!);

        try
        {
            _logs.Info("Project", $"Preparing Dreambit SDK {request.SdkVersion}.");
            var sdk = await _sdkManager.EnsureInstalledAsync(
                    request.SdkVersion,
                    cancellationToken)
                .ConfigureAwait(false);
            await _sdkManager.EnsureTemplateInstalledAsync(sdk, cancellationToken)
                .ConfigureAwait(false);

            _logs.Info("Project", $"Creating project '{request.Name}' at '{outputPath}'.");
            var createResult = await _processRunner.RunAsync(
                    new ProcessCommand(
                        "dotnet",
                        [
                            "new",
                            "--debug:custom-hive",
                            sdk.TemplateHiveDirectory,
                            DreambitSdkConstants.TemplateShortName,
                            "-n",
                            request.Name,
                            "-o",
                            outputPath,
                            "--game-title",
                            request.GameTitle,
                            "--sdkVersion",
                            request.SdkVersion,
                            "--targetRenderer",
                            request.TargetRenderer,
                            "--no-update-check"
                        ],
                        Path.GetDirectoryName(outputPath)!),
                    line => LogProcessOutput("Template", line),
                    cancellationToken)
                .ConfigureAwait(false);

            if (!createResult.Succeeded)
            {
                return new ProjectCreationResult(
                    false,
                    Directory.Exists(outputPath) ? outputPath : null,
                    $"Project generation failed with exit code {createResult.ExitCode}. " +
                    "Any generated files were preserved for diagnosis." +
                    FormatFailureDetails(createResult));
            }

            var validation = _validator.Validate(outputPath);
            if (!validation.IsValid)
            {
                return new ProjectCreationResult(
                    false,
                    outputPath,
                    $"The template generated an invalid Dreambit project.{Environment.NewLine}" +
                    validation.ErrorSummary,
                    validation);
            }

            _logs.Info("Project", "Restoring the generated project.");
            var restoreResult = await _processRunner.RunAsync(
                    new ProcessCommand(
                        "dotnet",
                        [
                            "restore",
                            validation.Project!.SolutionPath,
                            $"-p:RestoreAdditionalProjectSources={sdk.PackagesDirectory}",
                            "--nologo"
                        ],
                        outputPath),
                    line => LogProcessOutput("Restore", line),
                    cancellationToken)
                .ConfigureAwait(false);

            if (!restoreResult.Succeeded)
            {
                return new ProjectCreationResult(
                    false,
                    outputPath,
                    $"The project was created, but package restore failed with exit code " +
                    $"{restoreResult.ExitCode}. The project files were preserved." +
                    FormatFailureDetails(restoreResult),
                    validation);
            }

            return new ProjectCreationResult(
                true,
                outputPath,
                $"Created '{request.Name}' with Dreambit SDK {request.SdkVersion}.",
                validation);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new ProjectCreationResult(false, null, "Project creation was cancelled.");
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _logs.Error("Project", "Project creation failed.", exception);
            return new ProjectCreationResult(false, null, exception.Message);
        }
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
