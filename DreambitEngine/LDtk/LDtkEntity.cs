#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace Dreambit.LDtk;

public class LDtkEntity
{
    public string Identifier { get; set; } = string.Empty;
    public Guid Iid { get; set; }
    public int Uid { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public Vector2 PixelPosition { get; set; }
    public Vector2 PixelSize { get; set; }
    public Vector2 Pivot { get; set; }
    public Rectangle Tile { get; set; }
    public Color SmartColor { get; set; }
    public string[] Tags { get; set; } = [];
    public LayerInstance? Layer { get; set; }
    public int DrawLayer { get; set; }
    public float PixelsPerUnit { get; set; } = 1f;

    /// <summary>Raw LDtk field values keyed by their field identifiers.</summary>
    public Dictionary<string, JsonElement> Fields { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Deserializes a stored LDtk field value as <typeparamref name="T"/>.
    /// Missing fields throw; an explicit JSON null returns the default value.
    /// </summary>
    public T? GetField<T>(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        if (!Fields.TryGetValue(identifier, out var value))
        {
            Core.Logger.Warn($"Field '{identifier}' could not be found.");
            return default;
        }

        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            Core.Logger.Warn($"Field '{identifier}' is null");
            return default;
        }

        try
        {
            return value.Deserialize<T>(LdtkJson.Options);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            Core.Logger.Error($"Could not deserialize field '{identifier}' on LDtk entity " +
                              $"'{Identifier}' ({Iid}) as '{typeof(T).FullName}'.");

            return default;
        }
    }

    /// <summary>
    /// Deserializes a field, returning <paramref name="defaultValue"/> when it
    /// is missing, null, or incompatible with <typeparamref name="T"/>.
    /// </summary>
    public T GetField<T>(string identifier, T defaultValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        if (!Fields.TryGetValue(identifier, out var rawValue) ||
            rawValue.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return defaultValue;

        return TryGetField<T>(identifier, out var value) && value is not null
            ? value
            : defaultValue;
    }

    /// <summary>
    /// Attempts to deserialize a stored field without logging or throwing for
    /// a missing field or incompatible target type. An explicit JSON null is a
    /// successful lookup whose output is <c>default</c>.
    /// </summary>
    public bool TryGetField<T>(string identifier, out T? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        value = default;

        if (!Fields.TryGetValue(identifier, out var rawValue))
            return false;

        if (rawValue.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return true;

        try
        {
            value = rawValue.Deserialize<T>(LdtkJson.Options);
            return true;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return false;
        }
    }

    public static LDtkEntity FromInstance(EntityInstance instance)
        => FromInstance(instance, null, 1f, DrawLayers.DefaultLayer);

    /// <summary>
    /// Creates a runtime-ready snapshot. Position includes the owning layer's
    /// total pixel offset, and position and size are converted to world units.
    /// </summary>
    public static LDtkEntity FromInstance(
        EntityInstance instance,
        LayerInstance? layer,
        float pixelsPerUnit,
        int drawLayer)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (!float.IsFinite(pixelsPerUnit) || pixelsPerUnit <= 0f)
            throw new ArgumentOutOfRangeException(
                nameof(pixelsPerUnit),
                "Pixels per unit must be positive and finite.");

        var pixelPosition = instance.ToPositionVector2();
        if (layer is not null)
            pixelPosition += new Vector2(layer._PxTotalOffsetX, layer._PxTotalOffsetY);
        var pixelSize = instance.ToSizeVector2();

        var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var field in instance.FieldInstances ?? [])
        {
            if (!fields.TryAdd(field._Identifier, field._Value.Clone()))
                throw new LdtkException(
                    $"LDtk entity '{instance._Identifier}' ({instance.Iid}) contains " +
                    $"more than one field named '{field._Identifier}'.");
        }

        return new LDtkEntity
        {
            Identifier = instance._Identifier,
            Iid = instance.Iid,
            Uid = instance.DefUid,
            Position = pixelPosition / pixelsPerUnit,
            Size = pixelSize / pixelsPerUnit,
            PixelPosition = pixelPosition,
            PixelSize = pixelSize,
            Pivot = instance.ToPivotVector2(),
            Tile = instance.ToTileRectangle() ?? Rectangle.Empty,
            SmartColor = instance.ToSmartColor(),
            Fields = fields,
            Tags = instance._Tags ?? [],
            Layer = layer,
            DrawLayer = drawLayer,
            PixelsPerUnit = pixelsPerUnit,
        };
    }
}
