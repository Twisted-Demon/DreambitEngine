using System;
using System.Collections.Generic;
using System.Linq;
using PixelariaEngine.ECS;

namespace PixelariaEngine;

public class FSM : Component<FSM>
{
    private State CurrentState { get; set; }
    private State NextState { get; set; } = null;
    private readonly Dictionary<string, State> _statesMap = [];

    public override void OnUpdate()
    {
        //check for if we are going to another state
        if(NextState != null)
            ChangeState();
        
        CurrentState.OnExecute();
    }

    public void RegisterStates(List<Type> stateTypes)
    {
        if (_statesMap.Count != 0)
        {
            Logger.Warn("Trying to register states more than once");
            return;
        }
        
        foreach (var stateType in stateTypes.Where(stateType => stateType.IsSubclassOf(typeof(State))))
        {
            if (Activator.CreateInstance(stateType) is not State state) continue;
            
            state.FSM = this;
            state.OnInitialize();

            _statesMap.Add(stateType.Name, state);
        }
    }

    private void ChangeState()
    {
        CurrentState.OnEnd();
        CurrentState = NextState;
        
        CurrentState.OnEnter();
        NextState = null;
    }

    public void SetInitialState<T>() where T : State
    {
        if (!_statesMap.TryGetValue(typeof(T).Name, out var value))
        {
            Logger.Warn("State {0} is not registered with {1}", typeof(T).Name, Entity.Name);
            return;
        }
        
        CurrentState = value;
    }

    public void SetNextState<T>() where T : State
    {
        if (!_statesMap.TryGetValue(typeof(T).Name, out var value))
        {
            Logger.Warn("State {0} is not registered with {1}", typeof(T).Name, Entity.Name);
            return;
        }
        
        NextState = value;
    }
}
