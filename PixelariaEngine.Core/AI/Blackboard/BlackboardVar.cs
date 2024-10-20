namespace PixelariaEngine;

public class BlackboardVar<T> : BlackboardVar
{
    public BlackboardVar()
    {
        InternalValue = default;
    }

    public BlackboardVar(T startingValue)
    {
        InternalValue = startingValue;
    }

    public T Value
    {
        get => (T)InternalValue;
        set => InternalValue = value;
    }
}

public class BlackboardVar
{
    protected object InternalValue { get; set; }
}