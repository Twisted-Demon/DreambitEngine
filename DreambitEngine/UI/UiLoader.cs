using System.Collections.Generic;
using System.Xml;

namespace Dreambit.UI;

public static class UiLoader
{
    public static UiLayout LoadFromXml(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var rootNode = doc.SelectSingleNode("/Ui");
        if (rootNode is null)
            throw new XmlException("UI document must contain a <Ui> root.");

        var rootPanel = new UiPanel
        {
            Id = "root",
            X = UiLength.Pixels(0),
            Y = UiLength.Pixels(0),
            Width = UiLength.Percent(1f),
            Height = UiLength.Percent(1f),
            Anchor = UiAnchor.TopLeft,
            Origin = UiAnchor.TopLeft
        };

        foreach (XmlNode child in rootNode.ChildNodes)
        {
            if (child.NodeType != XmlNodeType.Element)
                continue;

            rootPanel.Children.Add(ParseElement(child, rootPanel));
        }

        ValidateUniqueIds(rootPanel, []);

        return new UiLayout
        {
            Root = rootPanel
        };
    }

    private static UiElement ParseElement(
        XmlNode node,
        UiContainer parent)
    {
        UiElement element = node.Name switch
        {
            "Panel" => new UiPanel(),
            "Text" => new UiText(),
            "StackPanel" => new UiStackPanel(),
            "Texture" => new UiTexture(),
            "Button" => new UiButton(),
            _ => throw new XmlException(
                $"Unsupported UI element <{node.Name}>.")
        };

        element.Parent = parent;
        element.ParseInternal(node);

        if (element is UiContainer container)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.NodeType != XmlNodeType.Element)
                    continue;

                container.Children.Add(
                    ParseElement(childNode, container));
            }
        }

        return element;
    }

    private static void ValidateUniqueIds(
        UiElement element,
        HashSet<string> ids)
    {
        if (!string.IsNullOrWhiteSpace(element.Id) &&
            !ids.Add(element.Id))
        {
            throw new XmlException(
                $"Duplicate UI element id '{element.Id}'.");
        }

        foreach (var child in element.Children)
            ValidateUniqueIds(child, ids);
    }
}
