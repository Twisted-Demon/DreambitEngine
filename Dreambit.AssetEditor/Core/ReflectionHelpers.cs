using System.Reflection;
using Dreambit;
using Dreambit.ECS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dreambit.AssetEditor.Core;

internal sealed record SerializableMember(
    string JsonName,
    string DisplayName,
    Type ValueType,
    MemberInfo Member,
    bool IsBlueprintMember = false);

internal static class ReflectionHelpers
{
    public static IReadOnlyList<SerializableMember> GetAssetMembers(Type type)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var members = new List<SerializableMember>();

        foreach (var property in type.GetProperties(flags))
        {
            if (property.GetIndexParameters().Length != 0 || property.GetMethod is null)
                continue;
            if (property.GetCustomAttribute<JsonIgnoreAttribute>() is not null)
                continue;

            var jsonProperty = property.GetCustomAttribute<JsonPropertyAttribute>();
            var hasPublicSetter = property.SetMethod?.IsPublic == true;
            if (jsonProperty is null && !hasPublicSetter)
                continue;

            var jsonName = string.IsNullOrWhiteSpace(jsonProperty?.PropertyName)
                ? property.Name
                : jsonProperty!.PropertyName!;

            members.Add(new SerializableMember(jsonName, SplitName(property.Name), property.PropertyType, property));
        }

        foreach (var field in type.GetFields(flags))
        {
            if (field.IsStatic || field.GetCustomAttribute<JsonIgnoreAttribute>() is not null)
                continue;

            var jsonProperty = field.GetCustomAttribute<JsonPropertyAttribute>();
            if (jsonProperty is null && !field.IsPublic)
                continue;

            var jsonName = string.IsNullOrWhiteSpace(jsonProperty?.PropertyName)
                ? field.Name
                : jsonProperty!.PropertyName!;

            if (members.Any(x => x.JsonName.Equals(jsonName, StringComparison.OrdinalIgnoreCase)))
                continue;

            members.Add(new SerializableMember(jsonName, SplitName(field.Name.TrimStart('_')), field.FieldType, field));
        }

