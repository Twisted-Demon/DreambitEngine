using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dreambit;

internal sealed class DreambitAssetReferenceConverter : JsonConverter
{
    public static DreambitAssetReferenceConverter Instance { get; } = new();

    public override bool CanConvert(Type objectType)
    {
        return typeof(DreambitAsset).IsAssignableFrom(objectType);
    }

    public override object ReadJson(
        JsonReader reader,
        Type objectType,
        object existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        if (reader.TokenType == JsonToken.String)
        {
            var assetName = (string)reader.Value;

            if (string.IsNullOrWhiteSpace(assetName))
                return null;

            var asset = Resources.LoadDreambitAsset(assetName, objectType);

            if (asset is null)
                throw new JsonSerializationException(
                    $"Could not load asset '{assetName}' as '{objectType.FullName}'.");

            if (!objectType.IsInstanceOfType(asset))
                throw new JsonSerializationException(
                    $"Asset '{assetName}' resolved to '{asset.GetType().FullName}', " +
                    $"not '{objectType.FullName}'.");

            return asset;
        }

        if (reader.TokenType == JsonToken.StartObject)
        {
            var jsonObject = JObject.Load(reader);
            var inlineAsset = jsonObject.ToObject(objectType, serializer);

            if (inlineAsset is null)
                throw new JsonSerializationException(
                    $"Could not deserialize inline asset '{objectType.FullName}'.");

            return inlineAsset;
        }

        throw new JsonSerializationException(
            $"Expected an asset path or inline object for " +
            $"'{objectType.FullName}', but found {reader.TokenType}.");
    }

    public override void WriteJson(
        JsonWriter writer,
        object value,
        JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        var asset = (DreambitAsset)value;

        if (!string.IsNullOrWhiteSpace(asset.AssetName))
        {
            writer.WriteValue(asset.AssetName);
            return;
        }

        serializer.Serialize(writer, asset);
    }
}