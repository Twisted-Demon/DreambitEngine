using System;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace Dreambit;

public class RectangleConverter : PropertyConverter<Rectangle>
{
    public override void WriteJson(JsonWriter writer, Rectangle value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        writer.WriteValue(value.X);
        writer.WriteValue(value.Y);
        writer.WriteValue(value.Width);
        writer.WriteValue(value.Height);
        writer.WriteEndArray();
    }

    public override Rectangle ReadJson(JsonReader reader, Type objectType, Rectangle existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartArray)
            throw new JsonSerializationException("Rectangle must be an array: [x,y,width,height].");

        reader.Read();
        var x = Convert.ToInt32(reader.Value);
        reader.Read();
        var y = Convert.ToInt32(reader.Value);
        reader.Read();
        var w = Convert.ToInt32(reader.Value);
        reader.Read();
        var h = Convert.ToInt32(reader.Value);

        if (!reader.Read() || reader.TokenType != JsonToken.EndArray)
            throw new JsonSerializationException("Rectangle must have exactly 4 elements.");

        return new Rectangle(x, y, w, h);
    }
}