using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Dreambit;

internal static class PropertyConverterRegistry
{
    private static readonly object Sync = new();
    private static Dictionary<Type, JsonConverter> _converters = BuildConvertersDictionary();

    public static Dictionary<Type, JsonConverter> Converters
    {
        get
        {
            lock (Sync)
                return _converters;
        }
    }

    public static JsonSerializerSettings CreateSerializerSettings()
    {
        var settings = new JsonSerializerSettings
        {
            // DefaultContractResolver caches Type contracts for its lifetime. A fresh resolver
            // keeps collectible game assembly Types scoped to this serializer operation.
            ContractResolver = new DreambitAssetContractResolver()
        };

        lock (Sync)
        {
            foreach (var converter in _converters.Values)
                settings.Converters.Add(converter);
        }

        return settings;
    }

    public static void Rebuild()
    {
        lock (Sync)
            _converters = BuildConvertersDictionary();
    }

    public static bool HasConverter(Type type)
    {
        lock (Sync)
            return _converters.ContainsKey(type);
    }

    internal static void ReleaseAssembly(Assembly assembly)
    {
        lock (Sync)
        {
            _converters = _converters
                .Where(pair =>
                    pair.Key.Assembly != assembly &&
                    pair.Value.GetType().Assembly != assembly)
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }
    }

    private static Dictionary<Type, JsonConverter> BuildConvertersDictionary()
    {
        var converters = new Dictionary<Type, JsonConverter>();
        var converterTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .Where(type =>
                !type.IsAbstract &&
                !type.IsGenericType &&
                typeof(IPropertyConverterMarker).IsAssignableFrom(type) &&
                type.GetConstructor(Type.EmptyTypes) is not null);

        foreach (var type in converterTypes)
        {
            try
            {
                var instance = (JsonConverter)Activator.CreateInstance(type);
                if (instance is null)
                    continue;

                var target = GetPropertyConverterTarget(type);
                converters[target] = instance;
            }
            catch
            {
                // A third-party converter should not prevent the engine from
                // discovering every other converter in the process.
            }
        }

        return converters;
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
        catch
        {
            return Array.Empty<Type>();
        }
    }

    private static Type GetPropertyConverterTarget(Type converterType)
    {
        for (var baseType = converterType;
             baseType != null && baseType != typeof(object);
             baseType = baseType.BaseType)
            if (baseType.IsGenericType &&
                baseType.GetGenericTypeDefinition() == typeof(PropertyConverter<>))
                return baseType.GetGenericArguments()[0];

        throw new ArgumentException(
            $"{converterType} does not inherit PropertyConverter<>.",
            nameof(converterType));
    }
}
