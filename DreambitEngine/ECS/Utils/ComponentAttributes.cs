using System;

namespace Dreambit.ECS;

[AttributeUsage(AttributeTargets.Class)]
public class RequireAttribute(params Type[] type) : Attribute
{
    public readonly Type[] RequiredTypes = type;
}