using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dreambit.ECS;
using Newtonsoft.Json;

namespace Dreambit;

/// <summary>
/// Shared reflection rules for Dreambit JSON, blueprints, and editor inspectors.
/// Reflection is intentionally uncached so collectible game types are not retained.
/// </summary>
public static class DreambitSerializationRules
{
    public const BindingFlags PublicInstanceMembers = BindingFlags.Instance | BindingFlags.Public;

    /// <summary>
    /// Returns whether a type uses explicit Dreambit opt-in member serialization.
    /// Custom Dreambit assets are always opt-in. Nested objects become opt-in when they declare
    /// at least one <see cref="DreambitSerializeAttribute"/> member. Engine-owned assets retain
    /// their legacy public/[JsonProperty] behavior until migrated.
    /// </summary>
    public static bool UsesOptInSerialization(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (typeof(DreambitAsset).IsAssignableFrom(type) &&
            type.Assembly != typeof(DreambitAsset).Assembly)
        {
            return true;
        }

        return GetCandidateMembers(type).Any(IsExplicitlyDreambitSerialized);
    }

    public static bool IsExplicitlyDreambitSerialized(MemberInfo member)
    {
        ArgumentNullException.ThrowIfNull(member);
        return member.GetCustomAttribute<DreambitSerializeAttribute>(true) is not null;
    }

    /// <summary>
    /// Returns whether a member participates in the containing type's Dreambit JSON contract.
    /// Explicit [JsonProperty] remains supported for legacy asset formats.
    /// </summary>
    public static bool ParticipatesInSerialization(Type containingType, MemberInfo member)
    {
        ArgumentNullException.ThrowIfNull(containingType);
        ArgumentNullException.ThrowIfNull(member);

        return ParticipatesInSerialization(
            containingType,
            member,
            UsesOptInSerialization(containingType));
    }

    /// <summary>
    /// Enumerates public members in the effective Dreambit JSON contract without retaining the
    /// reflected Type. Intended for editor metadata and nested-object inspection.
    /// </summary>
    public static IReadOnlyList<MemberInfo> GetSerializableMembers(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var usesOptIn = UsesOptInSerialization(type);
        return GetCandidateMembers(type)
            .Where(member => ParticipatesInSerialization(type, member, usesOptIn))
            .ToArray();
    }

    internal static bool ParticipatesInSerialization(
        Type containingType,
        MemberInfo member,
        bool usesOptInSerialization)
    {
        if (member.GetCustomAttribute<JsonIgnoreAttribute>(true) is not null ||
            IsStatic(member))
        {
            return false;
        }

        var explicitlyIncluded =
            IsExplicitlyDreambitSerialized(member) ||
            member.GetCustomAttribute<JsonPropertyAttribute>(true) is not null;
        if (usesOptInSerialization)
            return explicitlyIncluded && IsPublicReadableWritable(member);

        return explicitlyIncluded || IsPublicDefaultSerializable(member);
    }

    /// <summary>
    /// Returns whether a member is supported by component blueprint serialization: public,
    /// writable, and explicitly marked [DreambitSerialize].
    /// </summary>
    public static bool ParticipatesInBlueprintSerialization(MemberInfo member)
    {
        return IsExplicitlyDreambitSerialized(member) && IsPublicReadableWritable(member);
    }

    public static string GetSerializedName(MemberInfo member)
    {
        ArgumentNullException.ThrowIfNull(member);
        var jsonProperty = member.GetCustomAttribute<JsonPropertyAttribute>(true);
        return string.IsNullOrWhiteSpace(jsonProperty?.PropertyName)
            ? member.Name
            : jsonProperty!.PropertyName!;
    }

    public static IReadOnlyList<string> GetFormerNames(MemberInfo member)
    {
        ArgumentNullException.ThrowIfNull(member);
        return member.GetCustomAttribute<DreambitSerializeAttribute>(true)?.FormerNames
               ?? Array.Empty<string>();
    }

    public static IEnumerable<MemberInfo> GetCandidateMembers(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.GetProperties(PublicInstanceMembers).Cast<MemberInfo>()
            .Concat(type.GetFields(PublicInstanceMembers));
    }

    public static bool IsPublicReadableWritable(MemberInfo member)
    {
        return member switch
        {
            PropertyInfo property =>
                property.GetMethod?.IsPublic == true &&
                property.SetMethod?.IsPublic == true &&
                property.GetIndexParameters().Length == 0,
            FieldInfo field =>
                field.IsPublic && !field.IsStatic && !field.IsInitOnly && !field.IsLiteral,
            _ => false
        };
    }

    private static bool IsPublicDefaultSerializable(MemberInfo member)
    {
        return member switch
        {
            PropertyInfo property =>
                property.GetMethod?.IsPublic == true && property.GetIndexParameters().Length == 0,
            FieldInfo field => field.IsPublic && !field.IsStatic,
            _ => false
        };
    }

    private static bool IsStatic(MemberInfo member)
    {
        return member switch
        {
            PropertyInfo property =>
                property.GetMethod?.IsStatic == true || property.SetMethod?.IsStatic == true,
            FieldInfo field => field.IsStatic,
            _ => false
        };
    }
}
