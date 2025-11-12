using System;

namespace Dreambit;

public class WaitWhile : IYieldInstruction
{
    private readonly Func<bool> _predicate;

    public WaitWhile(Func<bool> predicate)
    {
        _predicate = predicate;
    }
    
    public bool KeepWaiting(CoroutineClock t)
    {
        return _predicate();
    }
}