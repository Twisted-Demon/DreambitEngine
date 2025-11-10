using DreambitEngine.AssetBaker.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(cfg =>
{
    cfg.SetApplicationName("asset baker");
    cfg.SetApplicationVersion("1.0.0");

    cfg.AddCommand<BakeAssetCommand>("bake")
        .WithDescription("Bake an input asset");
    
    cfg.AddCommand<BakeDirectoryCommand>("bake-dir")
        .WithDescription("Bake an input asset");
    
    cfg.AddCommand<BakePakCommand>("bake-pak")
        .WithDescription("Bake an input asset");
});

try
{
    return app.Run(args);

}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes);
    return -1;
}