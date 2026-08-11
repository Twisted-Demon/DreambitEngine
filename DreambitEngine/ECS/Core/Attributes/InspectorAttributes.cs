using System;

namespace Dreambit;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
public sealed class HideInInspectorAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
public sealed class ReadOnlyInInspectorAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
public sealed class RangeAttribute(double minimum, double maximum) : Attribute
{
    public double Minimum { get; } = minimum;
    public double Maximum { get; } = maximum;
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
public sealed class HeaderAttribute(string text) : Attribute
{
    public string Text { get; } = text;
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
public sealed class TooltipAttribute(string text) : Attribute
{
    public string Text { get; } = text;
}
