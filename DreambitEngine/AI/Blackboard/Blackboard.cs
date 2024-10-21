using System.Collections.Generic;

namespace Dreambit;

public class Blackboard
{
    public readonly Dictionary<string, BlackboardVar> Variables = [];

    public BlackboardVar<T> CreateVariable<T>(string name, T defaultValue = default)
    {
        var bbVar = new BlackboardVar<T>(defaultValue);

        return Variables.TryAdd(name, bbVar) ? bbVar : null;
    }

    public BlackboardVar<T> GetVariable<T>(string variableName)
    {
        if (Variables.TryGetValue(variableName, out var value) && value is BlackboardVar<T> typedValue)
            return typedValue;

        return default;
    }
}