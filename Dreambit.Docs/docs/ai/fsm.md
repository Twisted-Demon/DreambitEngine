# State machines and blackboards

Define states as classes and let them access the owning entity through `Fsm`:

```csharp
public sealed class IdleState : State<IdleState>
{
    public override void OnEnter()
    {
        Fsm.Entity.GetComponent<Mover>().Velocity = Vector3.Zero;
    }

    public override bool Reason()
    {
        return !Fsm.TryConsumeEvent("player-seen");
    }
}

public sealed class ChaseState : State<ChaseState>
{
    public override void OnEnter()
    {
        var target = Fsm.Blackboard
            .GetVariable<Vector2>("target")?.Value ?? Vector2.Zero;
        Fsm.Entity.GetComponent<AStarPathFollower>().Seek(target);
    }
}
```

Configure the machine after attaching:

```csharp
var fsm = entity.AttachComponent<FSM>();
fsm.Register(typeof(IdleState), typeof(ChaseState));
fsm.SetDefaultState<IdleState>();
fsm.AddTransition<IdleState, ChaseState>();

fsm.Blackboard.CreateVariable("target", Vector2.Zero);
fsm.GoToDefault();
```

`Reason` returns true to remain. Returning false makes the machine use a state-
requested next state, the first passing specific guard, the first passing any
guard, or the default. `Go<TState>()` is a protected state helper.

`CreateVariable` returns null when the name already exists. Keep the returned
typed variable for frequent access or retrieve it with `GetVariable<T>`.

Transition history is capped and supports `Revert`. The machine guards against
more than 16 immediate transitions in one update.

