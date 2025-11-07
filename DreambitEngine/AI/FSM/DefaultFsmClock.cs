using System;

namespace Dreambit;

/// <summary>Clock used to accumulate TimeInState. You can plug in your own.</summary>
public interface IFsmClock
{
    TimeSpan Delta();
}

/// <summary>Default: tries to use Core.GameTime if available; otherwise assumes ~60 FPS.</summary>
public sealed class DefaultFsmClock : IFsmClock
{
    private static readonly TimeSpan _fallback = TimeSpan.FromSeconds(1.0 / 60.0);

    public TimeSpan Delta()
    {
        // If your engine exposes a global GameTime, plug it here.
        // Otherwise this keeps TimeInState roughly correct for debug tooling.
        try
        {
            // Replace with your engine’s actual delta time if you have it:
            // return Core.GameTime?.ElapsedGameTime ?? _fallback;
            return _fallback;
        }
        catch { return _fallback; }
    }
}