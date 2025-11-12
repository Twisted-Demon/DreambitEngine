namespace Dreambit;

public class WaitForEndOfFrame : IYieldInstruction
{
    internal bool queued;
    
    public bool KeepWaiting(CoroutineClock t)
    {
        return queued;
    }
}