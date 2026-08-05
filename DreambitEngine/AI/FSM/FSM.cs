using System;
using System.Collections.Generic;
using System.Diagnostics;
using Dreambit.ECS;
using Dreambit.Events;

namespace Dreambit;

/// <summary>
/// A finite state machine component.
///
/// Transition priority each frame:
/// 1. A transition already requested before this update.
/// 2. Any-state transitions.
/// 3. A transition explicitly requested by the active state.
/// 4. State-specific guarded transitions.
/// 5. The default state when Reason() rejects the current state.
///
/// Transition guards are evaluated every frame. A state does not need to
/// return false from Reason() for one of its guarded transitions to activate.
/// </summary>
public class FSM : Component
{
    private const int TransitionSafetyLimit = 16;

    private readonly List<TransitionEdge> _anyEdges = new(8);
    private readonly Dictionary<Type, List<TransitionEdge>> _edges = new(32);

    private readonly Queue<string> _eventQueue = new(8);

    // LinkedList allows us to remove the oldest history entry while
    // retaining LIFO behavior for Revert().
    private readonly LinkedList<Type> _history = new();

    private readonly Dictionary<Type, State> _states = new(32);
    private readonly Logger<FSM> _logger = new();

    private Type _defaultStateType;
    private Type _lastUnresolvedRejectionState;

    private int _historyCapacity = 16;

    private string _pendingTransitionReason;
    private bool _allowPendingSelfTransition;

    private long _lastUpdateTimestamp = Stopwatch.GetTimestamp();

    // ---------------------------------------------------------------------
    // Public state
    // ---------------------------------------------------------------------

    public State CurrentState { get; private set; }

    public State NextState { get; private set; }

    /// <summary>
    /// Total number of completed state transitions.
    ///
    /// The initial transition from no state into the default state counts as
    /// a transition.
    /// </summary>
    public int TransitionCount { get; private set; }

    /// <summary>
    /// Number of update frames spent executing the current state.
    /// </summary>
    public int FramesInState { get; private set; }

    /// <summary>
    /// Time accumulated while the current state has been active.
    /// </summary>
    public TimeSpan TimeInState { get; private set; }

    /// <summary>
    /// Reason associated with the most recently completed transition.
    /// </summary>
    public string LastTransitionReason { get; private set; }

    /// <summary>
    /// Shared state data available to states and transition guards.
    /// </summary>
    public Blackboard Blackboard { get; private set; } = new();

    /// <summary>
    /// Optional game-time delta provider.
    ///
    /// When this is null, the FSM uses a monotonic Stopwatch-based delta.
    /// Set this when TimeInState should respect pausing, time scaling, or
    /// another engine-specific game clock.
    ///
    /// Example:
    /// fsm.DeltaTimeProvider = () => yourGameClock.DeltaTime;
    /// </summary>
    public Func<TimeSpan> DeltaTimeProvider { get; set; }

    // ---------------------------------------------------------------------
    // Events
    // ---------------------------------------------------------------------

    /// <summary>
    /// Raised after a transition has completed.
    ///
    /// Arguments:
    /// - Previous state type, or null for the initial state.
    /// - New state type.
    /// - Transition reason.
    /// </summary>
    public event Action<Type, Type, string> OnTransition;

    /// <summary>
    /// Raised after a state has entered.
    /// </summary>
    public event Action<Type> OnStateEntered;

    /// <summary>
    /// Raised after a state has exited.
    /// </summary>
    public event Action<Type> OnStateExited;

    // ---------------------------------------------------------------------
    // State registration
    // ---------------------------------------------------------------------

    /// <summary>
    /// Creates and registers the supplied state types.
    ///
    /// State types must:
    /// - Inherit from State.
    /// - Be non-abstract.
    /// - Have a parameterless constructor.
    ///
    /// Each state type is instantiated only once per FSM.
    /// </summary>
    public void Register(params Type[] stateTypes)
    {
        if (stateTypes == null)
            return;

        for (var i = 0; i < stateTypes.Length; i++)
        {
            var type = stateTypes[i];

            if (type == null)
                continue;

            if (type.IsAbstract || !typeof(State).IsAssignableFrom(type))
            {
                _logger.Warn(
                    "Cannot register FSM state {0}: type must be a non-abstract State.",
                    type.FullName);

                continue;
            }

            if (_states.ContainsKey(type))
                continue;

            try
            {
                if (Activator.CreateInstance(type) is not State state)
                {
                    _logger.Warn(
                        "Could not create FSM state instance for {0}.",
                        type.FullName);

                    continue;
                }

                state.Fsm = this;

                // Add before initialization so OnInitialize() can safely
                // interact with the owning FSM.
                _states.Add(type, state);

                try
                {
                    state.OnInitialize();
                }
                catch
                {
                    _states.Remove(type);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "Failed to register FSM state {0}: {1}",
                    type.FullName,
                    ex);
            }
        }
    }

