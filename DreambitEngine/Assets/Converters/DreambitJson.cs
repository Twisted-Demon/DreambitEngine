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
        => JsonConvert.SerializeObject(value, formatting, CreateSerializerSettings());

    public static T Deserialize<T>(string json)
        => JsonConvert.DeserializeObject<T>(json, CreateSerializerSettings());

    public static object Deserialize(string json, Type type)
        => JsonConvert.DeserializeObject(json, type, CreateSerializerSettings());

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
