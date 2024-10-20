using PixelariaEngine.ECS;

namespace PixelariaEngine;

public abstract class State
{
    public FSM Fsm;

    internal bool IsStarted = false;
    public string Identifier { get; internal set; }
    protected Blackboard Blackboard => Fsm.Blackboard;
    public Scene Scene => Fsm?.Scene;
    public Transform Transform => Fsm?.Transform;

    public virtual void OnInitialize()
    {
    }

    public virtual void OnEnter()
    {
    }

    /// <summary>
    ///     Here is where we should check if we should switch states
    ///     true for we should continue to run the current state
    ///     false for we should be switching states
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
    protected readonly Logger<T> Logger = new();
    public new string Identifier = typeof(T).Name;
}