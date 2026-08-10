#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.LDtk;

/// <summary>
/// Converts raw LDtk values into their closest MonoGame representations.
/// Conversions are explicit methods so callers must choose when pixel or grid
/// coordinates should also be converted into Dreambit world units.
/// </summary>
public static class LDtkMonoGameExtensions
{
    public static Point ToPoint(this LdtkPoint value)
        => new(value.X, value.Y);

    public static Point? ToPoint(this LdtkPoint? value)
        => value?.ToPoint();

    public static Vector2 ToVector2(this LdtkPoint value)
        => new(value.X, value.Y);

    public static Vector2? ToVector2(this LdtkPoint? value)
        => value?.ToVector2();

    public static Vector3 ToVector3(this LdtkPoint value, float z = 0f)
        => new(value.X, value.Y, z);

    public static Vector3? ToVector3(this LdtkPoint? value, float z = 0f)
        => value?.ToVector3(z);

    /// <summary>Converts an LDtk pixel coordinate into Dreambit world units.</summary>
    public static Vector2 ToWorldVector2(this LdtkPoint value, float pixelsPerUnit)
    {
        ValidatePositiveFinite(pixelsPerUnit, nameof(pixelsPerUnit));
        return value.ToVector2() / pixelsPerUnit;
    }

    /// <summary>Converts an LDtk pixel coordinate into Dreambit world units.</summary>
    public static Vector3 ToWorldVector3(this LdtkPoint value, float pixelsPerUnit, float z = 0f)
    {
        var position = value.ToWorldVector2(pixelsPerUnit);
        return new Vector3(position, z);
    }

    public static Vector2 ToVector2(this LdtkVector2 value)
        => new(value.X, value.Y);

    public static Vector2? ToVector2(this LdtkVector2? value)
        => value?.ToVector2();

    public static Vector3 ToVector3(this LdtkVector2 value, float z = 0f)
        => new(value.X, value.Y, z);

    public static Vector3? ToVector3(this LdtkVector2? value, float z = 0f)
        => value?.ToVector3(z);

    public static Color ToColor(this LdtkColor value)
        => new(value.R, value.G, value.B, value.A);

    public static Color? ToColor(this LdtkColor? value)
        => value?.ToColor();

    public static Vector2 ToPositionVector2(this EntityInstance value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Px.ToVector2();
    }

