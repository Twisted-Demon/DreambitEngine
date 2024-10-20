using System;
using System.Collections.Generic;
using System.Linq;
using PixelariaEngine.ECS;

namespace PixelariaEngine;

public class FSM : Component
{
    private readonly Dictionary<string, State> _statesMap = [];
    private readonly Logger<FSM> _logger = new();
    private string _initialState;
    private State NextState { get; set; }
    public Blackboard Blackboard { get; set; } = new();
    public State CurrentState { get; private set; }

    public override void OnUpdate()
    {
        // if we dont have a state, but set the next one
        if (NextState != null)
            ChangeState();
        // else if we do have a state then reason
        // change state if reasoning fails
        else if (CurrentState != null && !CurrentState.Reason()) ChangeState();

        CurrentState?.OnExecute();
    }

    public void RegisterStates(List<Type> stateTypes)
    {
        if (_statesMap.Count != 0)
        {
            _logger.Warn("Trying to register states more than once");
            return;
        }

        foreach (var stateType in stateTypes.Where(stateType => stateType.IsSubclassOf(typeof(State))))
        {
            if (Activator.CreateInstance(stateType) is not State state) continue;

            state.Fsm = this;
            state.OnInitialize();

            _statesMap.Add(stateType.Name, state);
        }
    }

    private void ChangeState()
    {
        while (true)
        {
            //leave the current state and set the next one
            CurrentState?.OnEnd();
            CurrentState = NextState;
            NextState = null;

            if (CurrentState == null) break; // if we never set the next state, we should leave

            // reason the state, if false then we should loop back and change states
            // if true (meaning we stay) then we run through the enter function and break
            if (!CurrentState.Reason()) continue;
            CurrentState.OnEnter();
            break;
        }
    }

    public void SetDefaultState<T>() where T : State
    {
        if (!_statesMap.TryGetValue(typeof(T).Name, out var value))
        {
            _logger.Warn("State {0} is not registered with {1}", typeof(T).Name, Entity.Name);
            return;
        }
        
        _initialState = typeof(T).Name;
    }

    public void GoToDefaultState()
    {
        if (!_statesMap.TryGetValue(_initialState, out var value))
        {
            _logger.Warn("State {0} is not registered with {1}", _initialState, Entity.Name);
            return;
        }

        NextState = value;
    }

    public void SetNextState<T>() where T : State
    {
        if (!_statesMap.TryGetValue(typeof(T).Name, out var value))
        {
            _logger.Warn("State {0} is not registered with {1}", typeof(T).Name, Entity.Name);
            return;
        }

        NextState = value;
    }
}