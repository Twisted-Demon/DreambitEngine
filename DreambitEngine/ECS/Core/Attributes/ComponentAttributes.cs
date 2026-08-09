using System;

namespace Dreambit.ECS;

[AttributeUsage(AttributeTargets.Class)]
public class RequireAttribute(params Type[] type) : Attribute
{
    public readonly Type[] RequiredTypes = type;
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class FromRequiredAttribute : Attribute
{
}

/// <summary>
///     Marks a component field or property as configurable by Dreambit entity blueprints.
///     Unmarked members are ignored by blueprint deserialization.
/// </summary>
[AttributeUsage(
    AttributeTargets.Field | AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = true)]
public sealed class DreambitSerializeAttribute : Attribute
{
}
