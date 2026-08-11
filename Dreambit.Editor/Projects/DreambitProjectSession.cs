using Dreambit.Editor.Assets;

namespace Dreambit.Editor.Projects;

internal sealed class DreambitProjectSession : IDisposable
{
    private readonly ProjectInstanceLease _lease;
    private readonly CancellationTokenSource _lifetime = new();
    private bool _disposed;

    public DreambitProjectSession(
        DreambitProjectDefinition project,
        ProjectInstanceLease lease,
        Action<AssetDatabaseDiagnostic>? reportAssetDiagnostic = null)
    {
        Project = project;
        _lease = lease;
        Assets = new AssetDatabase(
            project.RootDirectory,
            project.ContentRootPath,
            reportAssetDiagnostic);
        Resources.AssetRegistry = Assets;
    }

    public DreambitProjectDefinition Project { get; }
    public AssetDatabase Assets { get; }
    public CancellationToken LifetimeToken => _lifetime.Token;
    public bool IsDisposed => _disposed;

    public void Dispose()
    {
        if (_disposed)
            return;

        _lifetime.Cancel();
        if (ReferenceEquals(Resources.AssetRegistry, Assets))
            Resources.AssetRegistry = null;
        Assets.Dispose();
        _lifetime.Dispose();
        _lease.Dispose();
        _disposed = true;
    }
}
