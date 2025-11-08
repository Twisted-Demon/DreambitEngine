using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Dreambit;

public class Vector3Converter : PropertyConverter<Vector3>
{
    public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        writer.WriteValue(value.X);
        writer.WriteValue(value.Y);
        writer.WriteValue(value.Z);
        writer.WriteEndArray();
    }

    public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartArray)
            throw new JsonSerializationException("Vector3 must be an array: [x,y,z].");

        reader.Read(); var x = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture);
        reader.Read(); var y = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture);
        reader.Read(); var z = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture);

        if (!reader.Read() || reader.TokenType != JsonToken.EndArray)
            throw new JsonSerializationException("Vector3 must have exactly 3 elements.");

        return new Vector3(x, y, z);
    }
}