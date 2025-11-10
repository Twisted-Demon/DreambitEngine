using System.ComponentModel;
using DreambitEngine.AssetBaker.Abstractions;
using DreambitEngine.AssetBaker.Core;
using DreambitEngine.AssetBaker.Pipeline.Textures;
using Spectre.Console;
using Spectre.Console.Cli;


namespace DreambitEngine.AssetBaker.Commands;

public sealed class BakeDirectorySettings : CommandSettings
{
    [CommandArgument(0, "<INPUT_DIR>")]
    [Description("Folder to scan (recursively).")]
    public string InputDir { get; set; } = default!;

    [CommandArgument(1, "<OUTPUT_DIR>")]
    [Description("Root folder to write baked outputs (directory structure is mirrored).")]
    public string OutputDir { get; set; } = default!;

    [CommandOption("--parallel <N>")]
    [Description("Max degree of parallelism (default: 1).")]
    public int Parallel { get; set; } = 1;

    // Texture-oriented options passed to TextureBaker
    [CommandOption("--mips")]
    [Description("Generate full mip chain (texture files).")]
    public bool GenerateMips { get; set; }

    [CommandOption("--premul")]
    [Description("Premultiply alpha (texture files).")]
    public bool PremultiplyAlpha { get; set; }

    [CommandOption("--max-size <N>")]
    [Description("Clamp largest dimension (texture files).")]
    public int? MaxSize { get; set; }

    [CommandOption("--srgb")]
    [Description("Mark as sRGB (texture files).")]
    public bool SRgb { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(InputDir) || !Directory.Exists(InputDir))
            return ValidationResult.Error("INPUT_DIR does not exist.");
        if (string.IsNullOrWhiteSpace(OutputDir))
            return ValidationResult.Error("OUTPUT_DIR is required.");
        if (Parallel <= 0) return ValidationResult.Error("--parallel must be >= 1");
        return ValidationResult.Success();
    }
}

public sealed class BakeDirectoryCommand : Command<BakeDirectorySettings>
{
    public override int Execute(CommandContext context, BakeDirectorySettings s)
    {
        var inputRoot = Path.GetFullPath(s.InputDir);
        var outputRoot = Path.GetFullPath(s.OutputDir);
        Directory.CreateDirectory(outputRoot);

        var registry = new AssetBakerRegistry()
            .Register(AssetType.Texture, new TextureBaker());
        
        var files = Directory.EnumerateFiles(inputRoot, "*", SearchOption.AllDirectories).ToArray();
        if (files.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No files found.[/]");
            return 0;
        }
        
        AnsiConsole.MarkupLine($"[grey]Found[/] {files.Length} file(s).");

        foreach (var file in files)
        {
            var baker = registry.GetByExt(Path.GetExtension(file));

            var rel = Path.GetRelativePath(inputRoot, file);
            var relNoExt = Path.Combine(Path.GetDirectoryName(rel) ?? "", Path.GetFileNameWithoutExtension(rel));
            var outDir = Path.Combine(outputRoot, Path.GetDirectoryName(rel) ?? "");
            var outFile = Path.Combine(outDir, relNoExt + baker.OutputExtension);
            Directory.CreateDirectory(outDir);

            var ctxBake = new BakeContext
            {
                InputPath = file,
                OutputPath = outFile,
                GenerateMips = s.GenerateMips,
                PremultiplyAlpha = s.PremultiplyAlpha,
                MaxDimension = s.MaxSize,
                MarkSRgb = s.SRgb
            };

            baker.Bake(ctxBake);
        }

        return 1;
    }
}