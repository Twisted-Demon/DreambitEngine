using System;
using System.Collections.Generic;
using System.Linq;

namespace Dreambit;

/// <summary>
/// Declares the durable serialized identity of a <see cref="DreambitAsset"/> type.
/// The ID is independent of the CLR type, namespace, assembly name, and assembly version.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DreambitAssetTypeAttribute : Attribute
{
    public DreambitAssetTypeAttribute(string id, params string[] formerIds)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Dreambit asset type ID cannot be empty.", nameof(id));

        Id = id.Trim();
        FormerIds = Array.AsReadOnly((formerIds ?? [])
            .Select(formerId =>
            {
                if (string.IsNullOrWhiteSpace(formerId))
                    throw new ArgumentException(
                        "Former Dreambit asset type IDs cannot be empty.",
                        nameof(formerIds));

                return formerId.Trim();
            })
            .ToArray());

        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Id };
        foreach (var formerId in FormerIds)
            if (!identities.Add(formerId))
                throw new ArgumentException(
                    $"Dreambit asset type identity '{formerId}' is declared more than once.",
                    nameof(formerIds));
    }

    /// <summary>The current canonical ID written to asset documents.</summary>
    public string Id { get; }

    /// <summary>Previous IDs accepted while loading older asset documents.</summary>
    public IReadOnlyList<string> FormerIds { get; }
}
