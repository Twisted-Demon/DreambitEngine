using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace Dreambit.AssetEditor;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var runSmokeTests = args.Any(arg => arg.Equals("--smoke-test", StringComparison.OrdinalIgnoreCase));
        var lifetime = new ClassicDesktopStyleApplicationLifetime
        {
            Args = args,
            ShutdownMode = ShutdownMode.OnMainWindowClose
        };

        AppBuilder.Configure<Application>()
            .UsePlatformDetect()
            .AfterSetup(builder =>
            {
                if (builder.Instance is not { } app)
                    return;

                app.Name = "Dreambit Asset Editor";
                app.Styles.Add(new FluentTheme());
                app.RequestedThemeVariant = ThemeVariant.Dark;
            })
            .SetupWithLifetime(lifetime);

        var mainWindow = new MainWindow();
        lifetime.MainWindow = mainWindow;

        if (runSmokeTests)
        {
            var started = false;
            mainWindow.Opened += async (_, _) =>
            {
                if (started)
                    return;
                started = true;

                var exitCode = 0;
                try
                {
                    var checks = await AssetEditorSmokeTests.RunAsync(mainWindow);
                    foreach (var check in checks)
                        Console.WriteLine($"PASS  {check}");
                    Console.WriteLine($"Asset Editor smoke test passed ({checks.Count} checks).");
                }
                catch (Exception exception)
                {
                    exitCode = 1;
                    Console.Error.WriteLine("Asset Editor smoke test failed:");
                    Console.Error.WriteLine(exception);
                }
                finally
                {
                    AssetEditorSmokeTests.PrepareForShutdown(mainWindow);
                    Environment.ExitCode = exitCode;
                    Dispatcher.UIThread.Post(() => lifetime.Shutdown(exitCode));
                }
            };
        }

        lifetime.Start(args);
    }
}
