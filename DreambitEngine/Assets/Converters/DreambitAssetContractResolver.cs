using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dreambit;

internal sealed class DreambitAssetContractResolver : DefaultContractResolver
{
    public static DreambitAssetContractResolver Instance { get; } = new();

    protected override JsonProperty CreateProperty(
        MemberInfo member,
        MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        // Direct references:
        // public SoundCue WeaponSound { get; set; }
        if (typeof(DreambitAsset).IsAssignableFrom(property.PropertyType))
        {
            property.Converter ??= DreambitAssetReferenceConverter.Instance;
            return property;
        }

        // Collections:
        // public List<SoundCue> Sounds { get; set; }
        // public SoundCue[] Sounds { get; set; }
        // public Dictionary<string, SoundCue> Sounds { get; set; }
        if (TryGetAssetItemType(property.PropertyType, out _))
        {
            property.ItemConverter ??= DreambitAssetReferenceConverter.Instance;
        }

        return property;
    }

    private static bool TryGetAssetItemType(
        Type containerType,
        out Type assetType)
    {
        if (containerType.IsArray)
        {
            assetType = containerType.GetElementType();

            return assetType != null &&
                   typeof(DreambitAsset).IsAssignableFrom(assetType);
        }

        var candidates = containerType.GetInterfaces()
            .Prepend(containerType);

        // Dictionary values
        var candidatesList = candidates.ToList();
        foreach (var candidate in candidatesList)
        {
            if (!candidate.IsGenericType)
                continue;

            var definition = candidate.GetGenericTypeDefinition();

            if (definition != typeof(IDictionary<,>) &&
                definition != typeof(IReadOnlyDictionary<,>))
            {
                continue;
            }

            assetType = candidate.GetGenericArguments()[1];

            return typeof(DreambitAsset).IsAssignableFrom(assetType);
        }

        // Other collection elements
        foreach (var candidate in candidatesList)
        {
            if (!candidate.IsGenericType ||
                candidate.GetGenericTypeDefinition() != typeof(IEnumerable<>))
            {
                continue;
            }

            assetType = candidate.GetGenericArguments()[0];

            return typeof(DreambitAsset).IsAssignableFrom(assetType);
        }

        assetType = null;
        return false;
    }
}