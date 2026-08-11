using System.ComponentModel;
using DreambitEngine.AssetBaker.Abstractions;
using DreambitEngine.AssetBaker.Core;
using DreambitEngine.AssetBaker.Pipeline.Docs;
using DreambitEngine.AssetBaker.Pipeline.Textures;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DreambitEngine.AssetBaker.Commands;

public sealed class BakeAssetSettings : CommandSettings
{
    [CommandArgument(0, "<INPUT>")]
    [Description("Input file")]
    public string Input { get; set; } = default!;

    [CommandArgument(1, "<OUTPUT>")]
    [Description("Output file")]
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
        if (string.IsNullOrWhiteSpace(Input) || !File.Exists(Input))
            return ValidationResult.Error("Input file does not exist.");
        if (string.IsNullOrWhiteSpace(Output))
            return ValidationResult.Error("Output is required.");
        return ValidationResult.Success();
    }
}

public sealed class BakeAssetCommand : Command<BakeAssetSettings>
{
    protected override int Execute(CommandContext context, BakeAssetSettings s, CancellationToken cancellationToken)
    {
        var registry = AssetBakerRegistry.CreateDefault();
        
        var ext = Path.GetExtension(s.Input).ToLowerInvariant();

        var baker = registry.GetByExt(ext);
        if (baker is null)
        {
            AnsiConsole.MarkupLine($"[red]Unsupported asset extension:[/] {Markup.Escape(ext)}");
            return -1;
        }

        var outputPath = Path.GetFullPath(s.Output);
        if (string.IsNullOrWhiteSpace(Path.GetExtension(outputPath)))
            outputPath += baker.OutputExtension;

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (outputDirectory is not null)
            Directory.CreateDirectory(outputDirectory);

        AnsiConsole.MarkupLine(
            $"[grey]Baking[/] [bold]{Markup.Escape(baker.AssetTypeName)}[/] from " +
            $"[blue]{Markup.Escape(s.Input)}[/] → [green]{Markup.Escape(outputPath)}[/]");

        var ctx = new BakeContext
        {
            InputPath = s.Input,
            OutputPath = outputPath,
            GenerateMips = s.GenerateMips,
            PremultiplyAlpha = s.PremultiplyAlpha,
            MaxDimension = s.MaxSize,
            MarkSRgb = s.SRgb,
        };

        baker.Bake(ctx);

        AnsiConsole.MarkupLine("[green]Done.[/]");
        return 0;
    }
}


