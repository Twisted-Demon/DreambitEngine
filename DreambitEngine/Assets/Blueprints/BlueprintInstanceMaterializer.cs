using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace Dreambit;

internal static class BlueprintInstanceMaterializer
{
    public static IReadOnlyList<EntityBlueprint> Materialize(
        IEnumerable<EntityBlueprint> roots,
        Func<BlueprintInstanceReference, EntityBlueprint> resolver)
    {
        ArgumentNullException.ThrowIfNull(roots);
        ArgumentNullException.ThrowIfNull(resolver);

        var dependencyStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return roots
            .Select(root => MaterializeEntity(CloneInline(root), resolver, dependencyStack))
            .ToArray();
    }

    private static EntityBlueprint MaterializeEntity(
        EntityBlueprint entity,
        Func<BlueprintInstanceReference, EntityBlueprint> resolver,
        HashSet<string> dependencyStack)
    {
        if (entity.BlueprintInstance is not { } instance)
        {
            for (var index = 0; index < entity.Children.Count; index++)
                entity.Children[index] = MaterializeEntity(entity.Children[index], resolver, dependencyStack);
            return entity;
        }

        var dependencyKey = instance.AssetId != Guid.Empty
            ? instance.AssetId.ToString("D")
            : instance.AssetName;
        if (string.IsNullOrWhiteSpace(dependencyKey))
            throw new InvalidOperationException("A boxed Blueprint instance has no source asset.");
        if (!dependencyStack.Add(dependencyKey))
            throw new InvalidOperationException(
                $"Circular Blueprint instance dependency detected for '{instance.AssetName}'.");

        try
        {
            var source = resolver(instance)
                         ?? throw new InvalidOperationException(
                             $"Could not resolve Blueprint instance '{instance.AssetName}'.");
            var clone = CloneInline(source);
            RemapHierarchy(clone, entity.Guid);

            // The instance owns its root placement. All authored Blueprint data, including the
            // root name, tags, enabled state, components, and children, remains linked to source.
            clone.Position = entity.Position;
            clone.Rotation = entity.Rotation;
            clone.Scale = entity.Scale;

            if (clone.BlueprintInstance is not null)
                return MaterializeEntity(clone, resolver, dependencyStack);
            for (var index = 0; index < clone.Children.Count; index++)
                clone.Children[index] = MaterializeEntity(clone.Children[index], resolver, dependencyStack);
            return clone;
        }
        finally
        {
            dependencyStack.Remove(dependencyKey);
        }
    }

    private static EntityBlueprint CloneInline(EntityBlueprint source)
    {
        // The root object is deliberately serialized inline. Nested DreambitAsset references keep
        // their normal reference representation through DreambitJson's contract resolver.
        return DreambitJson.Deserialize<EntityBlueprint>(DreambitJson.Serialize(source))
               ?? throw new InvalidOperationException("Could not clone a Blueprint instance source.");
    }

    private static void RemapHierarchy(EntityBlueprint root, Guid instanceRootId)
    {
        if (instanceRootId == Guid.Empty)
            instanceRootId = Guid.NewGuid();

        var hierarchy = root.FlattenedHierarchy().ToArray();
        var sourceRootId = root.Guid;
        var remap = hierarchy.ToDictionary(
            entity => entity.Guid,
            entity => entity.Guid == sourceRootId
                ? instanceRootId
                : CreateDeterministicEntityId(instanceRootId, entity.Guid));

        foreach (var entity in hierarchy)
        {
            entity.Guid = remap[entity.Guid];
            foreach (var component in entity.Components)
                foreach (var key in component.Properties.Keys.ToArray())
                    component.Properties[key] = RemapReferences(component.Properties[key], remap);
        }
    }

    private static Guid CreateDeterministicEntityId(Guid instanceRootId, Guid sourceEntityId)
    {
        Span<byte> input = stackalloc byte[32];
        instanceRootId.TryWriteBytes(input[..16]);
        sourceEntityId.TryWriteBytes(input[16..]);
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(input, hash);
        // Mark the generated value as an RFC 4122 version-5-style UUID while retaining the
        // deterministic SHA-256 payload.
        hash[6] = (byte)((hash[6] & 0x0f) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);
        return new Guid(hash[..16]);
    }

    private static JToken RemapReferences(JToken token, IReadOnlyDictionary<Guid, Guid> remap)
    {
        if (token.Type == JTokenType.String &&
            Guid.TryParse(token.Value<string>(), out var oldId) &&
            remap.TryGetValue(oldId, out var newId))
        {
            return new JValue(newId.ToString());
        }

        var clone = token.DeepClone();
        if (clone is JContainer container)
            foreach (var child in container.Descendants().OfType<JValue>())
                if (child.Type == JTokenType.String &&
                    Guid.TryParse(child.Value<string>(), out oldId) &&
                    remap.TryGetValue(oldId, out newId))
                {
                    child.Value = newId.ToString();
                }
        return clone;
    }
}
