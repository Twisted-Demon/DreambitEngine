#nullable enable

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dreambit.LDtk;

[JsonConverter(typeof(LdtkPointJsonConverter))]
public readonly record struct LdtkPoint(int X, int Y);

[JsonConverter(typeof(LdtkVector2JsonConverter))]
public readonly record struct LdtkVector2(float X, float Y);

[JsonConverter(typeof(LdtkColorJsonConverter))]
public readonly record struct LdtkColor(byte R, byte G, byte B, byte A = byte.MaxValue)
{
    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";
}

internal sealed class LdtkPointJsonConverter : JsonConverter<LdtkPoint>
{
    public override LdtkPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected an LDtk [x,y] point array.");

        reader.Read();
        var x = reader.GetInt32();
        reader.Read();
        var y = reader.GetInt32();
        reader.Read();

        if (reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected exactly two values in an LDtk point array.");

        return new LdtkPoint(x, y);
    }

    public override void Write(Utf8JsonWriter writer, LdtkPoint value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteEndArray();
    }
}

internal sealed class LdtkVector2JsonConverter : JsonConverter<LdtkVector2>
{
    public override LdtkVector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected an LDtk [x,y] vector array.");

        reader.Read();
        var x = reader.GetSingle();
        reader.Read();
        var y = reader.GetSingle();
        reader.Read();

        if (reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected exactly two values in an LDtk vector array.");

        return new LdtkVector2(x, y);
    }

    public override void Write(Utf8JsonWriter writer, LdtkVector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteEndArray();
    }
}

internal sealed class LdtkColorJsonConverter : JsonConverter<LdtkColor>
{
    public override LdtkColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value is null || value.Length is not (7 or 9) || value[0] != '#')
            throw new JsonException($"Invalid LDtk color '{value}'.");

        var r = byte.Parse(value.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var g = byte.Parse(value.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var b = byte.Parse(value.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var a = value.Length == 9
            ? byte.Parse(value.AsSpan(7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            : byte.MaxValue;

        return new LdtkColor(r, g, b, a);
    }

    public override void Write(Utf8JsonWriter writer, LdtkColor value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.A == byte.MaxValue
            ? $"#{value.R:X2}{value.G:X2}{value.B:X2}"
            : value.ToString());
    }
}

public enum LayerType
{
    IntGrid,
    Entities,
    Tiles,
    AutoLayer,
}

public enum LdtkProjectFlag
{
    DiscardPreCsvIntGrid,
    ExportOldTableOfContentData,
    ExportPreCsvIntGridFormat,
    IgnoreBackupSuggest,
    PrependIndexToLevelFileNames,
    MultiWorlds,
    UseMultilinesType,
}
