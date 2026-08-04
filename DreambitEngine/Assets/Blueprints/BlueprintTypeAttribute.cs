using System;

namespace Dreambit;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class BlueprintTypeAttribute : Attribute
{
    public BlueprintTypeAttribute(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Blueprint type ID cannot be empty.", nameof(id));

        Id = id;
    }

    public string Id { get; }
}