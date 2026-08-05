namespace Dreambit.Events;

/// <summary>
///     Base type for all events dispatched through the EventBus.
/// </summary>
public abstract class DreambitEvent
{
}

/// <summary>
///     Defines an event and the exact argument type it accepts.
/// </summary>
/// <typeparam name="TArgs">
///     The argument type delivered to listeners.
/// </typeparam>
public abstract class DreambitEvent<TArgs> : DreambitEvent
    where TArgs : DreambitEventArgs
{
}
