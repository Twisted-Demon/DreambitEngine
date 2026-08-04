using System.ComponentModel;

namespace Dreambit;

public abstract class State
{
    /// <summary>
    ///     back reference to the owning FSM
    /// </summary>
    public FSM Fsm { get; internal set; }
    
    protected Blackboard Blackboard => Fsm.Blackboard;

    /// <summary>
    ///     Logger for the current state
    /// </summary>
    protected readonly ILogger Logger; 

    /// <summary>
    ///     state identifier - by default it's class name
    /// </summary>
    public string Identifier { get; }
    
    protected ICoroutineService CoroutineService => Core.Instance.CurrentScene.CoroutineService;

    public State()
    {
        Identifier = GetType().Name;
        Logger = new Logger(GetType());
    }

    /// <summary>
    ///     Called once after the State instance is created and registered.
    /// </summary>
    public virtual void OnInitialize()
    {
    }

    /// <summary>
    ///     Called every time the FSM switches into this state
    /// </summary>
    public virtual void OnEnter()
    {
    }

    /// <summary>
    ///     Called every frame while this state is active (game loop tick)
    /// </summary>
    public virtual void OnExecute()
    {
    }

    /// <summary>
    ///     Decide whether to remain in this state (true) or request a transition (false).
    /// </summary>
    /// <returns></returns>
    public virtual bool Reason()
    {
        return true;
    }

    /// <summary>
    ///     Called every time the FSM is leaving this state.
    /// </summary>
    public virtual void OnEnd()
    {
    }

    /// <summary>
    ///     request a transition to the given state type
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    protected void Go<TState>() where TState : State
    {
        Fsm?.SetNextState<TState>();
    }

    public override string ToString()
    {
        return Identifier ?? GetType().FullName;
    }
}