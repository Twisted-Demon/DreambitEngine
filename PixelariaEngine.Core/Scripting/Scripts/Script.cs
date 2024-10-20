namespace PixelariaEngine.Scripting;

public abstract class Script
{
    public bool IsComplete { get; set; } = false;
    private bool _isStarted = false;

    public abstract void OnUpdate();

    public virtual void OnStart()
    {
        
    }

    public virtual void OnEnd()
    {
        
    }

    internal void Update()
    {
        if (!_isStarted)
        {
            OnStart();
            _isStarted = true;
        }
        
        OnUpdate();

        if (IsComplete)
        {
            OnEnd();
        }
    }
}