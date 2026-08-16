namespace Dreambit.Editor.Logging;

internal sealed class EditorLogService
{
    private const int DefaultCapacity = 2_000;

    private readonly object _sync = new();
    private readonly int _capacity;
    private readonly List<EditorLogEntry> _entries;
    private readonly string? _errorLogPath;
    private EditorLogEntry[] _snapshot = [];
    private long _version;
    private bool _reportedPersistenceFailure;

    public EditorLogService(int capacity = DefaultCapacity, string? errorLogPath = null)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _capacity = capacity;
        _errorLogPath = string.IsNullOrWhiteSpace(errorLogPath)
            ? null
            : Path.GetFullPath(errorLogPath);
        _entries = new List<EditorLogEntry>(Math.Min(capacity, DefaultCapacity));
    }

    public void Trace(string category, string message) =>
        Write(EditorLogSeverity.Trace, category, message);

    public void Info(string category, string message) =>
        Write(EditorLogSeverity.Information, category, message);

    public void Warning(string category, string message) =>
        Write(EditorLogSeverity.Warning, category, message);

    public void Error(string category, string message, Exception? exception = null) =>
        Write(
            EditorLogSeverity.Error,
            category,
            message,
            exception?.ToString());

    public void Write(
        EditorLogSeverity severity,
        string category,
        string message,
        string? details = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        lock (_sync)
        {
            if (_entries.Count == _capacity)
                _entries.RemoveAt(0);

            var entry = new EditorLogEntry(
                DateTimeOffset.Now,
                severity,
                category,
                message,
                details);
            _entries.Add(entry);
            PublishSnapshot();

            if (severity == EditorLogSeverity.Error)
                TryPersistError(entry);
        }
    }

    public EditorLogSnapshot GetSnapshot()
    {
        lock (_sync)
            return new EditorLogSnapshot(_version, _snapshot);
    }

    public void Clear()
    {
        lock (_sync)
        {
            _entries.Clear();
            PublishSnapshot();
        }
    }

    private void PublishSnapshot()
    {
        _version++;
        _snapshot = _entries.ToArray();
    }

    private void TryPersistError(EditorLogEntry entry)
    {
        if (_errorLogPath is null)
            return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_errorLogPath)!);
            var report = $"[{entry.Timestamp:O}] ERROR [{entry.Category}] {entry.Message}";
            if (!string.IsNullOrWhiteSpace(entry.Details))
                report += Environment.NewLine + entry.Details;
            File.AppendAllText(_errorLogPath, report + Environment.NewLine + Environment.NewLine);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or
                ArgumentException or NotSupportedException)
        {
            if (_reportedPersistenceFailure)
                return;

            _reportedPersistenceFailure = true;
            Console.Error.WriteLine(
                $"Could not write the Dreambit Editor error log '{_errorLogPath}'. {exception.Message}");
        }
    }
}
