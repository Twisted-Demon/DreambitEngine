using System;

namespace Dreambit.Networking.Replication;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class NetworkReplicatedAttribute : Attribute
{
    public NetworkReplicatedAttribute(ushort componentId)
    {
        if (componentId == 0)
            throw new ArgumentOutOfRangeException(nameof(componentId));
        ComponentId = componentId;
    }

    public ushort ComponentId { get; }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
public sealed class ReplicatedAttribute : Attribute
{
    public ReplicatedAttribute(ushort fieldId)
    {
        if (fieldId == 0)
            throw new ArgumentOutOfRangeException(nameof(fieldId));
        FieldId = fieldId;
    }

    public ushort FieldId { get; }

    /// <summary>Maximum UTF-8 bytes for string values.</summary>
    public int MaxLength { get; set; } = 256;
}
