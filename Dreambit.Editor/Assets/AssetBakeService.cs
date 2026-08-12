using System.Diagnostics;
using Dreambit.Editor.Projects;
using DreambitEngine.AssetBaker.Pipeline;

namespace Dreambit.Editor.Assets;

internal enum AssetBakeState
{
    Idle,
    Waiting,
    Baking,
    Succeeded,
    Failed
}

internal sealed record AssetBakeStatus(
    AssetBakeState State,
    string Message,
    DateTimeOffset? CompletedUtc = null,
    string? OutputPak = null);

internal enum AssetBakeMessageSeverity
{
    Information,
    Warning,
    Error
}

internal sealed record AssetBakeMessage(
    AssetBakeMessageSeverity Severity,
    string Message,
    Exception? Exception = null);

internal sealed class AssetBakeService : IDisposable
{
    private static readonly TimeSpan AutomaticBakeDelay = TimeSpan.FromMilliseconds(750);

    private readonly DreambitProjectDefinition _project;
    private readonly AssetDatabase _assets;
    private readonly Action<AssetBakeMessage>? _report;
    private readonly CancellationTokenSource _lifetime;
    private readonly AssetBakePipeline _pipeline = new();
    private Task<AssetBakeResult>? _activeBake;
    private AssetBakeStatus _status = new(AssetBakeState.Idle, "Assets are up to date.");
    private long _lastAssetVersion;
    private long _requestedAt;
    private bool _bakePending;
    private bool _rebuildAll;
    private bool _disposed;

    public AssetBakeService(
        DreambitProjectDefinition project,
        AssetDatabase assets,
        CancellationToken projectLifetime,
        Action<AssetBakeMessage>? report = null)
    {
        _project = project;
        _assets = assets;
        _report = report;
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(projectLifetime);
        _lastAssetVersion = assets.GetSnapshot().Version;
        OutputPakPath = Path.Combine(
            project.RootDirectory,
            ".cache",
            "dreambit",
            "content.pak");
        CacheDirectory = Path.Combine(project.RootDirectory, ".cache", "dreambit", "bake");
        if (!File.Exists(OutputPakPath) ||
            !AssetBakePipeline.HasCurrentBuiltInContent(CacheDirectory))
            RequestBake(rebuildAll: false);
    }

    public string OutputPakPath { get; }
    public string CacheDirectory { get; }
    public AssetBakeStatus Status => _status;
    public bool IsRunning => _activeBake is not null;
    public event Action<AssetBakeResult>? BakeCompleted;

    public void Update()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        CompleteFinishedBake();

        var assetVersion = _assets.GetSnapshot().Version;
        if (assetVersion != _lastAssetVersion)
        {
            _lastAssetVersion = assetVersion;
            RequestBake(rebuildAll: false);
        }

        if (_activeBake is not null || !_bakePending ||
            Stopwatch.GetElapsedTime(_requestedAt) < AutomaticBakeDelay)
        {
            return;
        }

        StartBake();
    }

    public void RequestBake(bool rebuildAll)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _rebuildAll |= rebuildAll;
        _bakePending = true;
        _requestedAt = rebuildAll
            ? Stopwatch.GetTimestamp() - (long)(AutomaticBakeDelay.TotalSeconds * Stopwatch.Frequency)
            : Stopwatch.GetTimestamp();
        if (_activeBake is null)
            _status = new AssetBakeStatus(
                AssetBakeState.Waiting,
                rebuildAll ? "Full asset rebuild queued." : "Asset bake queued.");
    }

    private void StartBake()
    {
        var rebuildAll = _rebuildAll;
        _bakePending = false;
        _rebuildAll = false;
        _status = new AssetBakeStatus(
            AssetBakeState.Baking,
            rebuildAll ? "Rebuilding all assets..." : "Baking changed assets...");
        _report?.Invoke(new AssetBakeMessage(
            AssetBakeMessageSeverity.Information,
            rebuildAll ? "Rebuilding all source assets." : "Baking changed source assets."));

        var progress = new InlineProgress<AssetBakeProgress>(value =>
        {
            if (value.Stage is "Bake" or "Write" or "Complete")
                _report?.Invoke(new AssetBakeMessage(
                    AssetBakeMessageSeverity.Information,
                    value.Message));
        });
        _activeBake = _pipeline.BakePakAsync(
            new AssetBakeRequest(
                _project.ContentRootPath,
                OutputPakPath,
                _assets.RegistryPath,
                CacheDirectory,
                rebuildAll,
                MarkSrgb: true,
                TargetPlatform: _project.Metadata.TargetRenderer,
                IncludeBuiltInContent: true),
            progress,
            _lifetime.Token);
    }

    private void CompleteFinishedBake()
    {
        if (_activeBake is not { IsCompleted: true } task)
            return;

        _activeBake = null;
        try
        {
            var result = task.GetAwaiter().GetResult();
            _status = new AssetBakeStatus(
                AssetBakeState.Succeeded,
                $"{result.BakedCount} baked, {result.CacheHitCount} cached in " +
                $"{result.Duration.TotalSeconds:0.00}s.",
                DateTimeOffset.UtcNow,
                result.OutputPak);
            _report?.Invoke(new AssetBakeMessage(
                AssetBakeMessageSeverity.Information,
                $"Asset bake complete: {_status.Message}"));
            BakeCompleted?.Invoke(result);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            _status = new AssetBakeStatus(AssetBakeState.Idle, "Asset bake canceled.");
        }
        catch (Exception exception)
        {
            _status = new AssetBakeStatus(
                AssetBakeState.Failed,
                $"Asset bake failed: {exception.Message}",
                DateTimeOffset.UtcNow);
            _report?.Invoke(new AssetBakeMessage(
                AssetBakeMessageSeverity.Error,
                _status.Message,
                exception));
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _lifetime.Cancel();
        if (_activeBake is not null)
        {
            _ = _activeBake.ContinueWith(
                static task => _ = task.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted |
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
        _lifetime.Dispose();
        _disposed = true;
    }

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
