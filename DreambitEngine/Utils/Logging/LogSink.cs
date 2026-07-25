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

    private static void SpectreWrite(
        LogLevel level,
        string prefix,
        string msg,
        object[]? args)
    {
        var (header, body) = level switch
        {
            LogLevel.Trace => (
                "[dim grey58]",
                "[dim grey58]"),

            LogLevel.Debug => (
                "[bold mediumpurple1]",
                "[grey74]"),

            LogLevel.Info => (
                "[bold turquoise2]",
                "[grey93]"),

            LogLevel.Warn => (
                "[bold orange1]",
                "[wheat1]"),

            LogLevel.Error => (
                "[bold indianred1]",
                "[mistyrose1]"),

            _ => (
                "[bold deepskyblue1]",
                "[grey74]")
        };

        var line =
            $"{header}{Markup.Escape(prefix)}:[/] " +
            $"{body}{Markup.Escape(msg)}[/]";

        if (args is { Length: > 0 })
        {
            AnsiConsole.MarkupLine(line, args);
        }
        else
        {
            AnsiConsole.MarkupLine(line);
        }
    }
}