using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace Dreambit;

public class PointConverter : PropertyConverter<Point>
{
    public override void WriteJson(JsonWriter writer, Point value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        writer.WriteValue(value.X);
        writer.WriteValue(value.Y);
        writer.WriteEndArray();
    }

    public override Point ReadJson(JsonReader reader, Type objectType, Point existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartArray)
            throw new JsonSerializationException("Point must be an array: [x,y]");

        reader.Read();
        var x = Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture);
        reader.Read();
        var y = Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture);

        if (!reader.Read() || reader.TokenType != JsonToken.EndArray)
            throw new JsonSerializationException("Point must have exactly 2 elements");

        return new Point(x, y);
    }
}