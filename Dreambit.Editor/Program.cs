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
            return 1;
        }
    }
}
