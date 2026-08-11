namespace Dreambit.Editor.Logging;

internal sealed class EditorLogService
{
    private const int DefaultCapacity = 2_000;

    private readonly object _sync = new();
    private readonly int _capacity;
    private readonly List<EditorLogEntry> _entries;
    private EditorLogEntry[] _snapshot = [];
    private long _version;

    public EditorLogService(int capacity = DefaultCapacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _capacity = capacity;
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

            _entries.Add(new EditorLogEntry(
                DateTimeOffset.Now,
                severity,
                category,
                message,
                details));
            PublishSnapshot();
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
}
