namespace Dreambit.Scripting;

public abstract class ScriptAction
{
    internal bool IsStarted;
    public bool IsComplete { get; set; } = false;

    /// <summary>
    ///     Called every frame
    /// </summary>
    public abstract void OnUpdate();

    /// <summary>
    ///     Called once when this script has started, before the first update
    /// </summary>
    public virtual void OnStart()
    {
    }

    /// <summary>
    ///     Called once when the script is completed, before the script group has ended
    /// </summary>
    public virtual void OnCompleted()
    {
    }

    /// <summary>
    ///     Called once after all scripts in the current group have ended
    /// </summary>
    public virtual void OnGroupEnd()
    {
    }

    internal void Update()
    {
        if (!IsStarted)
        {
            OnStart();
            IsStarted = true;
        }

        OnUpdate();

        if (IsComplete)
            OnCompleted();
    }
}