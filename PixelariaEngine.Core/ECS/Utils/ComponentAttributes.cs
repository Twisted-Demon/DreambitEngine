using System;

namespace PixelariaEngine.ECS;

[AttributeUsage(AttributeTargets.Class)]
public class RequireAttribute(Type type) : Attribute
{
    private Type _type = type;
}