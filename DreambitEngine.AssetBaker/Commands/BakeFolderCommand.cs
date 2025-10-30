using System.ComponentModel;
using DreambitEngine.AssetBaker.Abstractions;
using DreambitEngine.AssetBaker.Core;
using DreambitEngine.AssetBaker.Pipeline.Docs;
using DreambitEngine.AssetBaker.Pipeline.Textures;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DreambitEngine.AssetBaker.Commands;

public sealed class BakeFolderSettings : CommandSettings
{
    [CommandArgument(0, "<INPUT_FOLDER>")]
    [Description("Input folder")]
    public string InputFolder { get; set; } = default!;

    [CommandArgument(1, "<OUTPUT>")]
    [Description("Output folder.")]
    public string Output { get; set; } = default!;
    
    // Texture options (safe defaults for other types)
    [CommandOption("--mips")]
    [Description("Generate full mip chain (texture-only).")]
    public bool GenerateMips { get; set; }

    [CommandOption("--premul")]
    [Description("Premultiply alpha (texture-only).")]
    public bool PremultiplyAlpha { get; set; }

    [CommandOption("--max-size <N>")]
    [Description("Clamp largest dimension before baking (texture-only).")]
    public int? MaxSize { get; set; }

    [CommandOption("--srgb")]
    [Description("Mark as sRGB (texture-only).")]
    public bool SRgb { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(InputFolder) || Directory.Exists(InputFolder))
            return ValidationResult.Error("Input folder does not exist.");
        if (string.IsNullOrWhiteSpace(Output))
            return ValidationResult.Error("Output is required.");
        return ValidationResult.Success();
    }
}

public class BakeFolderCommand : Command<BakeFolderSettings>
{
    public override int Execute(CommandContext context, BakeFolderSettings settings)
    {
        var registry = new AssetBakerRegistry()
            .Register(AssetType.Texture, new TextureBaker())
            .Register(AssetType.Json, new JsonbBaker());

        var baseDirectory = AppContext.BaseDirectory;
        var rel = settings.InputFolder.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        
        var folderPath = Path.Combine(AppContext.BaseDirectory, rel);
        
        var filesPaths = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

        foreach (var filePath in filesPaths)
        {
            AnsiConsole.MarkupLine(
                $"[grey]Baking[/] [bold]{"baker.AssetTypeName"}[/] from [blue]{filePath}[/] → " +
                $"[green]{settings.Output}[/]");
            
            var ext = Path.GetExtension(filePath).ToLowerInvariant();

            var baker = registry.GetByExt(ext);

            var outputPath = Path.Combine(AppContext.BaseDirectory, settings.Output);

            var ctx = new BakeContext()
            {
                InputPath = filePath,
                OutputPath = outputPath + Path.GetFileNameWithoutExtension(filePath),
                GenerateMips = settings.GenerateMips,
                PremultiplyAlpha = settings.PremultiplyAlpha,
                MaxDimension = settings.MaxSize,
                MarkSRgb = settings.SRgb
            };

            baker.Bake(ctx);

            AnsiConsole.MarkupLine("[green]Done.[/]");
        }

        return 0;
    }
}