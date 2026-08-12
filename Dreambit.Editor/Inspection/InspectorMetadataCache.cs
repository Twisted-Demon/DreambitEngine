using System.Reflection;
using System.Text;
using Dreambit.ECS;
using Newtonsoft.Json;

namespace Dreambit.Editor.Inspection;

internal sealed class InspectorMetadataCache
{
    private const BindingFlags Flags = DreambitSerializationRules.PublicInstanceMembers;

    private readonly Dictionary<(Type Type, InspectorTargetKind Kind), IReadOnlyList<InspectorMemberMetadata>> _cache =
        [];

    public IReadOnlyList<InspectorMemberMetadata> Get(Type type, InspectorTargetKind kind)
    {
        var key = (type, kind);
        if (_cache.TryGetValue(key, out var metadata))
            return metadata;
        metadata = kind == InspectorTargetKind.Component
            ? DiscoverComponent(type)
            : DiscoverAsset(type);
        _cache.Add(key, metadata);
        return metadata;
    }

    public void ReleaseAssembly(Assembly assembly)
    {
        foreach (var key in _cache.Keys.Where(key => key.Type.Assembly == assembly).ToArray())
            _cache.Remove(key);
    }

    public void Clear()
    {
        _cache.Clear();
    }

    private static IReadOnlyList<InspectorMemberMetadata> DiscoverComponent(Type type)
    {
        var members = new List<InspectorMemberMetadata>();
        foreach (var property in type.GetProperties(Flags))
        {
            if (!DreambitSerializationRules.ParticipatesInBlueprintSerialization(property) ||
                property.GetCustomAttribute<HideInInspectorAttribute>() is not null)
                continue;
            members.Add(Create(property.Name, property.Name, property.PropertyType, property,
                property.SetMethod is not null));
        }

        foreach (var field in type.GetFields(Flags))
        {
            if (!DreambitSerializationRules.ParticipatesInBlueprintSerialization(field) ||
                field.GetCustomAttribute<HideInInspectorAttribute>() is not null ||
                members.Any(member => member.SerializedName.Equals(field.Name, StringComparison.OrdinalIgnoreCase)))
                continue;
            members.Add(Create(field.Name, field.Name, field.FieldType, field, !field.IsInitOnly));
        }

        return members.OrderBy(member => member.DisplayName).ToArray();
    }

    private static IReadOnlyList<InspectorMemberMetadata> DiscoverAsset(Type type)
    {
        var members = new List<InspectorMemberMetadata>();
        var serializedMembers = DreambitSerializationRules.GetSerializableMembers(type);
        foreach (var property in serializedMembers.OfType<PropertyInfo>())
        {
            if (property.GetCustomAttribute<HideInInspectorAttribute>() is not null)
                continue;
            var json = property.GetCustomAttribute<JsonPropertyAttribute>();
            if (json is null && property.SetMethod?.IsPublic != true)
                continue;
            var name = DreambitSerializationRules.GetSerializedName(property);
            members.Add(Create(name, property.Name, property.PropertyType, property, property.SetMethod is not null));
        }

        foreach (var field in serializedMembers.OfType<FieldInfo>())
        {
            if (field.GetCustomAttribute<HideInInspectorAttribute>() is not null)
                continue;
            var name = DreambitSerializationRules.GetSerializedName(field);
            if (members.Any(member => member.SerializedName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                continue;
            members.Add(Create(name, field.Name.TrimStart('_'), field.FieldType, field, !field.IsInitOnly));
        }

        return members.OrderBy(member => member.DisplayName).ToArray();
    }

    private static InspectorMemberMetadata Create(
        string serializedName,
        string memberName,
        Type valueType,
        MemberInfo member,
        bool canWrite)
    {
        return new InspectorMemberMetadata(
            serializedName,
            SplitName(memberName),
            valueType,
            member,
            canWrite,
            !canWrite || member.GetCustomAttribute<ReadOnlyInInspectorAttribute>() is not null,
            member.GetCustomAttribute<RangeAttribute>(),
            member.GetCustomAttribute<HeaderAttribute>()?.Text,
            member.GetCustomAttribute<TooltipAttribute>()?.Text);
    }

    private static string SplitName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;
        var result = new StringBuilder(name.Length + 8);
        result.Append(char.ToUpperInvariant(name[0]));
        for (var index = 1; index < name.Length; index++)
        {
            if (char.IsUpper(name[index]) && !char.IsUpper(name[index - 1]))
                result.Append(' ');
            result.Append(name[index]);
        }

        return result.ToString();
    }
}
