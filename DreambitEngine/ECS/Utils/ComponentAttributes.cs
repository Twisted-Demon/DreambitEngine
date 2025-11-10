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