using Dreambit.Editor.Assets;
using Dreambit.Editor.Infrastructure;

namespace Dreambit.Editor.Projects;

internal sealed class DreambitProjectManager : IDisposable
{
    private readonly EditorPaths _editorPaths;
    private readonly DreambitProjectValidator _validator;
    private readonly Action<AssetDatabaseDiagnostic>? _reportAssetDiagnostic;
    private DreambitProjectSession? _session;
    private bool _disposed;

    public DreambitProjectManager(
        EditorPaths editorPaths,
        DreambitProjectValidator? validator = null,
        Action<AssetDatabaseDiagnostic>? reportAssetDiagnostic = null)
    {
        _editorPaths = editorPaths;
        _validator = validator ?? new DreambitProjectValidator();
        _reportAssetDiagnostic = reportAssetDiagnostic;
    }

    public DreambitProjectSession? CurrentSession => _session;

    public ProjectValidationResult Validate(string projectRoot) =>
        _validator.Validate(projectRoot);

    public bool TryOpen(
        string projectRoot,
        out ProjectValidationResult validation,
        out string? error)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_session is not null)
        {
            validation = new ProjectValidationResult(
                _session.Project.RootDirectory,
                _session.Project,
                []);
            error = "A project session is already open in this Editor process.";
            return false;
        }

        validation = _validator.Validate(projectRoot);
        if (!validation.IsValid)
        {
            error = validation.ErrorSummary;
            return false;
        }

        if (!ProjectInstanceLease.TryAcquire(
                _editorPaths.ProjectLockPath,
                validation.Project!.RootDirectory,
                out var lease,
                out error))
        {
            return false;
        }

        try
        {
            _session = new DreambitProjectSession(
                validation.Project,
                lease!,
                _reportAssetDiagnostic);
        }
        catch (Exception exception) when (
            exception is AssetDatabaseException or IOException or UnauthorizedAccessException)
        {
            lease!.Dispose();
            error = $"Could not initialize the asset database. {exception.Message}";
            return false;
        }

        error = null;
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _session?.Dispose();
        _session = null;
        _disposed = true;
    }
}
