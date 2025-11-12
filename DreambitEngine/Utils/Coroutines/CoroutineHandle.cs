namespace Dreambit;

public struct CoroutineHandle
{
    internal readonly int Id;

    internal CoroutineHandle(int id)
    {
        Id = id;    
    }

    public bool IsValid => Id != 0;
}