        return members.OrderBy(x => x.DisplayName).ToArray();
    }

    public static IReadOnlyList<SerializableMember> GetBlueprintMembers(Type componentType)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
        var members = new List<SerializableMember>();

        foreach (var property in componentType.GetProperties(flags))
        {
            if (!property.CanWrite || property.GetIndexParameters().Length != 0)
                continue;
            if (property.GetCustomAttribute<DreambitSerializeAttribute>() is null)
                continue;

            members.Add(new SerializableMember(
                property.Name,
                SplitName(property.Name),
                property.PropertyType,
                property,
                true));
        }

        foreach (var field in componentType.GetFields(flags))
        {
            if (field.IsInitOnly || field.IsLiteral)
                continue;
            if (field.GetCustomAttribute<DreambitSerializeAttribute>() is null)
                continue;
            if (members.Any(x => x.JsonName.Equals(field.Name, StringComparison.OrdinalIgnoreCase)))
                continue;

            members.Add(new SerializableMember(field.Name, SplitName(field.Name), field.FieldType, field, true));
        }

        return members.OrderBy(x => x.DisplayName).ToArray();
    }

    public static JToken CreateDefaultToken(Type valueType, object? instance = null, MemberInfo? member = null)
    {
        try
        {
            if (instance is not null && member is not null)
            {
                object? value = member switch
                {
                    PropertyInfo property => property.GetValue(instance),
                    FieldInfo field => field.GetValue(instance),
                    _ => null
                };

                if (value is not null)
                    return DreambitJson.ToToken(value);
            }
        }
        catch
        {
            // Fall through to type defaults.
        }

        var nullable = Nullable.GetUnderlyingType(valueType);
        if (nullable is not null)
            return JValue.CreateNull();

        if (valueType == typeof(string))
            return new JValue(string.Empty);
        if (typeof(DreambitAsset).IsAssignableFrom(valueType))
            return new JValue(string.Empty);
        if (IsDictionary(valueType))
            return new JObject();
        if (valueType.IsArray || IsCollection(valueType))
            return new JArray();

        try
        {
            var value = Activator.CreateInstance(valueType);
            if (value is not null)
            {
                try
                {
                    return DreambitJson.ToToken(value);
                }
                catch
                {
                    var members = GetAssetMembers(valueType);
                    if (members.Count > 0)
                    {
                        var obj = new JObject();
                        foreach (var child in members)
                        {
                            try
                            {
                                object? childValue = child.Member switch
                                {
                                    PropertyInfo property => property.GetValue(value),
                                    FieldInfo field => field.GetValue(value),
                                    _ => null
                                };
                                obj[child.JsonName] = childValue is null
                                    ? CreateShallowDefaultToken(child.ValueType)
                                    : DreambitJson.ToToken(childValue);
                            }
                            catch
                            {
                                obj[child.JsonName] = CreateShallowDefaultToken(child.ValueType);
                            }
                        }
                        return obj;
                    }
                }
            }
        }
        catch
        {
            // Fall through to a shape-based default.
        }

        if (!valueType.IsValueType && GetAssetMembers(valueType).Count > 0)
            return new JObject();

        return JValue.CreateNull();
    }

    private static JToken CreateShallowDefaultToken(Type type)
    {
        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable is not null)
            return JValue.CreateNull();
        if (type == typeof(string))
            return new JValue(string.Empty);
        if (typeof(DreambitAsset).IsAssignableFrom(type))
            return new JValue(string.Empty);
        if (IsDictionary(type))
            return new JObject();
        if (type.IsArray || IsCollection(type))
            return new JArray();
        if (!type.IsValueType)
            return GetAssetMembers(type).Count > 0 ? new JObject() : JValue.CreateNull();
        try
        {
            var value = Activator.CreateInstance(type);
            return value is null ? JValue.CreateNull() : DreambitJson.ToToken(value);
        }
        catch
        {
            return JValue.CreateNull();
        }
    }

    public static bool TryGetCollectionElementType(Type type, out Type elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }

        if (type == typeof(string))
        {
            elementType = null!;
            return false;
        }

        var candidate = type.GetInterfaces().Append(type)
            .FirstOrDefault(x => x.IsGenericType &&
                (x.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                 x.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                 x.GetGenericTypeDefinition() == typeof(IList<>) ||
                 x.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) ||
                 x.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>) ||
                 x.GetGenericTypeDefinition() == typeof(ISet<>)));

        if (candidate is null)
        {
            elementType = null!;
            return false;
        }

        elementType = candidate.GetGenericArguments()[0];
        return true;
    }

    public static bool TryGetDictionaryTypes(Type type, out Type keyType, out Type valueType)
    {
        var candidate = type.GetInterfaces().Append(type)
            .FirstOrDefault(x => x.IsGenericType &&
                (x.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                 x.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>) ||
                 x.GetGenericTypeDefinition() == typeof(Dictionary<,>)));

        if (candidate is null)
        {
            keyType = null!;
            valueType = null!;
            return false;
        }

        var args = candidate.GetGenericArguments();
        keyType = args[0];
        valueType = args[1];
        return true;
    }

    public static bool IsCollection(Type type)
        => type != typeof(string) && type.GetInterfaces().Append(type)
            .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));

    public static bool IsDictionary(Type type)
        => type.GetInterfaces().Append(type).Any(x => x.IsGenericType &&
            (x.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
             x.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));

    public static string ComponentTypeId(Type type)
    {
        var explicitId = type.GetCustomAttribute<BlueprintTypeAttribute>()?.Id;
        if (!string.IsNullOrWhiteSpace(explicitId))
            return explicitId;

        return $"{type.Assembly.GetName().Name}.{type.Name}";
    }

    public static string SplitName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var chars = new List<char>(value.Length + 8) { char.ToUpperInvariant(value[0]) };
        for (var i = 1; i < value.Length; i++)
        {
            if (char.IsUpper(value[i]) && !char.IsUpper(value[i - 1]))
                chars.Add(' ');
            chars.Add(value[i]);
        }
        return new string(chars.ToArray()).Replace('_', ' ');
    }
}