    /// <summary>
    /// Registers one state type.
    /// </summary>
    public void Register<TState>() where TState : State
    {
        Register(typeof(TState));
    }

    /// <summary>
    /// Returns whether the supplied state type is registered.
    /// </summary>
    public bool IsRegistered<TState>() where TState : State
    {
        return _states.ContainsKey(typeof(TState));
    }

    /// <summary>
    /// Returns a registered state instance.
    /// </summary>
    public TState GetState<TState>() where TState : State
    {
        return _states.TryGetValue(typeof(TState), out var state)
            ? state as TState
            : null;
    }

    // ---------------------------------------------------------------------
    // Blackboard
    // ---------------------------------------------------------------------

    public void SetBlackboard<T>(T blackboard) where T : Blackboard
    {
        Blackboard = blackboard
            ?? throw new ArgumentNullException(nameof(blackboard));
    }

    public T SetBlackboard<T>() where T : Blackboard
    {
        if (Activator.CreateInstance(typeof(T)) is not T typedBlackboard)
        {
            throw new InvalidOperationException(
                $"Could not create blackboard of type {typeof(T).FullName}.");
        }

        Blackboard = typedBlackboard;
        return typedBlackboard;
    }

    /// <summary>
    /// Retained for compatibility with the existing API.
    /// </summary>
    public T GetBlackBoard<T>() where T : Blackboard
    {
        return Blackboard as T;
    }

    /// <summary>
    /// Correctly-cased alias for GetBlackBoard().
    /// </summary>
    public T GetBlackboard<T>() where T : Blackboard
    {
        return Blackboard as T;
    }

    // ---------------------------------------------------------------------
    // Default state
    // ---------------------------------------------------------------------

    public void SetDefaultState<TState>() where TState : State
    {
        var type = typeof(TState);

        if (!_states.ContainsKey(type))
        {
            _logger.Warn(
                "Default state {0} has not been registered.",
                type.FullName);

            return;
        }

        _defaultStateType = type;

        // This supports configuring an FSM after it has already been added
        // to an entity but before it has entered a state.
        if (CurrentState == null && NextState == null)
        {
            QueueTransition(
                type,
                "DefaultState",
                allowSelfTransition: false);
        }
    }

    public void GoToDefault()
    {
        if (_defaultStateType == null)
        {
            _logger.Warn("Cannot go to default state: no default is configured.");
            return;
        }

        QueueTransition(
            _defaultStateType,
            "GoToDefault",
            allowSelfTransition: false);
    }

    // ---------------------------------------------------------------------
    // Transition registration
    // ---------------------------------------------------------------------

    /// <summary>
    /// Adds a transition that is evaluated while TFrom is the current state.
    ///
    /// Guards are evaluated every update. The current state's Reason()
    /// method does not need to return false for the guard to be checked.
    ///
    /// When multiple transitions pass, the first registered transition wins.
    /// </summary>
    public void AddTransition<TFrom, TTo>(
        Func<FSM, bool> guard = null)
        where TFrom : State
        where TTo : State
    {
        var from = typeof(TFrom);
        var to = typeof(TTo);

        if (!_states.ContainsKey(from) || !_states.ContainsKey(to))
        {
            _logger.Warn(
                "Transition {0} -> {1} references unregistered state(s).",
                from.Name,
                to.Name);

            return;
        }

        if (!_edges.TryGetValue(from, out var list))
        {
            list = new List<TransitionEdge>(4);
            _edges[from] = list;
        }

        list.Add(new TransitionEdge(to, guard));
    }

