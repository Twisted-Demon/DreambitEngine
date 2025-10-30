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
    public override int Execute(CommandContext context, BakeAssetSettings s)
    {
        var registry = new AssetBakerRegistry()
            .Register(AssetType.Texture, new TextureBaker())
            .Register(AssetType.Json, new JsonbBaker());
        
        var ext = Path.GetExtension(s.Input).ToLowerInvariant();

        var baker = registry.GetByExt(ext);

        AnsiConsole.MarkupLine($"[grey]Baking[/] [bold]{baker.AssetTypeName}[/] from [blue]{s.Input}[/] → [green]{s.Output}[/]");

        var ctx = new BakeContext
        {
            InputPath = s.Input,
            OutputPath = s.Output,
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


