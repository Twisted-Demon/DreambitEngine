using Microsoft.Xna.Framework;
using System.Xml;

namespace Dreambit.UI;

/// <summary>
/// Defines a reusable visual that draws inside bounds owned by a UI control.
/// Brush implementations are discovered from loaded assemblies by class name
/// or <see cref="UiXmlNameAttribute"/>.
/// </summary>
public interface IUiBrush
{
    /// <summary>Gets the smallest size at which the brush can render correctly.</summary>
    Point MinimumSize { get; }

    /// <summary>Reads brush-specific values from its XML element.</summary>
    /// <param name="node">The brush XML element.</param>
    void Parse(XmlNode node);

    /// <summary>Loads or refreshes assets required by the brush.</summary>
    void ResolveDependencies();

    /// <summary>Draws the brush within the supplied bounds.</summary>
    /// <param name="bounds">The destination rectangle.</param>
    /// <param name="tint">The color applied by the owning control.</param>
    void Draw(Rectangle bounds, Color tint);
}

/// <summary>
/// Convenient base for visuals drawn inside bounds owned by a UI element.
/// Brushes do not participate in the visual tree and can therefore be reused
/// by borders, buttons, and future controls.
/// </summary>
public abstract class UiBrush : IUiBrush
{
    /// <inheritdoc />
    public virtual Point MinimumSize => Point.Zero;

    /// <inheritdoc />
    public virtual void Parse(XmlNode node) { }

    /// <inheritdoc />
    public virtual void ResolveDependencies() { }

    /// <inheritdoc />
    public abstract void Draw(Rectangle bounds, Color tint);
}