    /// <summary>
    /// Adds a transition that can activate from any current state.
    ///
    /// Any-state transitions have priority over explicit state requests and
    /// state-specific transitions during the normal update cycle. This makes
    /// them appropriate for states such as Dead, Disabled, or Despawned.
    ///
    /// When multiple any-state transitions pass, the first registered
    /// transition wins.
    /// </summary>
    public void AddAnyTransition<TTo>(
        Func<FSM, bool> guard = null)
        where TTo : State
    {
        var to = typeof(TTo);

        if (!_states.ContainsKey(to))
        {
            _logger.Warn(
                "Any-state transition targets unregistered state {0}.",
                to.Name);

            return;
        }

        _anyEdges.Add(new TransitionEdge(to, guard));
    }

    // ---------------------------------------------------------------------
    // Direct transition requests
    // ---------------------------------------------------------------------

    /// <summary>
    /// Requests a transition for the next transition-processing point.
    /// </summary>
    public void SetNextState<TState>(
        string reason = null)
        where TState : State
    {
        SetNextState(typeof(TState), reason);
    }

    /// <summary>
    /// Requests a transition for the next transition-processing point.
    /// </summary>
    public void SetNextState(
        Type stateType,
        string reason = null)
    {
        if (stateType == null || !_states.ContainsKey(stateType))
        {
            _logger.Warn(
                "SetNextState: state is not registered: {0}",
                stateType?.FullName);

            return;
        }

        QueueTransition(
            stateType,
            reason ?? "Explicit",
            allowSelfTransition: false);
    }

    /// <summary>
    /// Exits and re-enters the current state.
    ///
    /// Ordinary SetNextState() requests targeting the current state are
    /// ignored to protect against accidental self-transition loops. Use this
    /// method when restarting the state is intentional.
    /// </summary>
    public void RestartCurrentState(string reason = null)
    {
        if (CurrentState == null)
            return;

        QueueTransition(
            CurrentState.GetType(),
            reason ?? "Restart",
            allowSelfTransition: true);
    }

    // ---------------------------------------------------------------------
    // History
    // ---------------------------------------------------------------------

    public void SetHistoryCapacity(int capacity)
    {
        _historyCapacity = Math.Max(0, capacity);

        while (_history.Count > _historyCapacity)
            _history.RemoveFirst();
    }

    public void ClearHistory()
    {
        _history.Clear();
    }

    /// <summary>
    /// Requests a transition to the most recently active state.
    /// </summary>
    public void Revert()
    {
        if (_history.Last == null)
            return;

        var previousStateType = _history.Last.Value;
        _history.RemoveLast();

        QueueTransition(
            previousStateType,
            "Revert",
            allowSelfTransition: false);
    }

    // ---------------------------------------------------------------------
    // String event queue
    // ---------------------------------------------------------------------

    public void Trigger(string evt)
    {
        if (!string.IsNullOrWhiteSpace(evt))
            _eventQueue.Enqueue(evt);
    }

    public void Trigger<TEvent>() where TEvent : DreambitEvent
    {
        Trigger(nameof(TEvent));
    }

    /// <summary>
    /// Removes and returns the first matching queued event.
    ///
    /// Transition guards are now evaluated every frame, so events can be
    /// consumed directly from a transition guard:
    ///
    /// fsm.AddTransition&lt;IdleState, AlertState&gt;(
    ///     machine => machine.TryConsumeEvent("EnemySeen"));
    /// </summary>
    public bool TryConsumeEvent(string evt)
    {
        if (string.IsNullOrEmpty(evt) || _eventQueue.Count == 0)
            return false;

        var found = false;
        var eventCount = _eventQueue.Count;

        for (var i = 0; i < eventCount; i++)
        {
            var currentEvent = _eventQueue.Dequeue();

            if (!found && currentEvent == evt)
            {
                found = true;
                continue;
            }

            _eventQueue.Enqueue(currentEvent);
        }

        return found;
    }

    public bool TryConsumeEvent<TEvent>() where TEvent : DreambitEvent
    {
        return TryConsumeEvent(nameof(TEvent));
    }

    public void ClearEvents()
    {
        _eventQueue.Clear();
    }

    // ---------------------------------------------------------------------
    // Component lifecycle
    // ---------------------------------------------------------------------

    public override void OnAddedToEntity()
    {
        base.OnAddedToEntity();

        _lastUpdateTimestamp = Stopwatch.GetTimestamp();

        if (CurrentState == null &&
            NextState == null &&
            _defaultStateType != null)
        {
            QueueTransition(
                _defaultStateType,
                "InitialDefaultState",
                allowSelfTransition: false);
        }
    }

