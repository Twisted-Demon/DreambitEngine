namespace Dreambit;

public class BlackboardVar<T> : BlackboardVar
{
    internal BlackboardVar(T startingValue = default)
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