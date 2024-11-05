using System.Collections.Generic;

namespace Dreambit;

public class Blackboard
{
    private readonly Dictionary<string, BlackboardVar> _variables = [];

    public BlackboardVar<T> CreateVariable<T>(string name, T defaultValue = default)
    {
        var bbVar = new BlackboardVar<T>(defaultValue);

        return _variables.TryAdd(name, bbVar) ? bbVar : null;
    }

    public BlackboardVar<T> GetVariable<T>(string variableName)
    {
        if (_variables.TryGetValue(variableName, out var value) && value is BlackboardVar<T> typedValue)
            return typedValue;

        return default;
    }
}