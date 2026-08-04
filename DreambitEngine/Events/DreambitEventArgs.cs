using System;

namespace Dreambit.Events;

/// <summary>
/// Base type for all Dreambit event arguments.
/// </summary>
public abstract class DreambitEventArgs : EventArgs
{
}

/// <summary>
/// Used by events that do not need to carry data.
/// </summary>
public sealed class EmptyDreambitEventArgs : DreambitEventArgs
{
    public static EmptyDreambitEventArgs Instance { get; } = new();

    private EmptyDreambitEventArgs()
    {
    }
}