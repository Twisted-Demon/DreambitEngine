namespace Dreambit;

public interface IYieldInstruction
{
    bool KeepWaiting(CoroutineClock t);
}