    public override void OnUpdate()
    {
        var deltaTime = ReadDeltaTime();

        // Apply requests made before this update, including an initial
        // default-state request.
        if (NextState != null)
            ApplyPendingTransitions();

        if (CurrentState == null)
        {
            TryEnterDefaultState();

            if (NextState != null)
                ApplyPendingTransitions();

            if (CurrentState == null)
                return;
        }

        CurrentState.OnExecute();

        FramesInState++;
        TimeInState += deltaTime;

        /*
         * Any-state transitions are deliberately checked first.
         *
         * Example: if AttackState requests ReloadState during OnExecute(),
         * but the enemy's health is now zero, an any-state DeadState
         * transition should win.
         */
        if (TryQueueAnyTransition())
        {
            ApplyPendingTransitions();
            return;
        }

        // Respect a transition explicitly requested from OnExecute().
        if (NextState != null)
        {
            ApplyPendingTransitions();
            return;
        }

        /*
         * Reason() remains supported for state-local decisions and for
         * compatibility with existing State implementations.
         *
         * Returning true means the state accepts remaining active.
         * Returning false means the state wants to leave.
         *
         * A state can call Go<TState>() from Reason(), but transition guards
         * no longer depend on Reason() returning false.
         */
        var stayInCurrentState = CurrentState.Reason();

        // Respect a transition explicitly requested from Reason().
        if (NextState != null)
        {
            ApplyPendingTransitions();
            return;
        }

        // State-specific guards are evaluated every update.
        if (TryQueueSpecificTransition())
        {
            ApplyPendingTransitions();
            return;
        }

        if (stayInCurrentState)
        {
            _lastUnresolvedRejectionState = null;
            return;
        }

        /*
         * The state rejected itself, but neither the state nor a transition
         * edge provided a destination. Fall back to the default state when
         * it would produce a real transition.
         */
        if (_defaultStateType != null &&
            CurrentState.GetType() != _defaultStateType)
        {
            QueueTransition(
                _defaultStateType,
                "RejectedToDefault",
                allowSelfTransition: false);

            ApplyPendingTransitions();
            return;
        }

        WarnAboutUnresolvedRejection();
    }

    public override void OnDestroyed()
    {
        base.OnDestroyed();

        if (CurrentState != null)
        {
            CurrentState.OnEnd();
            OnStateExited?.Invoke(CurrentState.GetType());
        }

        foreach (var state in _states.Values)
        {
            state.OnDestroyed();
        }

        CurrentState = null;
        NextState = null;

        _states.Clear();
        _edges.Clear();
        _anyEdges.Clear();
        _eventQueue.Clear();
        _history.Clear();

        OnTransition = null;
        OnStateEntered = null;
        OnStateExited = null;
    }

    // ---------------------------------------------------------------------
    // Transition internals
    // ---------------------------------------------------------------------

    private void TryEnterDefaultState()
    {
        if (CurrentState != null ||
            NextState != null ||
            _defaultStateType == null)
        {
            return;
        }

        QueueTransition(
            _defaultStateType,
            "DefaultFallback",
            allowSelfTransition: false);
    }

    private bool TryQueueAnyTransition()
    {
        if (CurrentState == null)
            return false;

        var currentType = CurrentState.GetType();

        for (var i = 0; i < _anyEdges.Count; i++)
        {
            var edge = _anyEdges[i];

            // Do not repeatedly transition into the state that is already
            // active. RestartCurrentState() exists for intentional re-entry.
            if (edge.To == currentType)
                continue;

            if (edge.Guard != null && !SafeGuard(edge.Guard))
                continue;

            QueueTransition(
                edge.To,
                "AnyGuarded",
                allowSelfTransition: false);

            return true;
        }

        return false;
    }

    private bool TryQueueSpecificTransition()
    {
        if (CurrentState == null)
            return false;

        var currentType = CurrentState.GetType();

        if (!_edges.TryGetValue(currentType, out var transitions))
            return false;

        for (var i = 0; i < transitions.Count; i++)
        {
            var edge = transitions[i];

            if (edge.To == currentType)
                continue;

            if (edge.Guard != null && !SafeGuard(edge.Guard))
                continue;

            QueueTransition(
                edge.To,
                "Guarded",
                allowSelfTransition: false);

            return true;
        }

        return false;
    }

