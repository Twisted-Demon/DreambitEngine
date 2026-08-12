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
                new AssetTypeInfo(AssetKind.Texture, "Dreambit.TextureAsset"),
            ".wav" or ".ogg" or ".mp3" or ".flac" =>
                new AssetTypeInfo(AssetKind.Audio, null),
            ".ttf" => new AssetTypeInfo(AssetKind.Font, "Dreambit.FontAsset"),
            ".fx" => new AssetTypeInfo(AssetKind.Effect, "Dreambit.EffectAsset"),
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

    public static bool IsCompatibleWith(AssetRecord asset, Type requestedType)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(requestedType);

        if (asset.Kind == AssetKind.Texture && requestedType == typeof(TextureAsset))
            return true;
        if (asset.Kind == AssetKind.Font && requestedType == typeof(FontAsset))
            return true;
        if (asset.Kind == AssetKind.Effect && requestedType == typeof(EffectAsset))
            return true;
        if (string.IsNullOrWhiteSpace(asset.TypeName))
            return false;

        var assetType = ResolveType(asset.TypeName);
        return assetType is not null && requestedType.IsAssignableFrom(assetType);
    }

    private static Type? ResolveType(string typeName)
    {
        var type = Type.GetType(typeName, throwOnError: false, ignoreCase: true);
        if (type is not null)
            return type;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName, throwOnError: false, ignoreCase: true);
            if (type is not null)
                return type;
        }

        return null;
    }
}
