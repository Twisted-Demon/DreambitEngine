namespace Dreambit;

public class WaitForFrames : IYieldInstruction
{
    private int _frames;

    public WaitForFrames(int frames)
    {
        _frames = Mathf.MaxInt(0, frames);
    }

    public bool KeepWaiting(CoroutineClock t)
    {
        return _frames-- > 0;
    }
}