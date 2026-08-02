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
}
