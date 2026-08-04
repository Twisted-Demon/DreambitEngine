using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dreambit;

internal static class PropertyConverterRegistry
{
    public static Dictionary<Type, JsonConverter> Converters { get; } = BuildConvertersDictionary();

    public static JsonSerializerSettings CreateSerializerSettings()
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = DreambitAssetContractResolver.Instance
        };

        foreach (var converter in Converters.Values)
            settings.Converters.Add(converter);

        return settings;
    }

    private static Dictionary<Type, JsonConverter> BuildConvertersDictionary()
    {
        var converters = new Dictionary<Type, JsonConverter>();
        var converterTypes = ReflectionUtils.GetAllTypesAssignableFrom(
            typeof(IPropertyConverterMarker),
            true);

        foreach (var type in converterTypes)
        {
            var instance = (JsonConverter)Activator.CreateInstance(type);
            if (instance is null)
                continue;

            var target = GetPropertyConverterTarget(type);
            converters[target] = instance;
        }

        return converters;
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