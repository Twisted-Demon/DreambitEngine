using System.Collections;
using System.Reflection;
using Dreambit.ECS;
using Newtonsoft.Json.Linq;

namespace Dreambit.Editor.Scenes;

internal static class SceneDocumentSerializer
{
    private const BindingFlags SerializableMemberFlags = BindingFlags.Instance | BindingFlags.Public;

    public static SceneBlueprint Deserialize(string json) =>
        DreambitJson.Deserialize<SceneBlueprint>(json)
        ?? throw new InvalidDataException("The scene file did not contain a Dreambit scene.");

    public static string Serialize(SceneBlueprint blueprint) => DreambitJson.Serialize(blueprint);

    public static SceneBlueprint Capture(
        Scene scene,
        SceneBlueprint source,
        string sceneName,
        IReadOnlySet<string>? explicitlyClearedReferences = null)
    {
        scene.FlushStructuralChanges();
        var sourceEntities = source.Entities
            .SelectMany(root => root.FlattenedHierarchy())
            .ToDictionary(entity => entity.Guid);
        var roots = scene.GetAllEntities()
            .Where(entity => entity.Parent is null && !entity.IsEditorOnly)
            .Select(entity => CaptureEntity(entity, sourceEntities, explicitlyClearedReferences))
            .ToList();

        return new SceneBlueprint
        {
            Name = sceneName,
            Entities = roots
        };
    }

    public static EntityBlueprint CaptureSubtree(Scene scene, SceneBlueprint source, Entity entity)
    {
        scene.FlushStructuralChanges();
        var sourceEntities = source.Entities
            .SelectMany(root => root.FlattenedHierarchy())
            .ToDictionary(item => item.Guid);
        return CaptureEntity(entity, sourceEntities, null);
    }

    public static EntityBlueprint CloneAndRemap(EntityBlueprint source)
    {
        var clone = DreambitJson.Deserialize<EntityBlueprint>(DreambitJson.Serialize(source))
                    ?? throw new InvalidOperationException("Could not duplicate the entity blueprint.");
        var remap = clone.FlattenedHierarchy()
            .ToDictionary(entity => entity.Guid, _ => Guid.NewGuid());

        foreach (var entity in clone.FlattenedHierarchy())
        {
            entity.Guid = remap[entity.Guid];
            foreach (var component in entity.Components)
                foreach (var key in component.Properties.Keys.ToArray())
                    component.Properties[key] = RemapReferences(component.Properties[key], remap);
        }

        clone.Name = MakeCopyName(clone.Name);
        return clone;
    }

    private static EntityBlueprint CaptureEntity(
        Entity entity,
        IReadOnlyDictionary<Guid, EntityBlueprint> sourceEntities,
        IReadOnlySet<string>? explicitlyClearedReferences)
    {
        sourceEntities.TryGetValue(entity.Id, out var source);
        var sourceComponents = source?.Components ?? [];
        var liveComponents = entity.GetAllComponents().ToArray();
        var capturedComponents = new List<ComponentBlueprint>(liveComponents.Length + sourceComponents.Count);

        foreach (var component in liveComponents)
        {
            var componentType = component.GetType();
            var original = sourceComponents.FirstOrDefault(candidate =>
                BlueprintResolver.ResolveComponentType(candidate.Type) == componentType);
            var properties = original is null
                ? new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase)
                : original.Properties.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.DeepClone(),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var member in GetBlueprintMembers(componentType))
            {
                var value = member switch
                {
                    PropertyInfo property => property.GetValue(component),
                    FieldInfo field => field.GetValue(component),
                    _ => null
                };
                var valueType = member switch
                {
                    PropertyInfo property => property.PropertyType,
                    FieldInfo field => field.FieldType,
                    _ => typeof(object)
                };
                var referenceKey = GetReferenceKey(entity.Id, componentType, member.Name);
                if (component.EditorSerializationFailures.Contains(member.Name) &&
                    properties.ContainsKey(member.Name))
                    continue;
                var isReference = typeof(DreambitAsset).IsAssignableFrom(valueType) ||
                                  valueType == typeof(Entity) ||
                                  typeof(Component).IsAssignableFrom(valueType);
                if (value is null && isReference && properties.ContainsKey(member.Name) &&
                    explicitlyClearedReferences?.Contains(referenceKey) != true)
                    continue;
                properties[member.Name] = SerializeValue(value, valueType);
            }

