using System;

namespace Dreambit;

public struct TransitionEdge(Type to, Func<FSM, bool> guard)
{
    public readonly Type To = to;
    public readonly Func<FSM, bool> Guard = guard;
}