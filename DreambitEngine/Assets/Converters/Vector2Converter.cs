using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace Dreambit;

public class Vector2Converter : PropertyConverter<Vector2>
{
    public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        writer.WriteValue(value.X);
        writer.WriteValue(value.Y);
        writer.WriteEndArray();
    }

    public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartArray)
            throw new JsonSerializationException("Vector2 must be an array: [x,y].");

        reader.Read();
        var x = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture);
        reader.Read();
        var y = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture);

        if (!reader.Read() || reader.TokenType != JsonToken.EndArray)
            throw new JsonSerializationException("Vector2 must have exactly 2 elements.");

        return new Vector2(x, y);
    }
}