            capturedComponents.Add(new ComponentBlueprint
            {
                Type = original?.Type ?? GetComponentTypeId(componentType),
                Enabled = component.Enabled,
                Properties = properties
            });
        }

        // Unknown game components remain as untouched JSON until their assembly returns.
        foreach (var missing in sourceComponents)
        {
            var resolvedType = BlueprintResolver.ResolveComponentType(missing.Type);
            if (resolvedType is not null && liveComponents.Any(component => component.GetType() == resolvedType))
                continue;
            capturedComponents.Add(CloneComponent(missing));
        }

        return new EntityBlueprint
        {
            Name = entity.Name,
            Guid = entity.Id,
            Tags = new HashSet<string>(entity.Tags, StringComparer.OrdinalIgnoreCase),
            Enabled = entity.LocallyEnabled,
            Position = entity.Transform.Position,
            Rotation = new Microsoft.Xna.Framework.Vector3(0f, 0f, entity.Transform.Rotation2D),
            Scale = entity.Transform.Scale,
            Components = capturedComponents,
            Children = entity.Children
                .Where(child => !child.IsEditorOnly)
                .Select(child => CaptureEntity(child, sourceEntities, explicitlyClearedReferences))
                .ToList()
        };
    }

    private static IEnumerable<MemberInfo> GetBlueprintMembers(Type type)
    {
        foreach (var property in type.GetProperties(SerializableMemberFlags))
            if (property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0 &&
                property.GetCustomAttribute<DreambitSerializeAttribute>() is not null)
                yield return property;

        foreach (var field in type.GetFields(SerializableMemberFlags))
            if (!field.IsInitOnly && !field.IsLiteral &&
                field.GetCustomAttribute<DreambitSerializeAttribute>() is not null)
                yield return field;
    }

    private static JToken SerializeValue(object? value, Type declaredType)
    {
        if (value is null)
            return JValue.CreateNull();
        if (value is DreambitAsset asset)
            return asset.AssetId.IsEmpty
                ? new JValue(asset.AssetName ?? string.Empty)
                : DreambitAssetReferenceToken.Create(asset.AssetId, asset.AssetName);
        if (value is Entity entity)
            return new JValue(entity.Id.ToString());
        if (value is Component component)
            return new JValue(component.Entity.Id.ToString());
        if (value is IDictionary dictionary)
        {
            var valueType = declaredType.IsGenericType
                ? declaredType.GetGenericArguments()[1]
                : typeof(object);
            var result = new JObject();
            foreach (DictionaryEntry entry in dictionary)
                result[Convert.ToString(entry.Key) ?? string.Empty] = SerializeValue(entry.Value, valueType);
            return result;
        }
        if (value is IEnumerable sequence && value is not string)
        {
            var elementType = declaredType.IsArray
                ? declaredType.GetElementType() ?? typeof(object)
                : declaredType.IsGenericType
                    ? declaredType.GetGenericArguments()[0]
                    : typeof(object);
            var result = new JArray();
            foreach (var item in sequence)
                result.Add(SerializeValue(item, elementType));
            return result;
        }

        return DreambitJson.ToToken(value);
    }

    private static string GetComponentTypeId(Type type)
    {
        var explicitId = type.GetCustomAttribute<BlueprintTypeAttribute>()?.Id;
        return string.IsNullOrWhiteSpace(explicitId)
            ? $"{type.Assembly.GetName().Name}.{type.Name}"
            : explicitId;
    }

    private static ComponentBlueprint CloneComponent(ComponentBlueprint source) => new()
    {
        Type = source.Type,
        Enabled = source.Enabled,
        Properties = source.Properties.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.DeepClone(),
            StringComparer.OrdinalIgnoreCase)
    };

    public static string GetReferenceKey(Guid entityId, Type componentType, string memberName) =>
        $"{entityId:N}|{componentType.AssemblyQualifiedName}|{memberName}";

    private static JToken RemapReferences(JToken token, IReadOnlyDictionary<Guid, Guid> remap)
    {
        if (token.Type == JTokenType.String &&
            Guid.TryParse(token.Value<string>(), out var oldId) &&
            remap.TryGetValue(oldId, out var newId))
            return new JValue(newId.ToString());

        var clone = token.DeepClone();
        if (clone is JContainer container)
            foreach (var child in container.Descendants().OfType<JValue>())
                if (child.Type == JTokenType.String &&
                    Guid.TryParse(child.Value<string>(), out oldId) &&
                    remap.TryGetValue(oldId, out newId))
                    child.Value = newId.ToString();
        return clone;
    }

    private static string MakeCopyName(string name) =>
        name.EndsWith(" Copy", StringComparison.OrdinalIgnoreCase) ? name + " 2" : name + " Copy";
}
