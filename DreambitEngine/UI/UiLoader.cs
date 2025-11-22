using System;
using System.Globalization;
using System.Xml;
using Microsoft.Xna.Framework;
using Spectre.Console;
using Color = Microsoft.Xna.Framework.Color;

namespace Dreambit.UI;

public static class UiLoader
{
    public static UiLayout LoadFromXml(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        var rootNode = doc.SelectSingleNode("Ui");

        var layout = new UiLayout();

        var screenSize = Window.ScreenSize;

        var rootPanel = new UiPanel();
        rootPanel.Id = "root";
        rootPanel.X = UiLength.Pixels(0);
        rootPanel.Y = UiLength.Pixels(0);
        rootPanel.Width = UiLength.Pixels(screenSize.X);
        rootPanel.Height = UiLength.Pixels(screenSize.Y);
        rootPanel.Anchor = UiAnchor.TopLeft;

        layout.Root = rootPanel;

        if (rootNode is null) return null;

        foreach (XmlNode child in rootNode.ChildNodes)
        {
            if (child.NodeType != XmlNodeType.Element)
                continue;

            var elem = ParseElement(child, rootPanel);
            if (elem != null)
                rootPanel.Children.Add(elem);
        }
        
        //resolve dependencies?
        
        layout.Root.Arrange(new Rectangle(0, 0, screenSize.X, screenSize.Y));
        return layout;
    }

    private static UiElement ParseElement(XmlNode node, UiContainer parent)
    {
        UiElement element = null;

        switch (node.Name)
        {
            case "Panel":
                element = new UiPanel();
                break;
            case "Text":
                element  = new UiText();
                element.Parse(node);
                break;
            case "StackPanel":
                element = new UiStackPanel();
                element.Parse(node);
                break;
        }
        
        if(element is null) return null;

        element.Parent = parent;
        element.Id = GetString(node, "id", string.Empty);
        element.X = ParseLength(GetString(node, "x", "0"));
        element.Y = ParseLength(GetString(node, "y", "0"));
        element.Width = ParseLength(GetString(node, "width", "100%"));
        element.Height = ParseLength(GetString(node, "height", "100%"));
        element.Anchor = ParseAnchor(GetString(node, "anchor", "TopLeft"));
        element.ZIndex = GetInt(node, "z", 0);

        if (element is UiContainer container)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.NodeType != XmlNodeType.Element)
                    continue;

                var childElem = ParseElement(childNode, container);
                if(childElem != null)
                    container.Children.Add(childElem);
            }
        }

        return element;
    }

    public static string GetString(XmlNode node, string name, string defaultValue)
    {
        if (node.Attributes == null) return string.Empty;
        
        var attr = node.Attributes[name];
        return attr != null ? attr.Value : defaultValue;

    }

    public static float GetFloat(XmlNode node, string attribute, float defaultValue = 0.0f)
    {
        return float.Parse(GetString(node, attribute, defaultValue.ToString(CultureInfo.InvariantCulture)), 
            CultureInfo.InvariantCulture);
    }

    public static int GetInt(XmlNode node, string attribute, int defaultValue = 0)
    {
        return int.Parse(GetString(node, attribute, defaultValue.ToString(CultureInfo.InvariantCulture)), 
            CultureInfo.InvariantCulture);
    }

    public static Color GetColor(XmlNode node, string attribute)
    {
        return ColorExt.FromHex(GetString(node, attribute, "#ff00dc".ToLowerInvariant()));
    }
    public static Vector2 GetVector2(XmlNode node, string attrX, string attrY)
    {
        var posX = float.Parse(GetString(node, attrX, "0"));
        var posY = float.Parse(GetString(node, attrY, "0"));
        
        return new Vector2(posX, posY);
    }

    public static UiLength ParseLength(string value)
    {
        if (string.IsNullOrEmpty(value))
            return UiLength.Pixels(0);

        value = value.Trim();

        if (value.EndsWith('%'))
        {
            var num = value.Substring(0, value.Length - 1);
            var pct = float.Parse(num, CultureInfo.InvariantCulture) / 100f;
            return UiLength.Percent(pct);
        }

        var px = float.Parse(value, CultureInfo.InvariantCulture);
        return UiLength.Pixels(px);
    }

    public static UiAnchor ParseAnchor(string value)
    {
        return Enum.TryParse<UiAnchor>(value, true, out var anchor)
            ? anchor
            : UiAnchor.TopLeft;
    }
}