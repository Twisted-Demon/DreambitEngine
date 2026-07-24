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

        var rootPanel = new UiPanel
        {
            Id = "root",
            X = UiLength.Pixels(0),
            Y = UiLength.Pixels(0),
            Width = UiLength.Pixels(screenSize.X),
            Height = UiLength.Pixels(screenSize.Y),
            Anchor = UiAnchor.TopLeft,
            Origin = UiAnchor.TopLeft
        };

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
                element.ParseInternal(node);
                break;
            case "StackPanel":
                element = new UiStackPanel();
                element.ParseInternal(node);
                break;
            case "Texture":
                element = new UiTexture();
                element.ParseInternal(node);
                break;
            case "Button":
                element = new UiButton();
                element.ParseInternal(node);
                break;
        }
        
        if(element is null) return null;

        element.Parent = parent;

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
    

    

    

    
    

    

    
}