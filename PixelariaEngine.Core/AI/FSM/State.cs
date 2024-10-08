namespace PixelariaEngine;

public abstract class State
{
    public string Identifier;

    internal bool IsStarted = false;
    public FSM FSM;
    
    public virtual void OnInitialize()
    {
        
    }

    public virtual void OnEnter()
    {
        
    }

    public abstract void OnExecute();

    public virtual void OnEnd()
    {
        
    }
}

public abstract class State<T> : State where T : class
{
    public new string Identifier = typeof(T).Name;
    protected readonly Logger<T> Logger = new();
}


