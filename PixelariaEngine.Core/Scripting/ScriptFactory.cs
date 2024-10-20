using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PixelariaEngine.Scripting;

internal class ScriptFactory
{
    private static readonly Logger<ScriptFactory> Logger = new();
    
    internal static Script CreateScript(Scene scene, Dictionary<string, object> scriptData)
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
                p.Name != null && scriptData.TryGetValue(p.Name, out var value) ? Convert.ChangeType(value, p.ParameterType) : GetDefault(p.ParameterType)
            ).ToArray();
            
            var script = (Script)Activator.CreateInstance(scriptType, paramValues);

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
    
    // Helper method to get default values for parameter types that may not be present in YAML
    private static object GetDefault(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}