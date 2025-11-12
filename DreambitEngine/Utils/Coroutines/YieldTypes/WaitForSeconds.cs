namespace Dreambit;

public sealed class WaitForSeconds : IYieldInstruction
{
    private readonly float _target;
    private bool Started;
    private float StartTime;

    public WaitForSeconds(float seconds, bool useUnscaledTime = false)
    {
        _target = seconds;
        UseUnscaled = useUnscaledTime;
        Started = false;
    }

    public bool UseUnscaled { get; }

    public bool KeepWaiting(CoroutineClock t)
    {
        if (!Started)
        {
            Started = true;
            StartTime = UseUnscaled ? t.unscaledTime : t.time;
        }

        var now = UseUnscaled ? t.unscaledTime : t.time;
        return now - StartTime < _target;
    }
}