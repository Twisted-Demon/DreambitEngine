namespace Dreambit;

public class CoroutineSubsystem
{
    private readonly CoroutineScheduler _sched = new();

    public ICoroutineService Service => _sched;
    
    public void Update() => _sched.Update();
    
    public void FixedUpdate() => _sched.FixedUpdate();
    
    public void EndOfFrame() => _sched.EndOfFrame();
}