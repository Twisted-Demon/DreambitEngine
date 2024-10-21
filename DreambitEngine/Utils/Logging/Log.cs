using Spectre.Console;

namespace Dreambit;

internal static class Log
{
    public static void Trace(string header, string format, params object[] args)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[white]{header}: [/][grey69]{format}[/]", args);
    }

    public static void Trace(string header, string message)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[white]{header}: [/][grey69]{message}[/]");
    }


    public static void Debug(string header, string format, params object[] args)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[white]{header}: [/][cyan]{format}[/]", args);
    }

    public static void Debug(string header, string message)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[white]{header}: [/][cyan]{message}[/]");
    }


    public static void Info(string header, string format, params object[] args)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[white]{header}: [/][deepskyblue1]{format}[/]", args);
    }

    public static void Info(string header, string message)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[white]{header}: [/][deepskyblue1]{message}[/]");
    }

    public static void Warn(string header, string format, params object[] args)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[white]{header}: [/][orange1]{format}[/]", args);
    }

    public static void Warn(string header, string message)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[white]{header}: [/][orange1]{message}[/]");
    }


    public static void Error(string header, string format, params object[] args)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[white]{header}: [/][orange1]{format}[/]", args);
    }

    public static void Error(string header, string message)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[white]{header}: [/][orange1]{message}[/]");
    }
}