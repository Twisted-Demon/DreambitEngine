using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

public abstract class UiElement
{
    public string Id;
    public UiContainer Parent;
    
    private UiLength _x = UiLength.Pixels(0);
    private UiLength _y = UiLength.Pixels(0);
    private UiLength _width = UiLength.Pixels(0);
    private UiLength _height = UiLength.Pixels(0);
    private UiAnchor _anchor = UiAnchor.TopLeft;
    private UiAnchor _origin = UiAnchor.TopLeft;
    private int _zIndex = 0;
    private Rectangle _lastParentBounds;
    private bool _hasParentBounds;

    public UiLength X
    {
        get => _x;
        set
        {
            if (LengthsEqual(_x, value)) return;

            _x = value;
            InvalidateLayout();
        }
    }

    public UiLength Y
    {
        get => _y;
        set
        {
            if (LengthsEqual(_y, value)) return;

            _y = value;
            InvalidateLayout();
        }
    }

    public UiLength Width
    {
        get => _width;
        set 
        {
            if (LengthsEqual(_width, value)) return;

            _width = value;
            InvalidateLayout();
        }
    }

    public UiLength Height
    {
        get => _height;
        set
        {
            if (LengthsEqual(_height, value)) return;

            _height = value;
            InvalidateLayout();
        }
    }

    public UiAnchor Anchor
    {
        get => _anchor;
        set
        {
            if (_anchor == value) return;

            _anchor = value;
            InvalidateLayout();
        }
    }

    public UiAnchor Origin
    {
        get => _origin;
        set
        {
            if (_origin == value) return;

            _origin = value;
            InvalidateLayout();
        }
    }

    public int ZIndex
    {
        get => _zIndex;
        set
        {
            if (_zIndex == value) return;

            _zIndex = value;
            InvalidateLayout();
        }
    }

    public Rectangle Bounds;

    public readonly List<UiElement> Children = [];
    

    private bool LayoutDirty { get; set; } = true;
    private bool DependenciesDirty { get; set; } = true;
    

    public void InvalidateLayout()
    {
        LayoutDirty = true;
        
        foreach(var child in Children)
            child.InvalidateLayout();
    }

    public void InvalidateDependencies()
    {
        DependenciesDirty = true;
    }

    private static bool LengthsEqual(UiLength left, UiLength right)
    {
        return left.IsPercent == right.IsPercent &&
               Math.Abs(left.Value - right.Value) < float.Epsilon;
    }

    protected void ArrangeSelf(Rectangle parentBounds)
    {
        if (!LayoutDirty &&
            _hasParentBounds &&
            _lastParentBounds == parentBounds)
        {
            return;
        }
        
        CalculateBounds(parentBounds);
        _lastParentBounds = parentBounds;
        _hasParentBounds = true;
        LayoutDirty = false;
    }

    public virtual void Arrange(Rectangle parentBounds)
    {
        ArrangeSelf(parentBounds);
        // default: arrange children within own bounds
        foreach (var child in Children)
            child.Arrange(Bounds);
    }
    
    private void CalculateBounds(Rectangle parentBounds)
    {
        int w = Width.Resolve(parentBounds.Width);
        int h = Height.Resolve(parentBounds.Height);

        int x = X.Resolve(parentBounds.Width);
        int y = Y.Resolve(parentBounds.Height);

        // anchor offset
        int offsetX = 0;
        int offsetY = 0;

        switch (Anchor)
        {
            case UiAnchor.TopLeft:
                offsetX = 0;
                offsetY = 0;
                break;
            case UiAnchor.TopCenter:
                offsetX = parentBounds.Width / 2;
                offsetY = 0;
                break;
            case UiAnchor.TopRight:
                offsetX = parentBounds.Width;
                offsetY = 0;
                break;
            case UiAnchor.CenterLeft:
                offsetX = 0;
                offsetY = parentBounds.Height / 2;
                break;
            case UiAnchor.Center:
                offsetX = parentBounds.Width / 2;
                offsetY = parentBounds.Height / 2;
                break;
            case UiAnchor.CenterRight:
                offsetX = parentBounds.Width;
                offsetY = parentBounds.Height / 2;
                break;
            case UiAnchor.BottomLeft:
                offsetX = 0;
                offsetY = parentBounds.Height;
                break;
            case UiAnchor.BottomCenter:
                offsetX = parentBounds.Width / 2;
                offsetY = parentBounds.Height;
                break;
            case UiAnchor.BottomRight:
                offsetX = parentBounds.Width;
                offsetY = parentBounds.Height;
                break;
        }

        switch (Origin)
        {
            case UiAnchor.TopLeft:
                break;
            case UiAnchor.TopCenter:
                offsetX -= (w / 2);
                break;
            case UiAnchor.TopRight:
                offsetX -= w;
                break;
            case UiAnchor.CenterLeft:
                offsetY -= (h / 2);
                break;
            case UiAnchor.Center:
                offsetX -= (w / 2);
                offsetY -= (h / 2);
                break;
            case UiAnchor.CenterRight:
                offsetX -= w;
                offsetY -= (h / 2);
                break;
            case UiAnchor.BottomLeft:
                offsetY -= h;
                break;
            case UiAnchor.BottomCenter:
                offsetX -= (w / 2);
                offsetY -= h;
                break;
            case UiAnchor.BottomRight:
                offsetX -= w;
                offsetY -= h;
                break;
        }

        int screenX = parentBounds.X + x + offsetX;
        int screenY = parentBounds.Y + y + offsetY;

        Bounds = new Rectangle(screenX, screenY, w, h);
    }

