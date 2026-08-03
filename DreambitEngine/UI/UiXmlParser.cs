using System;
using System.Globalization;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>
/// Provides shared helpers for reading UI element and brush values from XML.
/// </summary>
public static class UiXmlParser
{
    /// <summary>Reads a string attribute from an XML node.</summary>
    /// <param name="node">The XML node containing the attribute.</param>
    /// <param name="name">The attribute name.</param>
    /// <param name="defaultValue">The value returned when the attribute is absent.</param>
    /// <returns>The attribute value or <paramref name="defaultValue"/>.</returns>
    public static string ParseString(XmlNode node, string name, string defaultValue)
    {
        if (node.Attributes == null)
            return defaultValue;

        var attribute = node.Attributes[name];
        return attribute?.Value ?? defaultValue;
    }

    /// <summary>Reads an invariant-culture floating-point attribute.</summary>
    /// <param name="node">The XML node containing the attribute.</param>
    /// <param name="attribute">The attribute name.</param>
    /// <param name="defaultValue">The value used when the attribute is absent.</param>
    /// <returns>The parsed floating-point value.</returns>
    public static float ParseFloat(
        XmlNode node,
        string attribute,
        float defaultValue = 0.0f)
    {
        return float.Parse(
            ParseString(
                node,
                attribute,
                defaultValue.ToString(CultureInfo.InvariantCulture)),
            CultureInfo.InvariantCulture);
    }

    /// <summary>Reads an invariant-culture integer attribute.</summary>
    /// <param name="node">The XML node containing the attribute.</param>
    /// <param name="attribute">The attribute name.</param>
    /// <param name="defaultValue">The value used when the attribute is absent.</param>
    /// <returns>The parsed integer value.</returns>
    public static int ParseInt(
        XmlNode node,
        string attribute,
        int defaultValue = 0)
    {
        return int.Parse(
            ParseString(
                node,
                attribute,
                defaultValue.ToString(CultureInfo.InvariantCulture)),
            CultureInfo.InvariantCulture);
    }

    /// <summary>Reads a Boolean attribute.</summary>
    /// <param name="node">The XML node containing the attribute.</param>
    /// <param name="attribute">The attribute name.</param>
    /// <param name="defaultValue">The value used when the attribute is absent.</param>
    /// <returns>The parsed Boolean value.</returns>
    public static bool ParseBool(
        XmlNode node,
        string attribute,
        bool defaultValue = false)
    {
        return bool.Parse(
            ParseString(
                node,
                attribute,
                defaultValue.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Reads a <c>#RRGGBB</c> or <c>#RRGGBBAA</c> attribute as a premultiplied
    /// color.
    /// </summary>
    /// <param name="node">The XML node containing the attribute.</param>
    /// <param name="attribute">The attribute name.</param>
    /// <returns>The parsed color.</returns>
    public static Color ParseColor(XmlNode node, string attribute)
    {
        return ColorExt.FromHex(ParseString(node, attribute, "#ff00dc"));
    }

    /// <summary>Reads two invariant-culture floating-point attributes as a vector.</summary>
    /// <param name="node">The XML node containing the attributes.</param>
    /// <param name="attrX">The horizontal component attribute.</param>
    /// <param name="attrY">The vertical component attribute.</param>
    /// <returns>The parsed vector.</returns>
    public static Vector2 ParseVector2(
        XmlNode node,
        string attrX,
        string attrY)
    {
        return new Vector2(
            ParseFloat(node, attrX),
            ParseFloat(node, attrY));
    }

    /// <summary>Parses a pixel, percentage, or <c>*</c> automatic length.</summary>
    /// <param name="value">The XML length text.</param>
    /// <returns>The parsed UI length.</returns>
    public static UiLength ParseLength(string value)
    {
        if (string.IsNullOrEmpty(value))
            return UiLength.Pixels(0);

        value = value.Trim();

        if (value == "*")
            return UiLength.Auto();

        if (value.EndsWith('%'))
        {
            var number = value.Substring(0, value.Length - 1);
            var percentage = float.Parse(
                number,
                CultureInfo.InvariantCulture) / 100f;
            return UiLength.Percent(percentage);
        }

        var pixels = float.Parse(value, CultureInfo.InvariantCulture);
        return UiLength.Pixels(pixels);
    }

    /// <summary>Parses an anchor name, defaulting to <see cref="UiAnchor.TopLeft"/>.</summary>
    /// <param name="value">The anchor name.</param>
    /// <returns>The parsed anchor.</returns>
    public static UiAnchor ParseAnchor(string value)
    {
        return Enum.TryParse<UiAnchor>(value, true, out var anchor)
            ? anchor
            : UiAnchor.TopLeft;
    }

    /// <summary>
    /// Parses either one uniform inset or four comma-separated insets in
    /// left, top, right, bottom order.
    /// </summary>
    /// <param name="value">The thickness text to parse.</param>
    /// <param name="valueName">The value name used in parse errors.</param>
    /// <returns>The parsed edge thickness.</returns>
    /// <exception cref="XmlException">
    /// Thrown when the value does not contain one or four non-negative integers.
    /// </exception>
    public static UiThickness ParseThickness(
        string value,
        string valueName = "Thickness")
    {
        var parts = (value ?? string.Empty).Split(',');

        if (parts.Length == 1)
        {
            var uniform = ParseThicknessPart(parts[0], valueName);
            return UiThickness.Uniform(uniform);
        }

        if (parts.Length == 4)
        {
            return new UiThickness(
                ParseThicknessPart(parts[0], valueName),
                ParseThicknessPart(parts[1], valueName),
                ParseThicknessPart(parts[2], valueName),
                ParseThicknessPart(parts[3], valueName));
        }

        throw new XmlException(
            $"{valueName} must be one value or four comma-separated values.");
    }

    private static int ParseThicknessPart(string value, string valueName)
    {
        if (!int.TryParse(
                value.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var result) ||
            result < 0)
        {
            throw new XmlException(
                $"{valueName} values must be non-negative integers.");
        }

        return result;
    }

    /// <summary>
    /// Parses a grid track expressed as pixels, a percentage, <c>Auto</c>,
    /// <c>*</c>, or a weighted star such as <c>2*</c>.
    /// </summary>
    /// <param name="value">The grid track text.</param>
    /// <returns>The parsed grid length.</returns>
    /// <exception cref="XmlException">Thrown when the track is invalid.</exception>
    public static UiGridLength ParseGridLength(string value)
    {
        value = value?.Trim() ?? string.Empty;
        if (string.Equals(value, "Auto", StringComparison.OrdinalIgnoreCase))
            return UiGridLength.Auto();

        if (value.EndsWith('*'))
        {
            var weightText = value[..^1].Trim();
            var weight = weightText.Length == 0
                ? 1f
                : ParseNonNegativeFloat(weightText, "Grid star weight");
            if (weight <= 0f)
                throw new XmlException("Grid star weight must be greater than zero.");

            return UiGridLength.Star(weight);
        }

        if (value.EndsWith('%'))
        {
            var percent = ParseNonNegativeFloat(
                value[..^1].Trim(),
                "Grid percentage") / 100f;
            return UiGridLength.Percent(percent);
        }

        return UiGridLength.Pixels(
            ParseNonNegativeFloat(value, "Grid pixel size"));
    }

    private static float ParseNonNegativeFloat(string value, string valueName)
    {
        if (!float.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var result) ||
            !float.IsFinite(result) ||
            result < 0f)
        {
            throw new XmlException($"{valueName} must be a non-negative number.");
        }

        return result;
    }
}
