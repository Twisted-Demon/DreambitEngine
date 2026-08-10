using System;

namespace Dreambit.LDtk.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class LDtkEntityLoaderAttribute : Attribute
{
    public string TargetIdentifier { get; set; }
}
