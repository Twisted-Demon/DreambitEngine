using System;

namespace PixelariaEngine.ECS;

[AttributeUsage(AttributeTargets.Class)]
public class RequireAttribute(params Type[] type) : Attribute
{
    public readonly Type[] RequiredTypes = type;
}