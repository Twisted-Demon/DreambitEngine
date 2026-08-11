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
        if (string.IsNullOrWhiteSpace(InputFolder) || !Directory.Exists(InputFolder))
            return ValidationResult.Error("Input folder does not exist.");
        if (string.IsNullOrWhiteSpace(Output))
            return ValidationResult.Error("Output is required.");
        return ValidationResult.Success();
    }
}

public class BakeFolderCommand : Command<BakeFolderSettings>
{
    protected override int Execute(CommandContext context, BakeFolderSettings settings, CancellationToken cancellationToken)
    {
        var registry = AssetBakerRegistry.CreateDefault();
        var folderPath = Path.GetFullPath(settings.InputFolder);
        var outputRoot = Path.GetFullPath(settings.Output);
        Directory.CreateDirectory(outputRoot);
        var filesPaths = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
        var bakedCount = 0;

        foreach (var filePath in filesPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            var baker = registry.GetByExt(ext);
            if (baker is null)
                continue;

            var relativePath = Path.GetRelativePath(folderPath, filePath);
            var outputDirectory = Path.Combine(outputRoot, Path.GetDirectoryName(relativePath) ?? "");
            var outputPath = Path.Combine(
                outputDirectory,
                Path.GetFileNameWithoutExtension(relativePath) + baker.OutputExtension);
            Directory.CreateDirectory(outputDirectory);

            AnsiConsole.MarkupLine(
                $"[grey]Baking[/] [bold]{Markup.Escape(baker.AssetTypeName)}[/] from " +
                $"[blue]{Markup.Escape(filePath)}[/] → [green]{Markup.Escape(outputPath)}[/]");

            var ctx = new BakeContext()
            {
                InputPath = filePath,
                OutputPath = outputPath,
                GenerateMips = settings.GenerateMips,
                PremultiplyAlpha = settings.PremultiplyAlpha,
                MaxDimension = settings.MaxSize,
                MarkSRgb = settings.SRgb
            };

            baker.Bake(ctx);
            bakedCount++;
        }

        AnsiConsole.MarkupLine($"[green]Baked[/] {bakedCount} supported asset(s).");

        return 0;
    }
}
