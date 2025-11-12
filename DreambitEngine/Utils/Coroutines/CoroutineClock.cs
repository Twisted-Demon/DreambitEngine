namespace Dreambit;

public readonly struct CoroutineClock
{
    public readonly float time; // scaled
    public readonly float deltaTime; // scaled
    public readonly float unscaledTime; // unscaled
    public readonly float unscaledDeltaTime;

    private CoroutineClock(float t, float dt, float ut, float udt)
    {
        time = t;
        deltaTime = dt;
        unscaledTime = ut;
        unscaledDeltaTime = udt;
    }

    // Normal (per-frame) clock from Time
    public static CoroutineClock Now()
    {
        return new CoroutineClock(Time.ScaledTime, Time.DeltaTime, Time.UnscaledTime, Time.UnscaledDeltaTime);
    }

    // Fixed-step clock from Time
    public static CoroutineClock NowFixed()
    {
        return new CoroutineClock(Time.PhysicsTime, Time.PhysicsDeltaTime, Time.UnscaledPhysicsTime,
            Time.UnscaledPhysicsDeltaTime);
    }
}