using Dreambit.Editor.Infrastructure;
using Dreambit.Editor.Projects;

namespace Dreambit.Editor;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (!EditorLaunchOptions.TryParse(args, out var options, out var error))
        {
            Console.Error.WriteLine(error);
            return 2;
        }

        try
        {
            using var game = new DreambitEditorGame(options);
            game.Run();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("Dreambit Editor terminated unexpectedly.");
            Console.Error.WriteLine(exception);
            var crashLogPath = TryWriteCrashReport(options, exception);
            if (crashLogPath is not null)
                Console.Error.WriteLine($"Crash details were written to '{crashLogPath}'.");
            return 1;
        }
    }

    private static string? TryWriteCrashReport(
        EditorLaunchOptions options,
        Exception exception)
    {
        try
        {
            var path = EditorPaths.Create(options).CrashLogPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var report = $"""

                [{DateTimeOffset.UtcNow:O}]
                Dreambit Editor SDK {DreambitSdkConstants.CurrentVersion}
                Project: {options.ProjectPath ?? "(project hub)"}
                {exception}
                """;
            File.AppendAllText(path, report + Environment.NewLine);
            return path;
        }
        catch (Exception writeException) when (
            writeException is IOException or UnauthorizedAccessException or
                ArgumentException or NotSupportedException)
        {
            return null;
        }
    }
}
