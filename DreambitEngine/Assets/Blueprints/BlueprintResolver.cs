using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dreambit;

public class BlueprintResolver : Singleton<BlueprintResolver>
{
    public Dictionary<Type, JsonConverter> Converters { get; } = [];

    public BlueprintResolver()
    {
        BuildConvertersDictionary();
    }

    private void BuildConvertersDictionary()
    {
        var converterTypes = 
            ReflectionUtils.GetAllTypesAssignableFrom(
                typeof(IPropertyConverterMarker), 
                onlyIncludeParameterlessConstructors: true);

        foreach (var type in converterTypes)
        {
            var instance = (JsonConverter)Activator.CreateInstance(type);
            if (instance is null) continue;

            // get the target type and store it
            var target = GetPropertyConverterT(type);
            Converters[target] = instance;
        }
    }
    
    private static Type GetPropertyConverterT(Type converterType)
    {
        for (var bt = converterType; bt != null && bt != typeof(object); bt = bt.BaseType)
            if (bt.IsGenericType && bt.GetGenericTypeDefinition() == typeof(PropertyConverter<>))
                return bt.GetGenericArguments()[0];

        throw new ArgumentException($"{converterType} does not inherit PropertyConverter<>");
    }
    

    public static void ResolveComponent(ComponentBlueprint bp, Component component)
    {
        var type = component.GetType();

        foreach (var (propName, token) in bp.Properties)
        {
            var prop = type.GetProperty(propName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            if (prop is null || !prop.CanWrite) continue;

            var value = ConvertJToken(token, prop.PropertyType);
            prop.SetValue(component, value);
        }
    }

    public static Type ResolveComponentType(string typeName)
    {
        var type = Type.GetType(typeName);
        if (type is null || !type.IsSubclassOf(typeof(Component)))
            return null;
        
        return type;
    }

    private static object ConvertJToken(JToken token, Type targetType)
    {
        // Enums
        if (targetType.IsEnum)
        {
            if (token.Type == JTokenType.String)
                return Enum.Parse(targetType, token.Value<string>()!, ignoreCase: true);
            if (token.Type == JTokenType.Integer)
                return Enum.ToObject(targetType, token.Value<int>());
        }
        
        // Nullable<T>
        var underlying = Nullable.GetUnderlyingType(targetType);
        if (underlying != null)
        {
            if (token.Type == JTokenType.Null) return null;
            return ConvertJToken(token, underlying);
        }
        
        // Arrays & Lists
        if (targetType.IsArray)
        {
            var elemType = targetType.GetElementType()!;
            var arr = ((JArray)token).Select(j => ConvertJToken(j, elemType)).ToArray();
            var typedArr = Array.CreateInstance(elemType, arr.Length);
            for (int i = 0; i < arr.Length; i++) typedArr.SetValue(arr[i], i);
            return typedArr;
        }
        
        if (IsGenericList(targetType, out var listElem))
        {
            var jArr = (JArray)token;
            var list = (IList<object>)Activator.CreateInstance(typeof(List<>).MakeGenericType(listElem))!;
            foreach (var j in jArr)
                list.Add(ConvertJToken(j, listElem));
            return list;
        }

        if (Instance.Converters.TryGetValue(targetType, out var converter))
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(converter);
            
            var serializer = JsonSerializer.CreateDefault(settings);
            
            return token.ToObject(targetType, serializer);
        }
        
        
        return token.ToObject(targetType);
    }
    
    private static bool IsGenericList(Type t, out Type elem)
    {
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
        {
            elem = t.GetGenericArguments()[0];
            return true;
        }
        elem = null!;
        return false;
    }
}