    #region Internal Lifecycle

    internal void ResolveDependenciesRecursive()
    {
        if (DependenciesDirty)
        {
            ResolveDependencies();
            DependenciesDirty = false;
        }

        foreach (var child in Children)
            child.ResolveDependenciesRecursive();
    }

    public void Update(in UiInputState input)
    {
        OnUpdate(input);
    }
    
    
    #endregion

    #region Lifecycle Hooks

    protected virtual void OnUpdate(in UiInputState input)
    {
        foreach (var child in Children)
            child.Update(input);
    }

    public virtual void OnDraw()
    {
        
    }
    

    #endregion
    

    internal void ParseInternal(XmlNode node)
    {
        Id = ParseString(node, "id", string.Empty);
        X = ParseLength(ParseString(node, "x", "0%"));
        Y = ParseLength(ParseString(node, "y", "0%"));
        Width = ParseLength(ParseString(node, "width", "100%"));
        Height = ParseLength(ParseString(node, "height", "100%"));
        Anchor = ParseAnchor(ParseString(node, "anchor", "TopLeft"));
        Origin = ParseAnchor(ParseString(node, "origin", "TopLeft"));
        ZIndex = ParseInt(node, "z", 0);

        Parse(node);
    }
    
    public virtual void Parse(XmlNode node) { }

    public virtual void ResolveDependencies() { }
    
    protected static string ParseString(XmlNode node, string name, string defaultValue)
    {
        if (node.Attributes == null) return string.Empty;
        
        var attr = node.Attributes[name];
        return attr != null ? attr.Value : defaultValue;

    }
    
    protected static float ParseFloat(XmlNode node, string attribute, float defaultValue = 0.0f)
    {
        return float.Parse(ParseString(node, attribute, defaultValue.ToString(CultureInfo.InvariantCulture)), 
            CultureInfo.InvariantCulture);
    }
    
    protected static int ParseInt(XmlNode node, string attribute, int defaultValue = 0)
    {
        return int.Parse(ParseString(node, attribute, defaultValue.ToString(CultureInfo.InvariantCulture)), 
            CultureInfo.InvariantCulture);
    }

    protected static bool ParseBool(XmlNode node, string attribute, bool defaultValue = false)
    {
        return bool.Parse(ParseString(node, attribute, defaultValue.ToString(CultureInfo.InvariantCulture)));
    }
    
    protected static Color ParseColor(XmlNode node, string attribute)
    {
        return ColorExt.FromHex(ParseString(node, attribute, "#ff00dc".ToLowerInvariant()));
    }
    
    protected static Vector2 ParseVector2(XmlNode node, string attrX, string attrY)
    {
        var posX = float.Parse(ParseString(node, attrX, "0"));
        var posY = float.Parse(ParseString(node, attrY, "0"));
        
        return new Vector2(posX, posY);
    }
    
    protected static UiLength ParseLength(string value)
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
    
    protected static UiAnchor ParseAnchor(string value)
    {
        return Enum.TryParse<UiAnchor>(value, true, out var anchor)
            ? anchor
            : UiAnchor.TopLeft;
    }
}
