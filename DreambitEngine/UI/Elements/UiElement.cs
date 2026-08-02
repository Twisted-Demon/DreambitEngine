using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Dreambit.UI;

/// <summary>
/// Base class for every retained UI node. It provides XML-configurable
/// geometry, two-pass layout, input and drawing hooks, and asset lifecycle.
/// </summary>
public abstract class UiElement
{
    /// <summary>Gets or sets the optional ID used for layout lookup.</summary>
    public string Id;
    /// <summary>Gets or sets the container that owns this element.</summary>
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
    private Point _lastMeasureAvailableSize;
    private bool _hasMeasure;

    /// <summary>Gets or sets the horizontal offset relative to the parent.</summary>
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

    /// <summary>Gets or sets the vertical offset relative to the parent.</summary>
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

    /// <summary>Gets or sets the fixed, percentage, or automatic width.</summary>
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

    /// <summary>Gets or sets the fixed, percentage, or automatic height.</summary>
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

    /// <summary>Gets or sets the reference point on the parent used for positioning.</summary>
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

    /// <summary>Gets or sets the point on this element placed at its anchored position.</summary>
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

    /// <summary>Gets or sets the draw-order value used by the parent container.</summary>
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

    /// <summary>Gets the final rectangle produced by the arrange pass.</summary>
    public Rectangle Bounds;
    /// <summary>Gets the size requested by the most recent measure pass.</summary>
    public Point DesiredSize { get; private set; }

    /// <summary>Gets the child elements owned by this node.</summary>
    public readonly List<UiElement> Children = [];
    

    private bool LayoutDirty { get; set; } = true;
    private bool DependenciesDirty { get; set; } = true;
    

    /// <summary>Marks this element and its descendants for remeasurement and arrangement.</summary>
    public void InvalidateLayout()
    {
        LayoutDirty = true;
        _hasMeasure = false;
        
        foreach(var child in Children)
            child.InvalidateLayout();
    }

    /// <summary>Marks this element's asset dependencies for re-resolution.</summary>
    public void InvalidateDependencies()
    {
        DependenciesDirty = true;
    }

    private static bool LengthsEqual(UiLength left, UiLength right)
    {
        return left.IsAuto == right.IsAuto &&
               left.IsPercent == right.IsPercent &&
               Math.Abs(left.Value - right.Value) < float.Epsilon;
    }

