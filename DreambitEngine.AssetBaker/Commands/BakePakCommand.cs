using System.ComponentModel;
using DreambitEngine.AssetBaker.Abstractions;
using DreambitEngine.AssetBaker.Core;
using DreambitEngine.AssetBaker.pipeline.Audio;
using DreambitEngine.AssetBaker.Pipeline.Docs;
using DreambitEngine.AssetBaker.Pipeline.Textures;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DreambitEngine.AssetBaker.Commands;

public class BakePakSettings : CommandSettings
{
    [CommandArgument(0, "<INPUT_DIR>")]
    [Description("Folder to scan (recursively)")]
    public string InputDir { get; set; } = default!;
    
    [CommandArgument(1, "<OUTPUT_PAK>")]
    [Description("Output PAK file")]
    public string OutputPak { get; set; } = default!;
    
    [CommandOption("--mips")] public bool GenerateMips { get; set; }
    [CommandOption("--premul")] public bool PremultiplyAlpha { get; set; }
    [CommandOption("--max-size <N>")] public int? MaxSize { get; set; }
    [CommandOption("--srgb")] public bool SRgb { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(InputDir) || !Directory.Exists(InputDir))
            return ValidationResult.Error("INPUT_DIR does not exist.");
        if (!Path.GetExtension(OutputPak).Equals(".pak", StringComparison.OrdinalIgnoreCase))
            return ValidationResult.Error("OUTPUT_PAK must end with .pak");
        
        return ValidationResult.Success();
    }
}

public sealed class BakePakCommand : Command<BakePakSettings>
{
    public override int Execute(CommandContext context, BakePakSettings settings)
    {
        var inputRoot = Path.GetFullPath(settings.InputDir);
        var files = Directory.EnumerateFiles(inputRoot, "*", new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.System
        }).ToArray();

        var registry = new AssetBakerRegistry()
            .Register(AssetType.Texture, new TextureBaker())
            .Register(AssetType.Json, new JsonbBaker())
            .Register(AssetType.Audio, new AudioBaker());

        var pak = new PakWriter();
        
        foreach (var file in files)
        {
            try
            {
                var ext = Path.GetExtension(file);
                var baker = registry.GetByExt(ext);

                var rel = Path.GetRelativePath(inputRoot, file);
                var logicalRoot = inputRoot;

                AnsiConsole.MarkupLine($"[green]Baking: [/] {file}");
                
                var blob = baker.BakeToBytes(new BakeContext
                {
                    InputPath = file,
                    OutputPath = "",
                    GenerateMips = settings.GenerateMips,
                    PremultiplyAlpha = settings.PremultiplyAlpha,
                    MaxDimension = settings.MaxSize,
                    MarkSRgb = settings.SRgb,
                    LogicalRoot = logicalRoot,
                });

                lock (pak) pak.Add(blob);
            }
            catch (Exception e)
            {
                AnsiConsole.WriteException(e);
                return 0;
            }
        }
        
        AnsiConsole.MarkupLine($"[green]Saving [/] {settings.OutputPak}");
        pak.Save(settings.OutputPak);
        
        return 1;
    }
}