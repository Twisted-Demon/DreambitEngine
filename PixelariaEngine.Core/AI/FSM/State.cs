using PixelariaEngine.ECS;

namespace PixelariaEngine;

public abstract class State
{
    public string Identifier { get; internal set; }

    internal bool IsStarted = false;
    protected Blackboard Blackboard => Fsm.Blackboard;
    public FSM Fsm;
    public Scene Scene => Fsm?.Scene;
    public Transform Transform => Fsm?.Transform;
    
    public virtual void OnInitialize()
    {
        
    }

    public virtual void OnEnter()
    {
        
    }
    
    /// <summary>
    /// Here is where we should check if we should switch states
    /// true for we should continue to run the current state
    /// false for we should be switching states
    /// </summary>
    /// <returns></returns>
    public virtual bool Reason()
    {
        return true;
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