    /// <summary>
    /// Measures this element when necessary and calculates its own bounds
    /// without arranging its children.
    /// </summary>
    /// <param name="parentBounds">The rectangle available from the parent.</param>
    /// <param name="force">Whether to recalculate even when the cached layout is valid.</param>
    protected void ArrangeSelf(Rectangle parentBounds, bool force = false)
    {
        if (!_hasMeasure ||
            _lastMeasureAvailableSize != parentBounds.Size)
        {
            Measure(parentBounds.Size);
        }

        if (!force &&
            !LayoutDirty &&
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

    /// <summary>Calculates the size this element wants within the available space.</summary>
    /// <param name="availableSize">The maximum width and height offered by the parent.</param>
    public void Measure(Point availableSize)
    {
        availableSize = new Point(
            Math.Max(0, availableSize.X),
            Math.Max(0, availableSize.Y));

        var resolvedWidth = Width.Resolve(availableSize.X);
        var resolvedHeight = Height.Resolve(availableSize.Y);
        var contentConstraint = new Point(
            Width.IsAuto ? availableSize.X : resolvedWidth,
            Height.IsAuto ? availableSize.Y : resolvedHeight);
        var measuredContent = MeasureContent(contentConstraint);
        var nextDesiredSize = new Point(
            Width.IsAuto ? Math.Max(0, measuredContent.X) : resolvedWidth,
            Height.IsAuto ? Math.Max(0, measuredContent.Y) : resolvedHeight);

        if (DesiredSize != nextDesiredSize)
            LayoutDirty = true;

        DesiredSize = nextDesiredSize;
        _lastMeasureAvailableSize = availableSize;
        _hasMeasure = true;
    }

    /// <summary>Assigns final bounds to this element and arranges its children.</summary>
    /// <param name="parentBounds">The rectangle available from the parent.</param>
    public virtual void Arrange(Rectangle parentBounds)
    {
        ArrangeSelf(parentBounds, Width.IsAuto || Height.IsAuto);
        // default: arrange children within own bounds
        foreach (var child in Children)
            child.Arrange(Bounds);
    }
    
    private void CalculateBounds(Rectangle parentBounds)
    {
        var resolvedSize = ResolveSize(parentBounds);
        int w = resolvedSize.X;
        int h = resolvedSize.Y;

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

    /// <summary>Resolves this element's configured lengths into a final size.</summary>
    /// <param name="parentBounds">The rectangle available from the parent.</param>
    /// <returns>The resolved size in pixels.</returns>
    protected virtual Point ResolveSize(Rectangle parentBounds)
    {
        return new Point(
            Width.IsAuto
                ? DesiredSize.X
                : Width.Resolve(parentBounds.Width),
            Height.IsAuto
                ? DesiredSize.Y
                : Height.Resolve(parentBounds.Height));
    }

    /// <summary>
    /// Returns the element's natural content size within the supplied
    /// constraint. Custom UI elements only need to override this method to
    /// support width="*" and height="*".
    /// </summary>
    /// <param name="availableSize">The maximum content size offered by the element.</param>
    /// <returns>The content's desired size in pixels.</returns>
    protected virtual Point MeasureContent(Point availableSize)
    {
        return Point.Zero;
    }

    #region Internal Lifecycle

    /// <summary>Resolves dirty asset dependencies throughout this subtree.</summary>
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

    /// <summary>Updates this element and its subtree using the current input snapshot.</summary>
    /// <param name="input">The pointer state for the current frame.</param>
    public void Update(in UiInputState input)
    {
        OnUpdate(input);
    }
    
    
    #endregion

    #region Lifecycle Hooks

    /// <summary>Handles per-frame behavior and updates child elements.</summary>
    /// <param name="input">The pointer state for the current frame.</param>
    protected virtual void OnUpdate(in UiInputState input)
    {
        foreach (var child in Children)
            child.Update(input);
    }

    /// <summary>Draws this element. Containers are responsible for drawing their children.</summary>
    public virtual void OnDraw()
    {
        
    }
    

    #endregion
    

    /// <summary>Parses common attributes before invoking the element-specific parser.</summary>
    /// <param name="node">The XML element that describes this UI element.</param>
    internal void ParseInternal(XmlNode node)
    {
        Id = UiXmlParser.ParseString(node, "id", string.Empty);
        X = UiXmlParser.ParseLength(
            UiXmlParser.ParseString(node, "x", "0%"));
        Y = UiXmlParser.ParseLength(
            UiXmlParser.ParseString(node, "y", "0%"));
        Width = UiXmlParser.ParseLength(
            UiXmlParser.ParseString(node, "width", "100%"));
        Height = UiXmlParser.ParseLength(
            UiXmlParser.ParseString(node, "height", "100%"));
        Anchor = UiXmlParser.ParseAnchor(
            UiXmlParser.ParseString(node, "anchor", "TopLeft"));
        Origin = UiXmlParser.ParseAnchor(
            UiXmlParser.ParseString(node, "origin", "TopLeft"));
        ZIndex = UiXmlParser.ParseInt(node, "z", 0);

        Parse(node);
    }

    /// <summary>Parses attributes that are specific to the derived element.</summary>
    /// <param name="node">The XML element that describes this UI element.</param>
    public virtual void Parse(XmlNode node) { }

    /// <summary>Loads or refreshes external assets used by this element.</summary>
    public virtual void ResolveDependencies() { }
    
}
