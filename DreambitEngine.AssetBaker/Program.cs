using DreambitEngine.AssetBaker.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(cfg =>
{
    cfg.SetApplicationName("assetbaker");
    cfg.SetApplicationVersion("1.0.0");

    cfg.AddCommand<BakeAssetCommand>("bake-asset")
        .WithDescription("Bake one asset")
        .WithExample(new[] { "bake-asset", "input.png", "output.texb" });

    cfg.AddCommand<BakeDirectoryCommand>("bake-directory")
        .WithDescription("Bake every supported asset in a directory")
        .WithExample(new[] { "bake-directory", "/Content", "/Build/Content" });

    cfg.AddCommand<BakeFolderCommand>("bake-folder")
        .WithDescription("Alias for baking a folder recursively")
        .WithExample(new[] { "bake-folder", "/Content", "/Build/Content" });

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
