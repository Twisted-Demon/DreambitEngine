using DreambitEngine.AssetBaker.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

if (args is ["__compile-effect", var input, var output, var logicalRoot, var platform])
{
    try
    {
        var blob = new DreambitEngine.AssetBaker.Pipeline.Effects.EffectBaker().BakeToBytes(
            new DreambitEngine.AssetBaker.Abstractions.BakeContext
            {
                InputPath = input,
                OutputPath = output,
                LogicalRoot = logicalRoot,
                TargetPlatform = platform
            });
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output, blob.Data);
        return 0;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception);
        return -1;
    }
}

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

    cfg.AddCommand<BakeBlobsCommand>("bake-blobs")
        .WithDescription("Bake assets into the incremental development blob store")
        .WithExample(new[] { "bake-blobs", "/Content", "/Cache/bake" });

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
