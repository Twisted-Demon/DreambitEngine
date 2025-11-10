using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace Dreambit;

public class Vector4Converter : PropertyConverter<Vector4>
{
    public override void WriteJson(JsonWriter writer, Vector4 value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        writer.WriteValue(value.X);
        writer.WriteValue(value.Y);
        writer.WriteValue(value.Z);
        writer.WriteValue(value.W);
        writer.WriteEndArray();
    }

    public override Vector4 ReadJson(JsonReader reader, Type objectType, Vector4 existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartArray)
            throw new JsonSerializationException("Vector4 must be an array: [x,y,z,w].");

        reader.Read();
        var x = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture);
        reader.Read();
        var y = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture);
        reader.Read();
        var z = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture);
        reader.Read();
        var w = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture);

        if (!reader.Read() || reader.TokenType != JsonToken.EndArray)
            throw new JsonSerializationException("Vector4 must have exactly 4 elements.");

        return new Vector4(x, y, z, w);
    }
}