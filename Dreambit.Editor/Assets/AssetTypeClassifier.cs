namespace Dreambit.Editor.Assets;

internal readonly record struct AssetTypeInfo(AssetKind Kind, string? TypeName);

internal static class AssetTypeClassifier
{
    private static readonly (string Suffix, AssetKind Kind, string TypeName)[] JsonTypes =
    [
        (".spritesheet.json", AssetKind.SpriteSheet, "Dreambit.SpriteSheet"),
        (".animation.json", AssetKind.Animation, "Dreambit.SpriteSheetAnimation"),
        (".blueprint.json", AssetKind.Blueprint, "Dreambit.EntityBlueprint"),
        (".particlefx.json", AssetKind.ParticleEffect, "Dreambit.ParticleFxConfig"),
        (".soundcue.json", AssetKind.SoundCue, "Dreambit.SoundCue"),
        (".cutscene.json", AssetKind.Cutscene, "Dreambit.Scripting.Cutscene"),
        (".sprite.json", AssetKind.Sprite, "Dreambit.Sprite"),
        (".scene.json", AssetKind.Scene, "Dreambit.SceneBlueprint")
    ];

    public static AssetTypeInfo Classify(string relativePath)
    {
        foreach (var (suffix, kind, typeName) in JsonTypes)
            if (relativePath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return new AssetTypeInfo(kind, typeName);

        return Path.GetExtension(relativePath).ToLowerInvariant() switch
        {
            ".json" => new AssetTypeInfo(AssetKind.Json, null),
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tga" or ".webp" =>
                new AssetTypeInfo(AssetKind.Texture, "Microsoft.Xna.Framework.Graphics.Texture2D"),
            ".wav" or ".ogg" or ".mp3" or ".flac" =>
                new AssetTypeInfo(AssetKind.Audio, null),
            ".ttf" or ".otf" => new AssetTypeInfo(AssetKind.Font, null),
            ".txt" or ".md" => new AssetTypeInfo(AssetKind.Text, null),
            ".ldtk" or ".ldtkl" => new AssetTypeInfo(AssetKind.Ldtk, null),
            ".tmx" or ".tsx" => new AssetTypeInfo(AssetKind.TiledMap, null),
            ".xml" or ".yaml" or ".yml" => new AssetTypeInfo(AssetKind.Data, null),
            _ => new AssetTypeInfo(AssetKind.Unknown, null)
        };
    }

    public static string GetDuplicateFileName(string fileName, int copyNumber)
    {
        var copyLabel = copyNumber == 1 ? " Copy" : $" Copy {copyNumber}";
        foreach (var (suffix, _, _) in JsonTypes)
        {
            if (!fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                continue;

            return fileName[..^suffix.Length] + copyLabel + fileName[^suffix.Length..];
        }

        var extension = Path.GetExtension(fileName);
        return fileName[..^extension.Length] + copyLabel + extension;
    }

    public static string GetFileSuffix(Type type)
    {
        if (type == typeof(EntityBlueprint)) return ".blueprint.json";
        if (type == typeof(SceneBlueprint)) return ".scene.json";
        if (type == typeof(Sprite)) return ".sprite.json";
        if (type == typeof(SpriteSheet)) return ".spritesheet.json";
        if (type == typeof(SpriteSheetAnimation)) return ".animation.json";
        if (type == typeof(SoundCue)) return ".soundcue.json";
        if (type == typeof(ParticleFxConfig)) return ".particlefx.json";
        if (type == typeof(Dreambit.Scripting.Cutscene)) return ".cutscene.json";
        return ".json";
    }
}
