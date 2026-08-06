using System;
using System.Collections.Generic;

namespace Dreambit;

public class BlackboardVar<T> : BlackboardVar
{
    private T _value;

    internal BlackboardVar(T startingValue = default)
    {
        _value = startingValue;
    }

    public event Action<T> ValueChanged;

    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value))
                return;

            _value = value;
            ValueChanged?.Invoke(value);
        }
    }
}

public abstract class BlackboardVar
{
}