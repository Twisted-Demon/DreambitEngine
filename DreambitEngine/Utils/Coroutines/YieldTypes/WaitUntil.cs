using System;

namespace Dreambit;

public sealed class WaitUntil : IYieldInstruction
{
    private readonly Func<bool> _predicate;

    public WaitUntil(Func<bool> pred)
    {
        _predicate = pred;
    }

    public bool KeepWaiting(CoroutineClock t)
    {
        return !_predicate();
    }
}