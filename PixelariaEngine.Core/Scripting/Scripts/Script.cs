namespace PixelariaEngine.Scripting;

public abstract class Script
{
    private bool _isStarted;
    public bool IsComplete { get; set; } = false;

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

        if (IsComplete) OnEnd();
    }
}