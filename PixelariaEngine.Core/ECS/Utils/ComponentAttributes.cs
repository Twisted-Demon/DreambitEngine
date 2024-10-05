using System;

namespace PixelariaEngine.ECS;

[AttributeUsage(AttributeTargets.Class)]
public class RequireAttribute(Type type) : Attribute
{
    public Type RequiredType = type;
}