    private void QueueTransition(
        Type stateType,
        string reason,
        bool allowSelfTransition)
    {
        if (!_states.TryGetValue(stateType, out var state))
        {
            _logger.Warn(
                "Cannot queue transition to unregistered state {0}.",
                stateType?.FullName);

            return;
        }

        NextState = state;
        _pendingTransitionReason = reason;
        _allowPendingSelfTransition = allowSelfTransition;
    }

    /// <summary>
    /// Applies the pending transition and any additional transitions queued
    /// by OnEnd() or OnEnter().
    ///
    /// A safety limit prevents malformed states from infinitely chaining
    /// transitions within one frame.
    /// </summary>
    private void ApplyPendingTransitions()
    {
        var remainingTransitions = TransitionSafetyLimit;

        while (NextState != null && remainingTransitions-- > 0)
        {
            var destinationState = NextState;
            var transitionReason = _pendingTransitionReason;
            var allowSelfTransition = _allowPendingSelfTransition;

            NextState = null;
            _pendingTransitionReason = null;
            _allowPendingSelfTransition = false;

            var previousState = CurrentState;
            var previousType = previousState?.GetType();
            var destinationType = destinationState.GetType();

            if (!allowSelfTransition &&
                previousType != null &&
                previousType == destinationType)
            {
                // Ignore accidental transitions to the already-active state.
                continue;
            }

            AddToHistory(previousType);

            previousState?.OnEnd();

            if (previousType != null)
                OnStateExited?.Invoke(previousType);

            CurrentState = destinationState;

            FramesInState = 0;
            TimeInState = TimeSpan.Zero;
            TransitionCount++;

            _lastUnresolvedRejectionState = null;

            CurrentState.OnEnter();

            LastTransitionReason = transitionReason;

            OnStateEntered?.Invoke(destinationType);
            OnTransition?.Invoke(
                previousType,
                destinationType,
                transitionReason);

            /*
             * If OnEnter() queued another state, the loop applies it.
             *
             * We intentionally do not evaluate Reason() or guards here.
             * Normal guards are evaluated once during OnUpdate(), preventing
             * guard ping-pong and unpredictable enter/reject chains.
             */
        }

        if (remainingTransitions <= 0 && NextState != null)
        {
            _logger.Warn(
                "FSM transition safety limit reached. " +
                "OnEnter() or OnEnd() may be repeatedly queueing states.");

            NextState = null;
            _pendingTransitionReason = null;
            _allowPendingSelfTransition = false;
        }
    }

    private void AddToHistory(Type stateType)
    {
        if (stateType == null || _historyCapacity <= 0)
            return;

        _history.AddLast(stateType);

        // Remove the oldest entries, preserving the most recent history.
        while (_history.Count > _historyCapacity)
            _history.RemoveFirst();
    }

    private bool SafeGuard(Func<FSM, bool> guard)
    {
        try
        {
            return guard(this);
        }
        catch (Exception ex)
        {
            _logger.Error("FSM transition guard threw: {0}", ex);
            return false;
        }
    }

    private void WarnAboutUnresolvedRejection()
    {
        if (CurrentState == null)
            return;

        var currentType = CurrentState.GetType();

        // Avoid logging the same warning every frame.
        if (_lastUnresolvedRejectionState == currentType)
            return;

        _lastUnresolvedRejectionState = currentType;

        _logger.Warn(
            "State {0} returned false from Reason(), but no explicit, " +
            "guarded, any-state, or default transition was available.",
            currentType.FullName);
    }

    // ---------------------------------------------------------------------
    // Timing
    // ---------------------------------------------------------------------

    private TimeSpan ReadDeltaTime()
    {
        var currentTimestamp = Stopwatch.GetTimestamp();

        try
        {
            if (DeltaTimeProvider != null)
            {
                var providedDelta = DeltaTimeProvider();

                _lastUpdateTimestamp = currentTimestamp;

                return providedDelta < TimeSpan.Zero
                    ? TimeSpan.Zero
                    : providedDelta;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(
                "FSM DeltaTimeProvider threw an exception: {0}",
                ex);

            _lastUpdateTimestamp = currentTimestamp;
            return TimeSpan.Zero;
        }

        var elapsedTicks = currentTimestamp - _lastUpdateTimestamp;
        _lastUpdateTimestamp = currentTimestamp;

        if (elapsedTicks <= 0)
            return TimeSpan.Zero;

        var elapsedSeconds =
            elapsedTicks / (double)Stopwatch.Frequency;

        return TimeSpan.FromSeconds(elapsedSeconds);
    }
}