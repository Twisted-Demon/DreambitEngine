# FSM

`FSM` is a component-based finite state machine. Register `State` types,
configure a default, and add guarded transitions before the first update.

```csharp
var fsm = entity.AttachComponent<FSM>();
fsm.Register(typeof(IdleState), typeof(ChaseState));
fsm.SetDefaultState<IdleState>();
fsm.AddTransition<IdleState, ChaseState>(machine =>
    machine.Blackboard.GetVariable<bool>("seesPlayer")?.Value == true);
fsm.GoToDefault();
```

States receive `OnInitialize`, `OnEnter`, `OnExecute`, `Reason`, and `OnEnd`.
`SetNextState<T>` schedules a transition. `Trigger` and `TryConsumeEvent` provide
a small event queue; `Revert` returns through transition history.

Monitor `CurrentState`, `TransitionCount`, and the transition events for
debugging. `FramesInState` and `TimeInState` are exposed but the current
implementation only resets them on transition; it does not increment them yet.
See [State machines and blackboards](../../ai/fsm.md) for a complete state
implementation.
