using System;
using System.Collections.Generic;
using Dreambit.ECS;

namespace Dreambit;

public class FSM : Component
{
    private readonly List<TransitionEdge> _anyEdges = new(8);
    private readonly Dictionary<Type, List<TransitionEdge>> _edges = new(32);

    private readonly Queue<string> _eventQueue = new(8);
    private readonly Stack<Type> _history = new(8);

    // ----- Internals -----
    private readonly Logger<FSM> _logger = new();

    private readonly Dictionary<Type, State> _states = new(32);

    private Type _defaultStateType;
    private int _historyCapacity = 16;

    // ----- Public surface -----
    public State CurrentState { get; private set; }
    public State NextState { get; private set; }

    /// <summary>Total number of transitions taken.</summary>
    public int TransitionCount { get; private set; }

    /// <summary>Frames spent in current state.</summary>
    public int FramesInState { get; private set; }

    /// <summary>Time spent in current state (accumulated via clock).</summary>
    public TimeSpan TimeInState { get; private set; }

    /// <summary>Optional: reason string for the last transition (debug).</summary>
    public string LastTransitionReason { get; private set; }

    /// <summary>Simple key/value store available to states/guards.</summary>
    public Blackboard Blackboard { get; private set; } = new();

    // ----- Events -----
    public event Action<Type, Type, string> OnTransition;
    public event Action<Type> OnStateEntered;
    public event Action<Type> OnStateExited;

    public void Register(params Type[] stateTypes)
    {
        if (stateTypes == null) return;
        for (var i = 0; i < stateTypes.Length; i++)
        {
            var type = stateTypes[i];
            if (type is null || !type.IsSubclassOf(typeof(State))) continue;
            if (_states.ContainsKey(type)) continue;

            if (Activator.CreateInstance(type) is State state)
            {
                state.Fsm = this;
                state.Identifier = type.FullName;
                state.OnInitialize();
                _states.Add(type, state);
            }
        }
    }

    public void SetBlackboard<T>(T blackboard) where T : Blackboard
    {
        Blackboard = blackboard;
    }

    public T GetBlackBoard<T>() where T : Blackboard
    {
        return Blackboard as T;
    }

    public void SetDefaultState<TState>() where TState : State
    {
        var type = typeof(TState);
        if (_states.ContainsKey(type)) _defaultStateType = type;
        else _logger.Warn("Default state {0} not registered", type.FullName);
    }

    public void GoToDefault()
    {
        if (_defaultStateType != null)
            SetNextState(_defaultStateType);
    }

    public void AddTransition<TFrom, TTo>(Func<FSM, bool> guard = null)
        where TFrom : State where TTo : State
    {
        var from = typeof(TFrom);
        var to = typeof(TTo);
        if (!_states.ContainsKey(from) || !_states.ContainsKey(to))
        {
            _logger.Warn("Transition {0} -> {1} references unregistered state(s)", from.Name, to.Name);
            return;
        }

        if (!_edges.TryGetValue(from, out var list))
        {
            list = new List<TransitionEdge>(4);
            _edges[from] = list;
        }

        list.Add(new TransitionEdge(to, guard));
    }

    public void AddAnyTransition<TTo>(Func<FSM, bool> guard = null) where TTo : State
    {
        var to = typeof(TTo);
        if (!_states.ContainsKey(to))
        {
            _logger.Warn("Any-transition targets unregistered state {0}", to.Name);
            return;
        }

        _anyEdges.Add(new TransitionEdge(to, guard));
    }

    public void SetHistoryCapacity(int capacity)
    {
        _historyCapacity = Math.Max(0, capacity);
        while (_history.Count > _historyCapacity) _history.Pop();
    }

    /// <summary>Request a transition to the given state type next Update.</summary>
    public void SetNextState<TState>() where TState : State
    {
        SetNextState(typeof(TState));
    }

    /// <summary>Request a transition to the given state type next Update.</summary>
    public void SetNextState(Type stateType, string reason = null)
    {
        if (stateType == null || !_states.ContainsKey(stateType))
        {
            _logger.Warn("SetNextState: State not registered: {0}", stateType?.FullName);
            return;
        }

        NextState = _states[stateType];
        LastTransitionReason = reason;
    }

    /// <summary>Return to the previous state (if any).</summary>
    public void Revert()
    {
        if (_history.Count == 0) return;
        var prev = _history.Pop();
        SetNextState(prev, "Revert");
    }

    /// <summary>Queue a string event; states/guards can read them with TryConsumeEvent.</summary>
    public void Trigger(string evt)
    {
        if (!string.IsNullOrEmpty(evt)) _eventQueue.Enqueue(evt);
    }

