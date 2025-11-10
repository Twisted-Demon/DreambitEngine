using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace Dreambit;

public class Box2DConverter : PropertyConverter<Box2D>
{
    public override void WriteJson(JsonWriter writer, Box2D value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        WriteVert(writer, value.TopLeft);
        WriteVert(writer, value.TopRight);
        WriteVert(writer, value.BottomRight);
        WriteVert(writer, value.BottomLeft);
        writer.WriteEndArray();
    }

    private static void WriteVert(JsonWriter writer, Vector2 vec)
    {
        writer.WriteStartArray();
        writer.WriteValue(vec.X);
        writer.WriteValue(vec.Y);
        writer.WriteEndArray();
    }

    public override Box2D ReadJson(JsonReader reader, Type objectType, Box2D existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartArray)
            throw new JsonSerializationException("Expected Vector2[] as an array of [x,y] elements");

        var list = new List<Vector2>(4);

        // Move into the outer array
        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
        {
            if (list.Count == 4)
                throw new JsonSerializationException("Expected exactly 4 Vector2s, found more.");

            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonSerializationException("Each Vector2 must be an array: [x,y].");

            // Read [x, y]
            if (!reader.Read()) throw new JsonSerializationException("Unexpected end while reading Vector2.x.");
            var x = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture);

            if (!reader.Read()) throw new JsonSerializationException("Unexpected end while reading Vector2.y.");
            var y = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture);

            if (!reader.Read() || reader.TokenType != JsonToken.EndArray)
                throw new JsonSerializationException("Vector2 must have exactly 2 elements.");

            list.Add(new Vector2(x, y));
        }

        if (list.Count != 4)
            throw new JsonSerializationException($"Expected exactly 4 Vector2s, got {list.Count}.");

        return Box2D.CreateFromVerts(list.ToArray());
    }
}