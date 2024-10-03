using Spectre.Console;

namespace PixelariaEngine;

public static class Log
{
    public static void Trace(string format, object[] args)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[grey69]{format}[/]", args);
    }
    
    public static void Trace(string message)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[grey69]{message}[/]");
    }
    
    public static void Debug(string format, object[] args)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[darkcyan]{format}[/]", args);
    }
    
    public static void Debug(string message)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[darkcyan]{message}[/]");
    }
    
    public static void Info(string format, object[] args)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[deepskyblue1]{format}[/]", args);
    }
    
    public static void Info(string message)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[deepskyblue1]{message}[/]");
    }
    
    public static void Warn(string format, object[] args)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[orange1]{format}[/]", args);
    }
    
    public static void Warn(string message)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[orange1]{message}[/]");
    }
    
    public static void Error(string format, object[] args)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[red1]{format}[/]", args);
    }
    
    public static void Error(string message)
    {
        AnsiConsole.ResetColors();
        AnsiConsole.MarkupLine($"[red1]{message}[/]");
    }
}