using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace Dreambit;

public class ColorConverter : PropertyConverter<Color>
{
    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        writer.WriteValue(value.R);
        writer.WriteValue(value.G);
        writer.WriteValue(value.B);
        writer.WriteValue(value.A);
        writer.WriteEndArray();
    }

    public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartArray)
            throw new JsonSerializationException("Color must be an array: [r,g,b] or [r,g,b,a].");

        // [r,g,b,(a)]
        reader.Read(); var r = Convert.ToInt32(reader.Value);
        reader.Read(); var g = Convert.ToInt32(reader.Value);
        reader.Read(); var b = Convert.ToInt32(reader.Value);

        byte a = 255;
        reader.Read();
        if (reader.TokenType != JsonToken.EndArray)
        {
            a = Convert.ToByte(reader.Value);
            reader.Read(); // move to EndArray
        }

        if (reader.TokenType != JsonToken.EndArray)
            throw new JsonSerializationException("Color array must have 3 or 4 elements.");

        return new Color((byte)r, (byte)g, (byte)b, a);
    }
}