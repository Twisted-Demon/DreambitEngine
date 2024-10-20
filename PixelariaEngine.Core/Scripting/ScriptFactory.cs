using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace PixelariaEngine.Scripting;

internal class ScriptFactory
{
    private static readonly Logger<ScriptFactory> Logger = new();

    internal static ScriptAction CreateScript(Dictionary<string, object> scriptData)
    {
        var className = scriptData["script"].ToString();

        var scriptType = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .FirstOrDefault(t => t.Name == className);

        if (scriptType == null)
        {
            Logger.Warn("Script class {0} not found in current assembly", className);
            return null;
        }

        var constructors = scriptType.GetConstructors();
        foreach (var constructor in constructors)
        {
            var paramInfos = constructor.GetParameters();
            var paramValues = paramInfos.Select(p =>
            {
                if (scriptData.ContainsKey(p.Name))
                {
                    var value = scriptData[p.Name];
                    return ConvertToExpectedType(value, p.ParameterType);
                }
                else if (p.IsOptional)
                {
                    return p.DefaultValue;
                }
                else
                {
                    return GetDefault(p.ParameterType);
                }
            }).ToArray();

            var script = (ScriptAction)Activator.CreateInstance(scriptType, paramValues);

            if (script == null)
            {
                Logger.Warn("Unable to create instance of {0}", className);
                return null;
            }

            return script;
        }

        Logger.Warn("No valid script found in YAML data.");
        return null;
    }
    
    // Helper method to convert types, including arrays
    private static object ConvertToExpectedType(object value, Type targetType)
    {
        if (value == null)
        {
            return null;
        }
        
        // Handle Vector2 conversion
        if (targetType == typeof(Vector2))
        {
            var valueList = value as List<object>;
            if (valueList != null && valueList.Count == 2)
            {
                // Assuming the List<object> contains [x, y] as floats or ints
                float x = Convert.ToSingle(valueList[0]);
                float y = Convert.ToSingle(valueList[1]);
                return new Vector2(x, y);
            }
            throw new Exception("Invalid format for Vector2. Expected [x, y].");
        }
        
        // Handle Vector2 conversion
        if (targetType == typeof(Vector3))
        {
            var valueList = value as List<object>;
            if (valueList != null && valueList.Count == 2)
            {
                // Assuming the List<object> contains [x, y] as floats or ints
                float x = Convert.ToSingle(valueList[0]);
                float y = Convert.ToSingle(valueList[1]);
                float z = Convert.ToSingle(valueList[2]);
                return new Vector3(x, y, z);
            }
            throw new Exception("Invalid format for Vector2. Expected [x, y].");
        }

        // Handle arrays
        if (targetType.IsArray)
        {
            Type elementType = targetType.GetElementType();
            var valueList = value as List<object>;

            if (valueList != null)
            {
                // Convert the List<object> to the appropriate array type
                Array array = Array.CreateInstance(elementType, valueList.Count);
                for (int i = 0; i < valueList.Count; i++)
                {
                    array.SetValue(Convert.ChangeType(valueList[i], elementType), i);
                }
                return array;
            }
        }

        // Handle non-array types
        return Convert.ChangeType(value, targetType);
    }

    // Helper method to get default values for parameter types that may not be present in YAML
    private static object GetDefault(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}