    /// <summary>Try to consume (remove) an event from the queue. Returns true if found.</summary>
    public bool TryConsumeEvent(string evt)
    {
        if (string.IsNullOrEmpty(evt) || _eventQueue.Count == 0) return false;

        // Single-threaded, small queue: O(n) scan with rebuild is fine and allocations-free.
        var found = false;
        var n = _eventQueue.Count;
        for (var i = 0; i < n; i++)
        {
            var cur = _eventQueue.Dequeue();
            if (!found && cur == evt)
                found = true; // drop it
            else
                _eventQueue.Enqueue(cur); // keep it
        }

        return found;
    }

    /// <summary>Convenience: clear all queued events.</summary>
    public void ClearEvents()
    {
        _eventQueue.Clear();
    }

    // ---------- Lifecycle ----------

    public override void OnAddedToEntity()
    {
        base.OnAddedToEntity();
        // If no current state and default is set, go there on first update.
        if (CurrentState == null && _defaultStateType != null)
            NextState = _states[_defaultStateType];
    }

    public override void OnUpdate()
    {
        // Apply a pending transition first (e.g., SetNextState called externally).
        if (NextState != null) ApplyTransition();

        // Execute current state behavior.
        CurrentState?.OnExecute();

        // Single Reason() per frame:
        var stay = CurrentState?.Reason() ?? true;
        if (!stay)
        {
            // If the state asked to leave but didn't specify NextState, try table/wildcards/defaults.
            if (NextState == null && !TryApplyGuardedTransition())
            {
                // Neither the state nor the table provided a next state: fallback to default, or warn.
                if (_defaultStateType != null)
                    NextState = _states[_defaultStateType];
                else
                    _logger.Warn("State {0} rejected but provided no NextState and no default exists.",
                        CurrentState);
            }

            if (NextState != null)
                ApplyTransition();
        }
    }

    // ---------- Internals ----------

    private void ApplyTransition()
    {
        // Prevent infinite ping-pong in a single frame
        var safety = 16;
        while (NextState != null && safety-- > 0)
        {
            var from = CurrentState?.GetType();
            var to = NextState.GetType();

            // Record history
            if (from != null)
            {
                _history.Push(from);
                while (_history.Count > _historyCapacity) _history.Pop();
            }

            // Leave current
            CurrentState?.OnEnd();
            OnStateExited?.Invoke(from);

            // Switch
            CurrentState = NextState;
            NextState = null;
            FramesInState = 0;
            TimeInState = TimeSpan.Zero;
            TransitionCount++;

            // Enter
            CurrentState.OnEnter();
            OnStateEntered?.Invoke(to);

            // Immediately check if the new state rejects itself (e.g., guard fails at runtime).
            var stay = CurrentState.Reason();
            if (!stay)
            {
                // If it rejects itself, try guarded/wildcards or default.
                if (NextState == null && !TryApplyGuardedTransition())
                {
                    if (_defaultStateType != null)
                    {
                        NextState = _states[_defaultStateType];
                    }
                    else
                    {
                        _logger.Warn("State {0} rejected on enter and no fallback provided.", CurrentState);
                        CurrentState = null; // stop machine safely
                        break;
                    }
                }
                // loop to apply the new NextState
            }
            else
            {
                // stable state for this frame
                OnTransition?.Invoke(from, to, LastTransitionReason);
                LastTransitionReason = null;
                break;
            }
        }

        if (safety <= 0)
            _logger.Warn("FSM transition safety break triggered (possible ping-pong).");
    }

    private bool TryApplyGuardedTransition()
    {
        if (CurrentState == null) return false;
        var from = CurrentState.GetType();

        // 1) Specific transitions from this state
        if (_edges.TryGetValue(from, out var list))
            for (var i = 0; i < list.Count; i++)
            {
                var edge = list[i]; // copy
                if (edge.Guard == null || SafeGuard(edge.Guard))
                {
                    NextState = _states[edge.To];
                    LastTransitionReason = "Guarded";
                    return true;
                }
            }

        // 2) Wildcard transitions
        for (var i = 0; i < _anyEdges.Count; i++)
        {
            var edge = _anyEdges[i]; // copy
            if (edge.Guard == null || SafeGuard(edge.Guard))
            {
                NextState = _states[edge.To];
                LastTransitionReason = "AnyGuarded";
                return true;
            }
        }

        return false;
    }

    private bool SafeGuard(Func<FSM, bool> guard)
    {
        try
        {
            return guard(this);
        }
        catch (Exception ex)
        {
            _logger.Error("FSM guard threw: {0}", ex);
            return false;
        }
    }
}