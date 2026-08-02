using System;

namespace Dreambit.UI;

/// <summary>
/// Overrides the XML tag inferred from a UI element or brush class name.
/// Element classes otherwise drop a leading "Ui"; brush classes use their
/// full class name.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class UiXmlNameAttribute : Attribute
{
    /// <summary>Gets the XML tag used to instantiate the annotated type.</summary>
    public string Name { get; }

    /// <summary>Overrides the convention-based XML tag for a UI element or brush.</summary>
    /// <param name="name">The case-sensitive XML tag.</param>
    public UiXmlNameAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A UI XML name is required.", nameof(name));

        Name = name;
    }
}
