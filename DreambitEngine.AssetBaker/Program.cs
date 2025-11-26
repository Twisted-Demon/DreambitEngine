using DreambitEngine.AssetBaker.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(cfg =>
{
    cfg.SetApplicationName("assetbaker");
    cfg.SetApplicationVersion("1.0.0");
    
    
    cfg.AddCommand<BakePakCommand>("bake-pak")
        .WithDescription("Bake assets into a pak file")
        .WithExample(new[] { "bake-pak", "/Content", "/Content/content.pak" });
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