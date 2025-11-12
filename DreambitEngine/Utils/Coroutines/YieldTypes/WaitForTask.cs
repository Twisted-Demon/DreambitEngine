using System.Threading.Tasks;

namespace Dreambit;

public sealed class WaitForTask : IYieldInstruction
{
    private readonly Task _task;

    public WaitForTask(Task task)
    {
        _task = task;
    }
    
    public bool KeepWaiting(CoroutineClock t)
    {
        return !_task.IsCompleted;
    }
}