using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dreambit.Editor.Assets;

internal readonly record struct AssetTypeInfo(AssetKind Kind, string? TypeId);

internal static class AssetTypeClassifier
{
    public const int ClassificationVersion = 3;

    private static readonly (string Suffix, AssetKind Kind, Type AssetType)[] JsonTypes =
    [
        (".spritesheet.json", AssetKind.SpriteSheet, typeof(SpriteSheet)),
        (".animation.json", AssetKind.Animation, typeof(SpriteSheetAnimation)),
        (".blueprint.json", AssetKind.Blueprint, typeof(EntityBlueprint)),
        (".particlefx.json", AssetKind.ParticleEffect, typeof(ParticleFxConfig)),
        (".soundcue.json", AssetKind.SoundCue, typeof(SoundCue)),
        (".cutscene.json", AssetKind.Cutscene, typeof(Dreambit.Scripting.Cutscene)),
        (".sprite.json", AssetKind.Sprite, typeof(Sprite)),
        (".scene.json", AssetKind.Scene, typeof(SceneBlueprint))
    ];

    public static AssetTypeInfo Classify(string relativePath)
    {
        return Classify(relativePath, null, out _);
    }

    public static AssetTypeInfo Classify(
        string relativePath,
        string? json,
        out string? diagnostic)
    {
        diagnostic = null;
        foreach (var (suffix, kind, assetType) in JsonTypes)
            if (relativePath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return new AssetTypeInfo(kind, DreambitAssetTypeRegistry.GetTypeId(assetType));

        if (Path.GetExtension(relativePath).Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            if (json is null)
                return new AssetTypeInfo(AssetKind.Json, null);

            try
            {
                var token = JToken.Parse(json);
                if (token is not JObject document)
                    return new AssetTypeInfo(AssetKind.Json, null);
                if (!document.TryGetValue(
                        DreambitAssetTypeRegistry.MetadataPropertyName,
                        StringComparison.Ordinal,
                        out var typeToken))
                {
                    return new AssetTypeInfo(AssetKind.Json, null);
                }

                if (typeToken.Type != JTokenType.String ||
                    string.IsNullOrWhiteSpace(typeToken.Value<string>()))
                {
                    diagnostic =
                        $"'{DreambitAssetTypeRegistry.MetadataPropertyName}' must be a non-empty string.";
                    return new AssetTypeInfo(AssetKind.Json, null);
                }

                return new AssetTypeInfo(
                    AssetKind.DreambitAsset,
                    typeToken.Value<string>()!.Trim());
            }
            catch (JsonException exception)
            {
                diagnostic = $"Could not inspect JSON metadata. {exception.Message}";
                return new AssetTypeInfo(AssetKind.Json, null);
            }
        }

        return Path.GetExtension(relativePath).ToLowerInvariant() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tga" or ".webp" =>
                new AssetTypeInfo(AssetKind.Texture, DreambitAssetTypeRegistry.GetTypeId(typeof(TextureAsset))),
            ".wav" or ".ogg" or ".mp3" or ".flac" =>
                new AssetTypeInfo(AssetKind.Audio, null),
            ".ttf" => new AssetTypeInfo(AssetKind.Font, DreambitAssetTypeRegistry.GetTypeId(typeof(FontAsset))),
            ".fx" => new AssetTypeInfo(AssetKind.Effect, DreambitAssetTypeRegistry.GetTypeId(typeof(EffectAsset))),
            ".txt" or ".md" => new AssetTypeInfo(AssetKind.Text, null),
            ".ldtk" or ".ldtkl" => new AssetTypeInfo(AssetKind.Ldtk, null),
            ".tmx" => new AssetTypeInfo(
                AssetKind.TiledMap,
                DreambitAssetTypeRegistry.GetTypeId(typeof(Dreambit.Tiled.TmxMap))),
            ".tsx" => new AssetTypeInfo(
                AssetKind.TiledMap,
                DreambitAssetTypeRegistry.GetTypeId(typeof(Dreambit.Tiled.TmxTileset))),
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
        if (string.IsNullOrWhiteSpace(asset.TypeId))
            return false;

        return DreambitAssetTypeRegistry.TryResolve(asset.TypeId, out var assetType) &&
               requestedType.IsAssignableFrom(assetType);
    }
}
