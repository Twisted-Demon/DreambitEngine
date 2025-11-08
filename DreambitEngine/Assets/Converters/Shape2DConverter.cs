using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace Dreambit;

public class Shape2DConverter : PropertyConverter<Shape2D>
{
    
    public override void WriteJson(JsonWriter writer, Shape2D value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        foreach (var vert in value.GetVertices())
        {
            WriteVert(writer, vert);
        }
        writer.WriteEndArray();
    }

    public override Shape2D ReadJson(JsonReader reader, Type objectType, Shape2D existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if(reader.TokenType != JsonToken.StartArray)
            throw new JsonSerializationException("Expected Vector2[] as an array of [x,y] elements");

        var list = new List<Vector2>();
        
        // Move into the outer array
        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
        {
            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonSerializationException("Each Vector2 must be an array: [x,y].");

            // Read [x, y]
            if (!reader.Read()) throw new JsonSerializationException("Unexpected end while reading Vector2.x.");
            float x = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture);

            if (!reader.Read()) throw new JsonSerializationException("Unexpected end while reading Vector2.y.");
            float y = Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture);

            if (!reader.Read() || reader.TokenType != JsonToken.EndArray)
                throw new JsonSerializationException("Vector2 must have exactly 2 elements.");

            list.Add(new Vector2(x, y));
        }

        return PolyShape2D.Create(list.ToArray());
    }
    
    private static void WriteVert(JsonWriter writer, Vector2 vec)
    {
        writer.WriteStartArray();
        writer.WriteValue(vec.X);
        writer.WriteValue(vec.Y);
        writer.WriteEndArray();
    }
}