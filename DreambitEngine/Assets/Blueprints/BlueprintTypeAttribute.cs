using System;

namespace Dreambit;

[AttributeUsage(AttributeTargets.Class,  AllowMultiple = false, Inherited = false)]
public sealed class BlueprintTypeAttribute : Attribute
{
    public string Id { get; }
    
    public BlueprintTypeAttribute(string id)
    {
        if(string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Blueprint type ID cannot be empty.", nameof(id));
        
        Id = id;
    }
}