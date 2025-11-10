using System.Runtime.CompilerServices;

namespace Dreambit;

public class Logger<T> : ILogger where T : class
{
    private readonly string _prefix = typeof(T).Name;

    public LogLevel Level { get; set; } = LogLevel.None;

    public void Trace(string format, params object[] args)
    {
        if (!Enabled(LogLevel.Trace)) return;
        LogSink.Enqueue(new LogEntry(LogLevel.Trace, _prefix, format, args));
    }

    public void Trace(string message)
    {
        if (!Enabled(LogLevel.Trace)) return;
        LogSink.Enqueue(new LogEntry(LogLevel.Trace, _prefix, message, null));
    }

    public void Info(string format, params object[] args)
    {
        if (!Enabled(LogLevel.Info)) return;
        LogSink.Enqueue(new LogEntry(LogLevel.Info, _prefix, format, args));
    }

    public void Info(string message)
    {
        if (!Enabled(LogLevel.Info)) return;
        LogSink.Enqueue(new LogEntry(LogLevel.Info, _prefix, message, null));
    }

    public void Debug(string format, params object[] args)
    {
        if (!Enabled(LogLevel.Debug)) return;
        LogSink.Enqueue(new LogEntry(LogLevel.Debug, _prefix, format, args));
    }

    public void Debug(string message)
    {
        if (!Enabled(LogLevel.Debug)) return;
        LogSink.Enqueue(new LogEntry(LogLevel.Debug, _prefix, message, null));
    }

    public void Warn(string format, params object[] args)
    {
        if (!Enabled(LogLevel.Warn)) return;
        LogSink.Enqueue(new LogEntry(LogLevel.Warn, _prefix, format, args));
    }

    public void Warn(string message)
    {
        if (!Enabled(LogLevel.Warn)) return;
        LogSink.Enqueue(new LogEntry(LogLevel.Warn, _prefix, message, null));
    }

    public void Error(string format, params object[] args)
    {
        if (!Enabled(LogLevel.Error)) return;
        LogSink.Enqueue(new LogEntry(LogLevel.Error, _prefix, format, args));
    }

    public void Error(string message)
    {
        if (!Enabled(LogLevel.Error)) return;
        LogSink.Enqueue(new LogEntry(LogLevel.Error, _prefix, message, null));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Enabled(LogLevel check)
    {
        var level = Core.Level;
        if (Level != LogLevel.None) level = Level;
        return level <= check;
    }
}