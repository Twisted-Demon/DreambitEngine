using System.Diagnostics;
using System.Xml.Linq;
using Dreambit.Editor.Projects;

namespace Dreambit.Editor.Compilation;

internal sealed class GameCodeService : IDisposable
{
    private static readonly TimeSpan BuildDebounce = TimeSpan.FromMilliseconds(800);
    private static readonly HashSet<string> WatchedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".csproj", ".props", ".targets", ".editorconfig"
    };

    private readonly DreambitProjectDefinition _project;
    private readonly IProcessRunner _processRunner;
    private readonly Action<GameCodeMessage>? _report;
    private readonly CancellationTokenSource _lifetime;
    private readonly List<string> _sourceRoots;
    private readonly List<FileSystemWatcher> _sourceWatchers = [];
    private Dictionary<string, SourceFileStamp> _sourceFiles;
    private readonly object _watcherSync = new();
    private Task<GameBuildResult>? _activeBuild;
    private GameBuildStatus _status = new(GameBuildState.Idle, "Game code is up to date.");
    private long _requestedAt;
    private bool _buildPending;
    private bool _rebuild;
    private bool _disposed;
    private long _lastSourcePoll;

    public GameCodeService(
        DreambitProjectDefinition project,
        CancellationToken projectLifetime,
        Action<GameCodeMessage>? report = null,
        IProcessRunner? processRunner = null)
    {
        _project = project;
        _report = report;
        _processRunner = processRunner ?? new ProcessRunner();
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(projectLifetime);
        Assemblies = new GameAssemblyLoadService(project.RootDirectory, report);
        _sourceRoots = DiscoverSourceRoots(project);
        _sourceFiles = CaptureSourceFiles(_sourceRoots);
        foreach (var sourceRoot in _sourceRoots)
        {
            var watcher = new FileSystemWatcher(sourceRoot)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName |
                               NotifyFilters.DirectoryName |
                               NotifyFilters.LastWrite |
                               NotifyFilters.Size,
                EnableRaisingEvents = false
            };
            watcher.Created += OnSourceChanged;
            watcher.Changed += OnSourceChanged;
            watcher.Deleted += OnSourceChanged;
            watcher.Renamed += OnSourceRenamed;
            watcher.Error += OnWatcherError;
            watcher.EnableRaisingEvents = true;
            _sourceWatchers.Add(watcher);
        }
        RequestBuild(rebuild: false, immediate: true);
    }

    public GameAssemblyLoadService Assemblies { get; }
    public GameBuildStatus Status => _status;
    public bool IsRunning => _activeBuild is not null;

    public void Update()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        CompleteFinishedBuild();
        PollSourceChanges();
        lock (_watcherSync)
        {
            if (_activeBuild is not null || !_buildPending ||
                Stopwatch.GetElapsedTime(_requestedAt) < BuildDebounce)
            {
                return;
            }
            StartBuild();
        }
    }

    public void RequestBuild(bool rebuild, bool immediate = false)
    {
        if (_disposed)
            return;
        lock (_watcherSync)
        {
            _rebuild |= rebuild;
            _buildPending = true;
            _requestedAt = immediate
                ? Stopwatch.GetTimestamp() - (long)(BuildDebounce.TotalSeconds * Stopwatch.Frequency)
                : Stopwatch.GetTimestamp();
            if (_activeBuild is null)
                _status = new GameBuildStatus(
                    GameBuildState.Waiting,
                    rebuild ? "Game rebuild queued." : "Game build queued.");
        }
    }

    private void StartBuild()
    {
        var rebuild = _rebuild;
        _rebuild = false;
        _buildPending = false;
        _status = new GameBuildStatus(
            GameBuildState.Building,
            rebuild ? "Rebuilding game code..." : "Building game code...");
        _report?.Invoke(new GameCodeMessage(
            GameCodeMessageSeverity.Information,
            _status.Message));
        _activeBuild = BuildAsync(rebuild, _lifetime.Token);
    }

    private async Task<GameBuildResult> BuildAsync(
        bool rebuild,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var arguments = new List<string>
        {
            "build",
            _project.GameProjectPath,
            "-c",
            "Debug",
            "--nologo",
            "--no-restore"
        };
        if (rebuild)
            arguments.Add("-t:Rebuild");
        var result = await _processRunner.RunAsync(
                new ProcessCommand("dotnet", arguments, _project.RootDirectory),
                LogBuildLine,
                cancellationToken)
            .ConfigureAwait(false);
        stopwatch.Stop();
        var diagnostics = GameBuildDiagnosticParser.Parse(result.Output);
        var assemblyPath = result.Succeeded ? FindBuiltAssembly() : null;
        return new GameBuildResult(
            result.Succeeded && assemblyPath is not null,
            assemblyPath,
            result.Output,
            diagnostics,
            stopwatch.Elapsed);
    }

    private void CompleteFinishedBuild()
    {
        if (_activeBuild is not { IsCompleted: true } task)
            return;
        _activeBuild = null;
        try
        {
            var result = task.GetAwaiter().GetResult();
            if (!result.Succeeded || result.AssemblyPath is null)
            {
                _status = new GameBuildStatus(
                    GameBuildState.Failed,
                    "Game build failed. The last-known-good assembly remains loaded.",
                    DateTimeOffset.UtcNow,
                    result.Diagnostics);
                _report?.Invoke(new GameCodeMessage(
                    GameCodeMessageSeverity.Error,
                    _status.Message));
                return;
            }

            if (!Assemblies.TryLoad(result.AssemblyPath, out var loadError))
            {
                _status = new GameBuildStatus(
                    GameBuildState.Failed,
                    loadError ?? "The built game assembly could not be loaded.",
                    DateTimeOffset.UtcNow,
                    result.Diagnostics);
                return;
            }

            _status = new GameBuildStatus(
                GameBuildState.Succeeded,
                $"Game code built and loaded in {result.Duration.TotalSeconds:0.00}s.",
                DateTimeOffset.UtcNow,
                result.Diagnostics);
            _report?.Invoke(new GameCodeMessage(
                GameCodeMessageSeverity.Information,
                _status.Message));
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            _status = new GameBuildStatus(GameBuildState.Idle, "Game build canceled.");
        }
        catch (Exception exception)
        {
            _status = new GameBuildStatus(
                GameBuildState.Failed,
                $"Game build failed unexpectedly: {exception.Message}",
                DateTimeOffset.UtcNow);
            _report?.Invoke(new GameCodeMessage(
                GameCodeMessageSeverity.Error,
                _status.Message,
                exception));
        }
    }

    private string? FindBuiltAssembly()
    {
        var projectDirectory = Path.GetDirectoryName(_project.GameProjectPath)!;
        var assemblyName = Path.GetFileNameWithoutExtension(_project.GameProjectPath);
        try
        {
            var document = XDocument.Load(_project.GameProjectPath);
            assemblyName = document.Descendants("AssemblyName")
                               .Select(element => element.Value.Trim())
                               .FirstOrDefault(value => value.Length > 0)
                           ?? assemblyName;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }

        var binDirectory = Path.Combine(projectDirectory, "bin", "Debug");
        if (!Directory.Exists(binDirectory))
            return null;
        return Directory.EnumerateFiles(
                binDirectory,
                assemblyName + ".dll",
                SearchOption.AllDirectories)
            .Where(path => !path.Contains(
                Path.DirectorySeparatorChar + "ref" + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private void LogBuildLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;
        var severity = line.Contains(": error ", StringComparison.OrdinalIgnoreCase)
            ? GameCodeMessageSeverity.Error
            : line.Contains(": warning ", StringComparison.OrdinalIgnoreCase)
                ? GameCodeMessageSeverity.Warning
                : GameCodeMessageSeverity.Information;
        _report?.Invoke(new GameCodeMessage(severity, line));
    }

    private void OnSourceChanged(object sender, FileSystemEventArgs args)
    {
        if (ShouldBuild(args.FullPath))
            RequestBuild(rebuild: false);
    }

    private void OnSourceRenamed(object sender, RenamedEventArgs args)
    {
        if (ShouldBuild(args.FullPath) || ShouldBuild(args.OldFullPath))
            RequestBuild(rebuild: false);
    }

    private void OnWatcherError(object sender, ErrorEventArgs args)
    {
        _report?.Invoke(new GameCodeMessage(
            GameCodeMessageSeverity.Warning,
            "The game source watcher lost events; a rebuild was scheduled.",
            args.GetException()));
        RequestBuild(rebuild: true);
    }

    private void PollSourceChanges()
    {
        if (Stopwatch.GetElapsedTime(_lastSourcePoll) < TimeSpan.FromSeconds(1))
            return;
        _lastSourcePoll = Stopwatch.GetTimestamp();
        var current = CaptureSourceFiles(_sourceRoots);
        if (!SourceFilesEqual(_sourceFiles, current))
            RequestBuild(rebuild: false);
        _sourceFiles = current;
    }

    private static List<string> DiscoverSourceRoots(DreambitProjectDefinition project)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var projectPrefix = Path.TrimEndingDirectorySeparator(project.RootDirectory) +
                            Path.DirectorySeparatorChar;
        var projects = new HashSet<string>(comparer);
        var roots = new HashSet<string>(comparer);
        var pending = new Queue<string>();
        pending.Enqueue(project.GameProjectPath);

        while (pending.Count > 0)
        {
            var projectPath = Path.GetFullPath(pending.Dequeue());
            if (!projects.Add(projectPath) || !File.Exists(projectPath))
                continue;
            var directory = Path.GetDirectoryName(projectPath)!;
            roots.Add(directory);
            try
            {
                var document = XDocument.Load(projectPath);
                foreach (var include in document.Descendants("ProjectReference")
                             .Select(element => element.Attribute("Include")?.Value)
                             .Where(value => !string.IsNullOrWhiteSpace(value) &&
                                             !value.Contains("$(", StringComparison.Ordinal)))
                {
                    var referenced = Path.GetFullPath(Path.Combine(directory, include!));
                    if (referenced.StartsWith(projectPrefix, comparison))
                        pending.Enqueue(referenced);
                }
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or System.Xml.XmlException)
            {
            }
        }

        return roots.OrderBy(path => path, comparer).ToList();
    }

    private static Dictionary<string, SourceFileStamp> CaptureSourceFiles(IEnumerable<string> roots)
    {
        var comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var files = new Dictionary<string, SourceFileStamp>(comparer);
        foreach (var root in roots)
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    if (!ShouldBuild(path))
                        continue;
                    var info = new FileInfo(path);
                    files[path] = new SourceFileStamp(info.LastWriteTimeUtc.Ticks, info.Length);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
            }
        }
        return files;
    }

    private static bool SourceFilesEqual(
        IReadOnlyDictionary<string, SourceFileStamp> left,
        IReadOnlyDictionary<string, SourceFileStamp> right)
    {
        if (left.Count != right.Count)
            return false;
        foreach (var (path, stamp) in left)
            if (!right.TryGetValue(path, out var current) || current != stamp)
                return false;
        return true;
    }

    private static bool ShouldBuild(string path)
    {
        if (!WatchedExtensions.Contains(Path.GetExtension(path)))
            return false;
        var normalized = path.Replace('\\', '/');
        return !normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase) &&
               !normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        foreach (var watcher in _sourceWatchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Created -= OnSourceChanged;
            watcher.Changed -= OnSourceChanged;
            watcher.Deleted -= OnSourceChanged;
            watcher.Renamed -= OnSourceRenamed;
            watcher.Error -= OnWatcherError;
            watcher.Dispose();
        }
        _sourceWatchers.Clear();
        _lifetime.Cancel();
        if (_activeBuild is not null)
        {
            _ = _activeBuild.ContinueWith(
                static task => _ = task.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted |
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
        Assemblies.Dispose();
        _lifetime.Dispose();
        _disposed = true;
    }

    private readonly record struct SourceFileStamp(long LastWriteTicks, long Length);
}
