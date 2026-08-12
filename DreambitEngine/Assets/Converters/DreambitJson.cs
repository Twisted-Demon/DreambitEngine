using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dreambit;

/// <summary>
/// Public entry point for reading and writing Dreambit JSON using the exact
/// converter stack used by runtime .jsonb assets and entity blueprints.
/// </summary>
public static class DreambitJson
{
    public static JsonSerializerSettings CreateSerializerSettings()
        => PropertyConverterRegistry.CreateSerializerSettings();

    /// <summary>
    /// Re-scans loaded assemblies for PropertyConverter&lt;T&gt; implementations.
    /// Call this after loading a game/plugin assembly at runtime.
    /// </summary>
    public static void RefreshConverters()
        => PropertyConverterRegistry.Rebuild();

    /// <summary>
    /// Returns true when the currently loaded Dreambit converter registry has a
    /// PropertyConverter&lt;T&gt; registered for <paramref name="type"/>.
    /// </summary>
    public static bool HasPropertyConverter(Type type)
        => PropertyConverterRegistry.HasConverter(type);

    public static string Serialize(object value, Formatting formatting = Formatting.Indented)
    {
        var json = JsonConvert.SerializeObject(value, formatting, CreateSerializerSettings());
        if (value is not DreambitAsset asset ||
            !DreambitAssetTypeRegistry.ShouldPersistTypeMetadata(asset.GetType()))
        {
            return json;
        }

        var document = JObject.Parse(json);
        document.Property(
            DreambitAssetTypeRegistry.MetadataPropertyName,
            StringComparison.OrdinalIgnoreCase)?.Remove();
        document.AddFirst(new JProperty(
            DreambitAssetTypeRegistry.MetadataPropertyName,
            DreambitAssetTypeRegistry.GetTypeId(asset.GetType())));
        return document.ToString(formatting);
    }

    public static T Deserialize<T>(string json)
        => JsonConvert.DeserializeObject<T>(json, CreateSerializerSettings());

    public static object Deserialize(string json, Type type)
        => JsonConvert.DeserializeObject(json, type, CreateSerializerSettings());

    /// <summary>
    /// Deserializes a self-describing generic Dreambit asset using its <c>$dreambitType</c> ID.
    /// </summary>
    public static DreambitAsset DeserializeAsset(string json)
    {
        var document = JObject.Parse(json);
        var typeToken = document[DreambitAssetTypeRegistry.MetadataPropertyName];
        if (typeToken?.Type != JTokenType.String ||
            string.IsNullOrWhiteSpace(typeToken.Value<string>()))
        {
            throw new JsonSerializationException(
                $"A generic Dreambit asset requires a non-empty string " +
                $"'{DreambitAssetTypeRegistry.MetadataPropertyName}' property.");
        }

        var assetType = DreambitAssetTypeRegistry.Resolve(typeToken.Value<string>()!);
        return Deserialize(json, assetType) as DreambitAsset
               ?? throw new JsonSerializationException(
                   $"Could not deserialize Dreambit asset type '{assetType.FullName}'.");
    }

    public static JToken ToToken(object? value)
    {
        if (value is null)
            return JValue.CreateNull();

        var serializer = JsonSerializer.Create(CreateSerializerSettings());
        return JToken.FromObject(value, serializer);
    }

    public static object? FromToken(JToken token, Type type)
    {
        var serializer = JsonSerializer.Create(CreateSerializerSettings());
        return token.ToObject(type, serializer);
    }
}
