using System.Threading.Channels;
using System.Threading.Tasks;
using Spectre.Console;

namespace Dreambit;

public static class LogSink
{
    private static readonly Channel<LogEntry> Channel =
        System.Threading.Channels.Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(2048)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

    static LogSink()
    {
        Task.Run(async () =>
        {
            var reader = Channel.Reader;
            while (await reader.WaitToReadAsync().ConfigureAwait(false))
            while (reader.TryRead(out var entry))
                if (entry.Args is { Length: > 0 })
                    SpectreWrite(entry.Level, entry.Prefix, entry.Message, entry.Args);
                else
                    SpectreWrite(entry.Level, entry.Prefix, entry.Message, null);
        });
    }

    public static void Enqueue(in LogEntry entry)
    {
        Channel.Writer.TryWrite(entry);
    }

    private static void SpectreWrite(LogLevel level, string prefix, string msg, object[]? args)
    {
        // choose color by level; cheap switch
        var (hdr, body) = level switch
        {
            LogLevel.Trace => ("[white]", "[grey69]"),
            LogLevel.Debug => ("[white]", "[cyan]"),
            LogLevel.Info => ("[white]", "[deepskyblue1]"),
            LogLevel.Warn => ("[white]", "[orange1]"),
            LogLevel.Error => ("[white]", "[red1]"),
            _ => ("[white]", "[grey70]")
        };
        var line = $"{hdr}{prefix}: [/]{body}{msg}[/]";
        if (args is null) AnsiConsole.MarkupLine(line);
        else AnsiConsole.MarkupLine(line, args);
    }
}