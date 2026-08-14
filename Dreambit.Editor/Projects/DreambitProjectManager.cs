using Dreambit.Editor.Assets;
using Dreambit.Editor.Compilation;
using Dreambit.Editor.Infrastructure;

namespace Dreambit.Editor.Projects;

internal sealed class DreambitProjectManager : IDisposable
{
    private readonly EditorPaths _editorPaths;
    private readonly DreambitProjectValidator _validator;
    private readonly Action<AssetDatabaseDiagnostic>? _reportAssetDiagnostic;
    private readonly Action<AssetBakeMessage>? _reportAssetBake;
    private readonly Action<GameCodeMessage>? _reportGameCode;
    private readonly Action<string, Exception?>? _reportSceneError;
    private DreambitProjectSession? _session;
    private bool _disposed;

    public DreambitProjectManager(
        EditorPaths editorPaths,
        DreambitProjectValidator? validator = null,
        Action<AssetDatabaseDiagnostic>? reportAssetDiagnostic = null,
        Action<AssetBakeMessage>? reportAssetBake = null,
        Action<GameCodeMessage>? reportGameCode = null,
        Action<string, Exception?>? reportSceneError = null)
    {
        _editorPaths = editorPaths;
        _validator = validator ?? new DreambitProjectValidator();
        _reportAssetDiagnostic = reportAssetDiagnostic;
        _reportAssetBake = reportAssetBake;
        _reportGameCode = reportGameCode;
        _reportSceneError = reportSceneError;
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
                _reportAssetDiagnostic,
                _reportAssetBake,
                _reportGameCode,
                _reportSceneError);
        }
        catch (Exception exception)
        {
            lease!.Dispose();
            error = $"Could not initialize the project session. {exception.Message}";
            return false;
        }

        error = null;
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        var session = _session;
        _session = null;
        try
        {
            session?.Dispose();
        }
        finally
        {
            _disposed = true;
        }
    }
}
