using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Dreambit.ECS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dreambit;

public class BlueprintResolver : Singleton<BlueprintResolver>
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, BlueprintMember>> MemberCache = new();
    private static readonly Dictionary<string, Type> ComponentTypesById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object ComponentRegistryLock = new();
    private static bool _componentRegistryBuilt;

    public Dictionary<Type, JsonConverter> Converters => PropertyConverterRegistry.Converters;

    public static void ResolveComponent(
        ComponentBlueprint blueprint,
        BlueprintSpawnContext context,
        Component component)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(component);

        var componentType = component.GetType();
        var members = GetBlueprintMembers(componentType);
        var errors = new List<Exception>();

        foreach (var (memberName, token) in blueprint.Properties)
        {
            if (!members.TryGetValue(memberName, out var member))
            {
                errors.Add(new InvalidOperationException(
                    $"Component '{componentType.FullName}' has no public writable " +
                    $"property or field named '{memberName}'."));
                continue;
            }

            try
            {
                var value = ConvertJToken(token, member.ValueType, context);
                member.SetValue(component, value);
            }
            catch (Exception exception)
            {
                errors.Add(new InvalidOperationException(
                    $"Could not assign blueprint member " +
                    $"'{componentType.FullName}.{memberName}' from token '{token}'.",
                    exception));
            }
        }

        if (errors.Count > 0)
        {
            throw new AggregateException(
                $"Failed to deserialize component '{componentType.FullName}'.",
                errors);
        }
    }

    internal static bool TryGetBlueprintMemberType(
        Type componentType,
        string memberName,
        out Type memberType)
    {
        var members = GetBlueprintMembers(componentType);
        if (members.TryGetValue(memberName, out var member))
        {
            memberType = member.ValueType;
            return true;
        }

        memberType = null!;
        return false;
    }

    private static IReadOnlyDictionary<string, BlueprintMember> GetBlueprintMembers(Type componentType)
    {
        return MemberCache.GetOrAdd(componentType, static type =>
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            var members = new Dictionary<string, BlueprintMember>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in type.GetProperties(flags))
            {
                if (!property.CanWrite || property.GetIndexParameters().Length != 0)
                    continue;

                members[property.Name] = new BlueprintMember(property);
            }

            foreach (var field in type.GetFields(flags))
            {
                if (field.IsInitOnly || field.IsLiteral)
                    continue;

                // A writable property wins if a field has the same name.
                members.TryAdd(field.Name, new BlueprintMember(field));
            }

            return members;
        });
    }

    public static Type ResolveComponentType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        EnsureComponentTypeRegistry();

        lock (ComponentRegistryLock)
        {
            if (ComponentTypesById.TryGetValue(typeName, out var registeredType))
                return registeredType;
        }

        var resolvedType = Type.GetType(typeName, false, true);
        if (IsValidComponentType(resolvedType))
            return resolvedType;

        // Also support a full type name without an assembly name.
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            resolvedType = assembly.GetType(typeName, false, true);
            if (!IsValidComponentType(resolvedType))
                continue;

            lock (ComponentRegistryLock)
                ComponentTypesById[typeName] = resolvedType;

            return resolvedType;
        }

        return null;
    }

    public static void RebuildComponentTypeRegistry()
    {
        lock (ComponentRegistryLock)
        {
            ComponentTypesById.Clear();
            _componentRegistryBuilt = false;
        }

        EnsureComponentTypeRegistry();
    }

    private static void EnsureComponentTypeRegistry()
    {
        lock (ComponentRegistryLock)
        {
            if (_componentRegistryBuilt)
                return;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in GetLoadableTypes(assembly))
                {
                    if (!IsValidComponentType(type))
                        continue;
                    
                    var logger = new Logger<BlueprintResolver>();

                    var assemblyName = type.Assembly.GetName().Name;
                    var componentName = type.Name;
                    
                    if (!string.IsNullOrWhiteSpace(componentName))
                        RegisterComponentTypeKey($"{assemblyName}.{componentName}", type, false);
                    
                    logger.Trace($"registered: {assemblyName}.{componentName}");
                }
            }

            _componentRegistryBuilt = true;
        }
    }

    private static void RegisterComponentTypeKey(
        string key,
        Type type,
        bool throwOnDuplicate)
    {
        if (ComponentTypesById.TryGetValue(key, out var existingType) && existingType != type)
        {
            if (throwOnDuplicate)
            {
                throw new InvalidOperationException(
                    $"Blueprint component type ID '{key}' is used by both " +
                    $"'{existingType.FullName}' and '{type.FullName}'.");
            }

            return;
        }

        ComponentTypesById[key] = type;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

    private static bool IsValidComponentType(Type type)
    {
        return type != null &&
               !type.IsAbstract &&
               !type.IsGenericType &&
               typeof(Component).IsAssignableFrom(type);
    }

    private static object ConvertJToken(
        JToken token,
        Type targetType,
        BlueprintSpawnContext context)
    {
        if (token.Type is JTokenType.Null or JTokenType.Undefined)
        {
            if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                return null;

            throw new InvalidOperationException(
                $"Cannot assign null to non-nullable type '{targetType.FullName}'.");
        }

        var nullableType = Nullable.GetUnderlyingType(targetType);
        if (nullableType != null)
            return ConvertJToken(token, nullableType, context);

        if (IsDreambitAsset(targetType))
        {
            var assetName = token.Value<string>();
            if (string.IsNullOrWhiteSpace(assetName))
                return null;

            return GetAssetReference(assetName, targetType);
        }

        if (IsEntityReference(targetType))
            return ResolveEntityReference(token, context);

        if (IsComponentReference(targetType))
            return ResolveComponentReference(token, targetType, context);

        if (targetType.IsEnum)
        {
            if (token.Type == JTokenType.String)
                return Enum.Parse(targetType, token.Value<string>()!, true);

            if (token.Type == JTokenType.Integer)
                return Enum.ToObject(targetType, token.Value<long>());
        }

        if (targetType.IsArray)
            return ConvertArray(token, targetType, context);

        if (TryGetDictionaryTypes(targetType, out var keyType, out var valueType))
            return ConvertDictionary(token, targetType, keyType, valueType, context);

        if (TryGetCollectionElementType(targetType, out var elementType))
            return ConvertCollection(token, targetType, elementType, context);

        if (Instance.Converters.TryGetValue(targetType, out var converter))
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(converter);
            var serializer = JsonSerializer.CreateDefault(settings);
            return token.ToObject(targetType, serializer);
        }

        return token.ToObject(targetType);
    }

    private static Array ConvertArray(
        JToken token,
        Type targetType,
        BlueprintSpawnContext context)
    {
        if (token is not JArray jsonArray)
            throw new InvalidOperationException(
                $"Expected a JSON array for '{targetType.FullName}'.");

        var elementType = targetType.GetElementType()!;
        var array = Array.CreateInstance(elementType, jsonArray.Count);

        for (var i = 0; i < jsonArray.Count; i++)
            array.SetValue(ConvertJToken(jsonArray[i], elementType, context), i);

        return array;
    }

    private static object ConvertCollection(
        JToken token,
        Type targetType,
        Type elementType,
        BlueprintSpawnContext context)
    {
        if (token is not JArray jsonArray)
            throw new InvalidOperationException(
                $"Expected a JSON array for '{targetType.FullName}'.");

        var concreteType = GetConcreteCollectionType(targetType, elementType);
        var collection = Activator.CreateInstance(concreteType)
                         ?? throw new InvalidOperationException(
                             $"Could not instantiate collection type '{concreteType.FullName}'.");

        var collectionInterface = typeof(ICollection<>).MakeGenericType(elementType);
        var addMethod = collectionInterface.GetMethod(nameof(ICollection<object>.Add))!;

        foreach (var childToken in jsonArray)
        {
            var value = ConvertJToken(childToken, elementType, context);
            addMethod.Invoke(collection, [value]);
        }

        return collection;
    }

    private static Type GetConcreteCollectionType(Type targetType, Type elementType)
    {
        if (!targetType.IsInterface && !targetType.IsAbstract)
            return targetType;

        if (targetType.IsGenericType &&
            targetType.GetGenericTypeDefinition() == typeof(ISet<>))
        {
            return typeof(HashSet<>).MakeGenericType(elementType);
        }

        return typeof(List<>).MakeGenericType(elementType);
    }

    private static object ConvertDictionary(
        JToken token,
        Type targetType,
        Type keyType,
        Type valueType,
        BlueprintSpawnContext context)
    {
        if (token is not JObject jsonObject)
            throw new InvalidOperationException(
                $"Expected a JSON object for dictionary '{targetType.FullName}'.");

        var concreteType = targetType.IsInterface || targetType.IsAbstract
            ? typeof(Dictionary<,>).MakeGenericType(keyType, valueType)
            : targetType;

        var dictionary = Activator.CreateInstance(concreteType)
                         ?? throw new InvalidOperationException(
                             $"Could not instantiate dictionary type '{concreteType.FullName}'.");

        var dictionaryInterface = typeof(IDictionary<,>).MakeGenericType(keyType, valueType);
        var addMethod = dictionaryInterface.GetMethod(nameof(IDictionary<object, object>.Add))!;

        foreach (var property in jsonObject.Properties())
        {
            var key = ConvertDictionaryKey(property.Name, keyType);
            var value = ConvertJToken(property.Value, valueType, context);
            addMethod.Invoke(dictionary, [key, value]);
        }

        return dictionary;
    }

    internal static object ConvertDictionaryKey(string rawKey, Type keyType)
    {
        var nullableType = Nullable.GetUnderlyingType(keyType);
        if (nullableType != null)
            keyType = nullableType;

        if (keyType == typeof(string))
            return rawKey;

        if (keyType == typeof(Guid))
            return Guid.Parse(rawKey);

        if (keyType.IsEnum)
            return Enum.Parse(keyType, rawKey, true);

        return Convert.ChangeType(rawKey, keyType, CultureInfo.InvariantCulture);
    }

    private static Entity ResolveEntityReference(
        JToken token,
        BlueprintSpawnContext context)
    {
        var blueprintGuid = ParseBlueprintReferenceGuid(token);

        if (context.TryGetEntity(blueprintGuid, out var entity))
            return entity;

        throw new KeyNotFoundException(
            $"Unable to find runtime entity for blueprint GUID '{blueprintGuid}'.");
    }

    private static Component ResolveComponentReference(
        JToken token,
        Type componentType,
        BlueprintSpawnContext context)
    {
        var entity = ResolveEntityReference(token, context);
        var component = entity.GetComponent(componentType);

        if (component != null)
            return component;

        throw new KeyNotFoundException(
            $"Entity '{entity.Name}' does not contain component '{componentType.FullName}'.");
    }

    private static Guid ParseBlueprintReferenceGuid(JToken token)
    {
        var guidString = token.Value<string>();
        if (!Guid.TryParse(guidString, out var blueprintGuid))
        {
            throw new FormatException(
                $"'{guidString}' is not a valid blueprint entity GUID.");
        }

        return blueprintGuid;
    }

    internal static bool IsDreambitAsset(Type type)
    {
        return typeof(DreambitAsset).IsAssignableFrom(type);
    }

    internal static bool IsComponentReference(Type type)
    {
        return typeof(Component).IsAssignableFrom(type);
    }

    internal static bool IsEntityReference(Type type)
    {
        return typeof(Entity).IsAssignableFrom(type);
    }

    internal static bool TryGetCollectionElementType(Type type, out Type elementType)
    {
        if (type == typeof(string) || type.IsArray)
        {
            elementType = null!;
            return false;
        }

        if (type.IsGenericType)
        {
            var genericDefinition = type.GetGenericTypeDefinition();
            if (genericDefinition == typeof(List<>) ||
                genericDefinition == typeof(IList<>) ||
                genericDefinition == typeof(ICollection<>) ||
                genericDefinition == typeof(IReadOnlyCollection<>) ||
                genericDefinition == typeof(IReadOnlyList<>) ||
                genericDefinition == typeof(IEnumerable<>) ||
                genericDefinition == typeof(ISet<>) ||
                genericDefinition == typeof(HashSet<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }
        }

        var collectionInterface = type.GetInterfaces()
            .FirstOrDefault(interfaceType =>
                interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>));

        if (collectionInterface != null)
        {
            elementType = collectionInterface.GetGenericArguments()[0];
            return true;
        }

        elementType = null!;
        return false;
    }

    internal static bool TryGetDictionaryTypes(
        Type type,
        out Type keyType,
        out Type valueType)
    {
        Type dictionaryType = null;

        if (type.IsGenericType)
        {
            var genericDefinition = type.GetGenericTypeDefinition();
            if (genericDefinition == typeof(Dictionary<,>) ||
                genericDefinition == typeof(IDictionary<,>) ||
                genericDefinition == typeof(IReadOnlyDictionary<,>))
            {
                dictionaryType = type;
            }
        }

        if (dictionaryType == null)
        {
            dictionaryType = type.GetInterfaces()
                .FirstOrDefault(interfaceType =>
                    interfaceType.IsGenericType &&
                    (interfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                     interfaceType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));
        }

        if (dictionaryType != null)
        {
            var genericArguments = dictionaryType.GetGenericArguments();
            keyType = genericArguments[0];
            valueType = genericArguments[1];
            return true;
        }

        keyType = null!;
        valueType = null!;
        return false;
    }

    public static object GetAssetReference(string assetName, Type assetType)
    {
        var reference = Resources.LoadDreambitAsset(assetName, assetType);
        if (reference is null)
        {
            Instance.Logger.Warn(
                "Unable to deserialize {0} reference {1}",
                assetType.Name,
                assetName);
        }

        return reference;
    }

    private sealed class BlueprintMember
    {
        private readonly FieldInfo _field;
        private readonly PropertyInfo _property;

        public BlueprintMember(PropertyInfo property)
        {
            _property = property;
            ValueType = property.PropertyType;
        }

        public BlueprintMember(FieldInfo field)
        {
            _field = field;
            ValueType = field.FieldType;
        }

        public Type ValueType { get; }

        public void SetValue(Component component, object value)
        {
            if (_property != null)
                _property.SetValue(component, value);
            else
                _field!.SetValue(component, value);
        }
    }
}
