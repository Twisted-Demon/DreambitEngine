namespace PixelariaEngine;

public class BlackboardVar<T> : BlackboardVar
{
    public T Value
    {
        get => (T)InternalValue;
        set => InternalValue = value;
    }

    public BlackboardVar()
    {
        InternalValue = default;
    }

    public BlackboardVar(T startingValue)
    {
        InternalValue = startingValue;
    }
}

public class BlackboardVar
{
    protected object InternalValue { get; set; }
}