    public static Vector2 ToWorldPositionVector2(this EntityInstance value, float pixelsPerUnit)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Px.ToWorldVector2(pixelsPerUnit);
    }

    public static Vector2 ToSizeVector2(this EntityInstance value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Vector2(value.Width, value.Height);
    }

    public static Vector2 ToWorldSizeVector2(this EntityInstance value, float pixelsPerUnit)
    {
        ArgumentNullException.ThrowIfNull(value);
        ValidatePositiveFinite(pixelsPerUnit, nameof(pixelsPerUnit));
        return value.ToSizeVector2() / pixelsPerUnit;
    }

    public static Vector2 ToPivotVector2(this EntityInstance value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value._Pivot.ToVector2();
    }

    public static Rectangle? ToTileRectangle(this EntityInstance value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value._Tile?.ToRectangle();
    }

    public static Color ToSmartColor(this EntityInstance value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value._SmartColor.ToColor();
    }

    /// <summary>Converts an LDtk Point field's grid coordinate.</summary>
    public static Point ToPoint(this GridPoint value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Point(value.Cx, value.Cy);
    }

    public static Vector2 ToVector2(this GridPoint value)
        => value.ToPoint().ToVector2();

    /// <summary>Converts an LDtk Point field from grid cells to pixels.</summary>
    public static Point ToPixelPoint(this GridPoint value, int gridSize)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfLessThan(gridSize, 1);
        return new Point(
            checked(value.Cx * gridSize),
            checked(value.Cy * gridSize));
    }

    /// <summary>Converts an LDtk Point field from grid cells to Dreambit world units.</summary>
    public static Vector2 ToWorldVector2(
        this GridPoint value,
        float gridSize,
        float pixelsPerUnit)
    {
        ArgumentNullException.ThrowIfNull(value);
        ValidatePositiveFinite(gridSize, nameof(gridSize));
        ValidatePositiveFinite(pixelsPerUnit, nameof(pixelsPerUnit));
        return new Vector2(value.Cx * gridSize, value.Cy * gridSize) / pixelsPerUnit;
    }

    public static Rectangle ToRectangle(this TilesetRectangle value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Rectangle(value.X, value.Y, value.W, value.H);
    }

    public static Point ToPositionPoint(this TileInstance value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Px.ToPoint();
    }

    public static Vector2 ToPositionVector2(this TileInstance value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Px.ToVector2();
    }

    public static Rectangle ToSourceRectangle(this TileInstance value, int tileSize)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfLessThan(tileSize, 1);
        return new Rectangle(value.Src.X, value.Src.Y, tileSize, tileSize);
    }

    public static SpriteEffects ToSpriteEffects(this TileInstance value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var effects = SpriteEffects.None;
        if ((value.F & 1) != 0)
            effects |= SpriteEffects.FlipHorizontally;
        if ((value.F & 2) != 0)
            effects |= SpriteEffects.FlipVertically;
        return effects;
    }

    public static Color ToTint(this TileInstance value, float layerOpacity = 1f)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!float.IsFinite(layerOpacity))
            throw new ArgumentOutOfRangeException(nameof(layerOpacity), "Opacity must be finite.");
        return Color.White * MathHelper.Clamp(value.A * layerOpacity, 0f, 1f);
    }

    /// <summary>
    /// Converts the LDtk background crop array into the integer source
    /// rectangle expected by MonoGame's SpriteBatch.
    /// </summary>
    public static Rectangle ToCropRectangle(this LevelBackgroundPosition value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.CropRect is not { Length: >= 4 } crop)
            throw new InvalidOperationException("The LDtk background position has no four-value crop rectangle.");

        return new Rectangle(
            RoundToInt(crop[0], "crop X"),
            RoundToInt(crop[1], "crop Y"),
            RoundToInt(crop[2], "crop width"),
            RoundToInt(crop[3], "crop height"));
    }

    public static Color? GetMonoGameColor(this FieldInstance field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return field._Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            ? null
            : field.GetValue<LdtkColor>().ToColor();
    }

    public static IReadOnlyList<Color> GetMonoGameColors(this FieldInstance field)
    {
        ArgumentNullException.ThrowIfNull(field);
        var values = field.GetValue<LdtkColor[]>() ?? [];
        var result = new Color[values.Length];
        for (var index = 0; index < values.Length; index++)
            result[index] = values[index].ToColor();
        return result;
    }

    public static Point? GetMonoGamePoint(this FieldInstance field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.GetValue<GridPoint>()?.ToPoint();
    }

    public static IReadOnlyList<Point> GetMonoGamePoints(this FieldInstance field)
    {
        ArgumentNullException.ThrowIfNull(field);
        var values = field.GetValue<GridPoint[]>() ?? [];
        var result = new Point[values.Length];
        for (var index = 0; index < values.Length; index++)
            result[index] = values[index].ToPoint();
        return result;
    }

    public static Rectangle? GetMonoGameRectangle(this FieldInstance field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.GetValue<TilesetRectangle>()?.ToRectangle();
    }

    public static IReadOnlyList<Rectangle> GetMonoGameRectangles(this FieldInstance field)
    {
        ArgumentNullException.ThrowIfNull(field);
        var values = field.GetValue<TilesetRectangle[]>() ?? [];
        var result = new Rectangle[values.Length];
        for (var index = 0; index < values.Length; index++)
            result[index] = values[index].ToRectangle();
        return result;
    }

    private static int RoundToInt(float value, string description)
    {
        var preciseValue = (double)value;
        if (!float.IsFinite(value) || preciseValue < int.MinValue || preciseValue > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), $"LDtk {description} must fit in a MonoGame Rectangle.");
        return checked((int)MathF.Round(value));
    }

    private static void ValidatePositiveFinite(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value <= 0f)
            throw new ArgumentOutOfRangeException(parameterName, "Value must be positive and finite.");
    }
}
