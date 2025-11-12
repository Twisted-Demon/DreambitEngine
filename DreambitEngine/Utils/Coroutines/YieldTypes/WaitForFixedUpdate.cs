namespace Dreambit;

public class WaitForFixedUpdate : IYieldInstruction
{
    internal bool pending;
    
    public bool KeepWaiting(CoroutineClock t)
    {
        return pending;
    }
}