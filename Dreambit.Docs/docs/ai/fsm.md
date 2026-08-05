# Finite State Machines

The Dreambit finite state machine, or `FSM`, manages behavior that can be divided into distinct states.

Common uses include:

* Player movement
* Enemy artificial intelligence
* User-interface navigation
* Game phases
* Menus
* Cutscenes
* Character abilities
* Interaction modes

An FSM has one active state at a time. The active state executes its behavior every frame, while registered transition conditions determine when the machine should enter another state.

A basic platformer player might use the following states:

```text
                         ┌─────────────┐
                         │  HurtState  │
                         └──────┬──────┘
                                │ Recovered
                                v
┌───────────┐  Movement   ┌──────────┐  Jump pressed  ┌───────────┐
│ IdleState │ ──────────► │ RunState │ ─────────────► │ JumpState │
└─────┬─────┘             └────┬─────┘                └─────┬─────┘
      │ Jump pressed            │ No movement                │ Falling
      └─────────────────────────┴────────────────────────────v
                                                         ┌───────────┐
                                                         │ FallState │
                                                         └─────┬─────┘
                                                               │ Grounded
                                                               v
                                                         Idle or Run
```

A global death transition can interrupt any of these states:

```text
Any active state ── Health <= 0 ──► DeadState
```

---

## Core concepts

An FSM is composed of four main parts:

| Part             | Purpose                                                       |
| ---------------- | ------------------------------------------------------------- |
| `FSM`            | Owns states, checks transitions, and changes the active state |
| `State`          | Defines behavior for one mode                                 |
| `Blackboard`     | Stores data shared between states and transition guards       |
| `TransitionEdge` | Connects a source state to a destination state                |
| Guard            | A function that decides whether a transition should occur     |

A typical update looks like this:

```text
Run CurrentState.OnExecute()
              |
              v
Check any-state transition guards
              |
              v
Check transition guards for the current state
              |
              v
If a guard passes, leave the current state
              |
              v
Enter the destination state
```

!!! note

```
This page documents the corrected FSM implementation where registered transition guards are evaluated every update.
```

---

# State lifecycle

Every state inherits from `State`.

```csharp
using Dreambit;

public sealed class PlayerIdleState : State
{
}
```

A state can override several lifecycle methods.

## `OnInitialize`

Called once when the state is created and registered.

```csharp
public override void OnInitialize()
{
    // Perform one-time setup.
}
```

Use `OnInitialize` for:

* Creating state-owned helper objects
* Resolving long-lived references
* Allocating reusable collections
* Performing setup that should happen only once

Do not use it for values that must reset every time the state becomes active.

---

## `OnEnter`

Called each time the FSM enters the state.

```csharp
public override void OnEnter()
{
    Logger.Info("Player entered IdleState");
}
```

Use `OnEnter` for:

* Resetting state-local values
* Starting an animation
* Enabling state-specific effects
* Reading the latest blackboard values
* Preparing the state for execution

A state instance is reused. Therefore, private state fields should normally be reset in `OnEnter`.

```csharp
private float _elapsedTime;

public override void OnEnter()
{
    _elapsedTime = 0f;
}
```

---

## `OnExecute`

Called once per frame while the state is active.

```csharp
public override void OnExecute()
{
    // Run the state's active behavior.
}
```

Use `OnExecute` for:

* Applying movement
* Updating animation
* Reading input
* Running artificial intelligence
* Updating timers
* Requesting explicit state changes

For example:

```csharp
public override void OnExecute()
{
    var data = Fsm.GetBlackboard<PlayerBlackboard>();

    data.Motor.Value.ApplyHorizontalMovement(
        data.MoveInputX.Value);
}
```

---

## `Reason`

`Reason` allows the active state to decide whether it can remain active.

```csharp
public override bool Reason()
{
    return true;
}
```

The return value means:

| Return value | Meaning                            |
| ------------ | ---------------------------------- |
| `true`       | The state accepts remaining active |
| `false`      | The state wants to leave           |

With the corrected FSM, ordinary transition guards are checked every frame. Most states therefore do not need to override `Reason`.

Use `Reason` when a state needs to make a local decision and explicitly choose its destination.

```csharp
public override bool Reason()
{
    if (_animationFinished)
    {
        Go<PlayerIdleState>();
        return false;
    }

    return true;
}
```

For most movement states, prefer registered transition guards instead.

---

## `OnEnd`

Called whenever the FSM leaves the state.

```csharp
public override void OnEnd()
{
    Logger.Info("Player left IdleState");
}
```

Use `OnEnd` for:

* Stopping state-specific effects
* Ending animations
* Clearing temporary flags
* Disabling state-owned objects
* Releasing temporary resources

---

## `OnDestroyed`

Called when the owning FSM is destroyed.

```csharp
public override void OnDestroyed()
{
    // Release state-lifetime resources.
}
```

Use this for cleanup related to resources created in `OnInitialize`.

---

# Registering states

A state must be registered before it can become active or be used in a transition.

```csharp
Fsm.Register(
    typeof(PlayerIdleState),
    typeof(PlayerRunState),
    typeof(PlayerJumpState),
    typeof(PlayerFallState),
    typeof(PlayerHurtState),
    typeof(PlayerDeadState));
```

Registration:

1. Creates one instance of each state.
2. Assigns the owning FSM.
3. Calls the state's `OnInitialize` method.
4. Stores the state instance for later reuse.

Registered state classes must:

* Inherit from `State`
* Be non-abstract
* Have a parameterless constructor

A state is not recreated every time it becomes active.

---

# Setting the initial state

Registering states does not automatically make one active.

Set a default state after registering all states:

```csharp
Fsm.SetDefaultState<PlayerIdleState>();
Fsm.GoToDefault();
```

This starts the FSM in `PlayerIdleState`.

Without an initial state:

```csharp
Fsm.CurrentState == null
```

State-specific transitions cannot be evaluated because there is no active source state.

The recommended setup order is:

```text
1. Create the blackboard
2. Register states
3. Register transitions
4. Set the default state
5. Enter the default state
```

For example:

```csharp
private void ConfigureFsm()
{
    _blackboard = Fsm.SetBlackboard<PlayerBlackboard>();

    RegisterStates();
    RegisterTransitions();

    Fsm.SetDefaultState<PlayerIdleState>();
    Fsm.GoToDefault();
}
```

---

# The blackboard

A blackboard stores data shared between states and transition guards.

For a platformer player, shared data might include:

* Movement input
* Grounded status
* Vertical velocity
* Current health
* Jump requests
* A reference to the player's movement controller

## Example blackboard

```csharp
using Dreambit;

public sealed class PlayerBlackboard : Blackboard
{
    public BlackboardVar<PlayerMotor> Motor { get; }

    public BlackboardVar<float> MoveInputX { get; }

    public BlackboardVar<float> VerticalVelocity { get; }

    public BlackboardVar<bool> IsGrounded { get; }

    public BlackboardVar<bool> JumpRequested { get; }

    public BlackboardVar<bool> IsInvulnerable { get; }

    public BlackboardVar<int> Health { get; }

    public PlayerBlackboard()
    {
        Motor = CreateVariable<PlayerMotor>(
            "Motor",
            null);

        MoveInputX = CreateVariable(
            "MoveInputX",
            0f);

        VerticalVelocity = CreateVariable(
            "VerticalVelocity",
            0f);

        IsGrounded = CreateVariable(
            "IsGrounded",
            false);

        JumpRequested = CreateVariable(
            "JumpRequested",
            false);

        IsInvulnerable = CreateVariable(
            "IsInvulnerable",
            false);

        Health = CreateVariable(
            "Health",
            3);
    }
}
```

`PlayerMotor` represents the platformer's movement component. Its exact implementation depends on the game.

A simple example interface might look like:

```csharp
public class PlayerMotor
{
    public void StopHorizontalMovement()
    {
    }

    public void ApplyHorizontalMovement(float input)
    {
    }

    public void BeginJump()
    {
    }

    public void ApplyAirMovement(float input)
    {
    }

    public void ApplyGravity()
    {
    }
}
```

These methods are example game APIs rather than required Dreambit APIs.

---

## Creating the blackboard

Install the blackboard on the FSM:

```csharp
_blackboard = Fsm.SetBlackboard<PlayerBlackboard>();
```

Then configure its initial references:

```csharp
_blackboard.Motor.Value = playerMotor;
_blackboard.Health.Value = 3;
```

---

## Retrieving the blackboard

Retrieve the typed blackboard from the FSM:

```csharp
var data =
    Fsm.GetBlackboard<PlayerBlackboard>();
```

Read a value:

```csharp
var grounded =
    data.IsGrounded.Value;
```

Update a value:

```csharp
data.MoveInputX.Value =
    horizontalInput;
```

---

## Accessing the blackboard from states

A common base state can expose the correctly typed blackboard:

```csharp
using Dreambit;

public abstract class PlayerState : State
{
    protected PlayerBlackboard Data =>
        Fsm.GetBlackboard<PlayerBlackboard>();

    protected PlayerMotor Motor =>
        Data.Motor.Value;
}
```

Platformer states can then inherit from `PlayerState`:

```csharp
public sealed class PlayerIdleState : PlayerState
{
    public override void OnExecute()
    {
        Motor.StopHorizontalMovement();
    }
}
```

This avoids repeating the blackboard cast in every state.

---

# Creating platformer states

The following states form a basic platformer movement FSM.

## Idle state

```csharp
public sealed class PlayerIdleState : PlayerState
{
    public override void OnEnter()
    {
        Logger.Info("Player entered IdleState");
    }

    public override void OnExecute()
    {
        Motor.StopHorizontalMovement();
    }
}
```

The idle state does not need to decide when to leave. Registered transition guards handle that.

---

## Run state

```csharp
public sealed class PlayerRunState : PlayerState
{
    public override void OnEnter()
    {
        Logger.Info("Player entered RunState");
    }

    public override void OnExecute()
    {
        Motor.ApplyHorizontalMovement(
            Data.MoveInputX.Value);
    }
}
```

---

## Jump state

```csharp
public sealed class PlayerJumpState : PlayerState
{
    public override void OnEnter()
    {
        Logger.Info("Player entered JumpState");

        Motor.BeginJump();

        // The jump request has now been handled.
        Data.JumpRequested.Value = false;
    }

    public override void OnExecute()
    {
        Motor.ApplyAirMovement(
            Data.MoveInputX.Value);

        Motor.ApplyGravity();
    }
}
```

---

## Fall state

```csharp
public sealed class PlayerFallState : PlayerState
{
    public override void OnEnter()
    {
        Logger.Info("Player entered FallState");
    }

    public override void OnExecute()
    {
        Motor.ApplyAirMovement(
            Data.MoveInputX.Value);

        Motor.ApplyGravity();
    }
}
```

---

## Hurt state

```csharp
public sealed class PlayerHurtState : PlayerState
{
    private float _remainingDuration;

    public override void OnEnter()
    {
        _remainingDuration = 0.35f;

        Data.IsInvulnerable.Value = true;

        Logger.Info("Player entered HurtState");
    }

    public override void OnExecute()
    {
        // Replace with the engine's actual delta-time value.
        _remainingDuration -= GameTime.DeltaSeconds;

        if (_remainingDuration <= 0f)
        {
            Data.IsInvulnerable.Value = false;

            Fsm.SetNextState<PlayerIdleState>(
                "Hurt recovery completed");
        }
    }

    public override void OnEnd()
    {
        Data.IsInvulnerable.Value = false;
    }
}
```

`GameTime.DeltaSeconds` is illustrative. Replace it with the project's actual frame-delta API.

---

## Dead state

```csharp
public sealed class PlayerDeadState : PlayerState
{
    public override void OnEnter()
    {
        Motor.StopHorizontalMovement();

        Logger.Info("Player entered DeadState");
    }

    public override void OnExecute()
    {
        // Play death animation or wait for respawn.
    }
}
```

Because death is normally terminal until the player respawns, this state does not need an automatic outgoing transition.

---

# State-specific transitions

Use `AddTransition<TFrom, TTo>` for a transition that only applies while a particular source state is active.

```csharp
Fsm.AddTransition<PlayerIdleState, PlayerRunState>(
    machine =>
    {
        var data =
            machine.GetBlackboard<PlayerBlackboard>();

        return MathF.Abs(data.MoveInputX.Value) > 0.01f;
    });
```

The generic parameters identify the connection:

```text
TFrom = PlayerIdleState
TTo   = PlayerRunState
```

The lambda is the transition guard:

```csharp
machine =>
{
    return condition;
}
```

The result means:

| Result  | Behavior                    |
| ------- | --------------------------- |
| `true`  | Enter the destination state |
| `false` | Remain in the current state |

Conceptually, the FSM performs:

```csharp
if (CurrentState is PlayerIdleState)
{
    if (MathF.Abs(data.MoveInputX.Value) > 0.01f)
    {
        ChangeState<PlayerRunState>();
    }
}
```

The guard is only relevant while `PlayerIdleState` is active.

---

# Any-state transitions

Use `AddAnyTransition<TTo>` for a transition that can interrupt nearly any active state.

```csharp
Fsm.AddAnyTransition<PlayerDeadState>(
    machine =>
    {
        var data =
            machine.GetBlackboard<PlayerBlackboard>();

        return data.Health.Value <= 0;
    });
```

This means:

> Enter `PlayerDeadState` whenever health reaches zero, regardless of the active state.

It replaces several repetitive transitions:

```text
PlayerIdleState ─────► PlayerDeadState
PlayerRunState ──────► PlayerDeadState
PlayerJumpState ─────► PlayerDeadState
PlayerFallState ─────► PlayerDeadState
PlayerHurtState ─────► PlayerDeadState
```

Any-state transitions are appropriate for:

* Death
* Disabling
* Despawning
* Global interruption states
* Game-over states
* Forced cutscene states

They should not be used for every ordinary movement transition.

---

# Complete platformer transition setup

The following transition table supports:

* Idle to run
* Idle to jump
* Run to idle
* Run to jump
* Jump to fall
* Fall to idle
* Fall to run
* Damage from most states
* Death from any state

```csharp
private void RegisterTransitions()
{
    Fsm.AddAnyTransition<PlayerDeadState>(
        machine =>
        {
            var data =
                machine.GetBlackboard<PlayerBlackboard>();

            return data.Health.Value <= 0;
        });

    Fsm.AddAnyTransition<PlayerHurtState>(
        machine =>
        {
            var data =
                machine.GetBlackboard<PlayerBlackboard>();

            return data.Health.Value > 0 &&
                   !data.IsInvulnerable.Value &&
                   machine.TryConsumeEvent("PlayerDamaged");
        });

    Fsm.AddTransition<
        PlayerIdleState,
        PlayerRunState>(
        machine =>
        {
            var data =
                machine.GetBlackboard<PlayerBlackboard>();

            return HasMovementInput(data);
        });

    Fsm.AddTransition<
        PlayerIdleState,
        PlayerJumpState>(
        machine =>
        {
            var data =
                machine.GetBlackboard<PlayerBlackboard>();

            return data.IsGrounded.Value &&
                   data.JumpRequested.Value;
        });

    Fsm.AddTransition<
        PlayerRunState,
        PlayerIdleState>(
        machine =>
        {
            var data =
                machine.GetBlackboard<PlayerBlackboard>();

            return !HasMovementInput(data);
        });

    Fsm.AddTransition<
        PlayerRunState,
        PlayerJumpState>(
        machine =>
        {
            var data =
                machine.GetBlackboard<PlayerBlackboard>();

            return data.IsGrounded.Value &&
                   data.JumpRequested.Value;
        });

    Fsm.AddTransition<
        PlayerJumpState,
        PlayerFallState>(
        machine =>
        {
            var data =
                machine.GetBlackboard<PlayerBlackboard>();

            return data.VerticalVelocity.Value <= 0f;
        });

    Fsm.AddTransition<
        PlayerFallState,
        PlayerIdleState>(
        machine =>
        {
            var data =
                machine.GetBlackboard<PlayerBlackboard>();

            return data.IsGrounded.Value &&
                   !HasMovementInput(data);
        });

    Fsm.AddTransition<
        PlayerFallState,
        PlayerRunState>(
        machine =>
        {
            var data =
                machine.GetBlackboard<PlayerBlackboard>();

            return data.IsGrounded.Value &&
                   HasMovementInput(data);
        });
}

private static bool HasMovementInput(
    PlayerBlackboard data)
{
    return MathF.Abs(data.MoveInputX.Value) > 0.01f;
}
```

---

# Transition ordering

When multiple guards pass during the same update, transition priority matters.

In the corrected FSM, the general priority is:

1. A pending transition requested before the update
2. Any-state transitions
3. A transition explicitly requested by the active state
4. State-specific transitions
5. Default-state fallback

Within the same category, the first registered passing transition wins.

Consider the idle transitions:

```csharp
Fsm.AddTransition<PlayerIdleState, PlayerRunState>(
    CanRun);

Fsm.AddTransition<PlayerIdleState, PlayerJumpState>(
    CanJump);
```

If both guards return `true`, `PlayerRunState` wins because it was registered first.

For a platformer, jump usually deserves higher priority:

```csharp
Fsm.AddTransition<PlayerIdleState, PlayerJumpState>(
    CanJump);

Fsm.AddTransition<PlayerIdleState, PlayerRunState>(
    CanRun);
```

Now pressing jump while holding a direction enters `PlayerJumpState`.

The same ordering should be used for `PlayerRunState`:

```csharp
Fsm.AddTransition<PlayerRunState, PlayerJumpState>(
    CanJump);

Fsm.AddTransition<PlayerRunState, PlayerIdleState>(
    ShouldIdle);
```

---

# Updating the blackboard

The FSM only knows what the game writes into its blackboard.

A platformer controller might update it every frame before the FSM runs:

```csharp
private void UpdateBlackboard()
{
    _blackboard.MoveInputX.Value =
        Input.GetAxis("MoveHorizontal");

    _blackboard.JumpRequested.Value =
        Input.IsActionPressed("Jump");

    _blackboard.IsGrounded.Value =
        _playerMotor.IsGrounded;

    _blackboard.VerticalVelocity.Value =
        _playerMotor.VerticalVelocity;
}
```

The update order should be:

```text
1. Read input
2. Update collision and movement facts
3. Write those facts to the blackboard
4. Update the FSM
```

If the blackboard is updated after the FSM, transitions will react one frame later.

---

# Complete controller example

```csharp
using System;
using Dreambit;
using Dreambit.ECS;

namespace ExamplePlatformer;

[Require(typeof(FSM))]
public sealed class PlayerStateController : Component
{
    private PlayerBlackboard _blackboard;
    private PlayerMotor _playerMotor;

    [FromRequired]
    private FSM Fsm { get; set; }

    public override void OnCreated()
    {
        _playerMotor =
            Entity.GetRequiredComponent<PlayerMotor>();

        ConfigureFsm();

        Fsm.OnTransition += HandleTransition;
    }

    public override void OnUpdate()
    {
        UpdateBlackboard();
    }

    private void ConfigureFsm()
    {
        _blackboard =
            Fsm.SetBlackboard<PlayerBlackboard>();

        _blackboard.Motor.Value =
            _playerMotor;

        Fsm.Register(
            typeof(PlayerIdleState),
            typeof(PlayerRunState),
            typeof(PlayerJumpState),
            typeof(PlayerFallState),
            typeof(PlayerHurtState),
            typeof(PlayerDeadState));

        RegisterTransitions();

        Fsm.SetDefaultState<PlayerIdleState>();
        Fsm.GoToDefault();
    }

    private void RegisterTransitions()
    {
        Fsm.AddAnyTransition<PlayerDeadState>(
            machine =>
            {
                var data =
                    machine.GetBlackboard<PlayerBlackboard>();

                return data.Health.Value <= 0;
            });

        Fsm.AddAnyTransition<PlayerHurtState>(
            machine =>
            {
                var data =
                    machine.GetBlackboard<PlayerBlackboard>();

                return data.Health.Value > 0 &&
                       !data.IsInvulnerable.Value &&
                       machine.TryConsumeEvent("PlayerDamaged");
            });

        Fsm.AddTransition<
            PlayerIdleState,
            PlayerJumpState>(
            CanJump);

        Fsm.AddTransition<
            PlayerIdleState,
            PlayerRunState>(
            HasMovementInput);

        Fsm.AddTransition<
            PlayerRunState,
            PlayerJumpState>(
            CanJump);

        Fsm.AddTransition<
            PlayerRunState,
            PlayerIdleState>(
            machine =>
                !HasMovementInput(machine));

        Fsm.AddTransition<
            PlayerJumpState,
            PlayerFallState>(
            machine =>
            {
                var data =
                    machine.GetBlackboard<PlayerBlackboard>();

                return data.VerticalVelocity.Value <= 0f;
            });

        Fsm.AddTransition<
            PlayerFallState,
            PlayerRunState>(
            machine =>
            {
                var data =
                    machine.GetBlackboard<PlayerBlackboard>();

                return data.IsGrounded.Value &&
                       HasMovementInput(machine);
            });

        Fsm.AddTransition<
            PlayerFallState,
            PlayerIdleState>(
            machine =>
            {
                var data =
                    machine.GetBlackboard<PlayerBlackboard>();

                return data.IsGrounded.Value &&
                       !HasMovementInput(machine);
            });
    }

    private void UpdateBlackboard()
    {
        _blackboard.MoveInputX.Value =
            Input.GetAxis("MoveHorizontal");

        if (Input.IsActionPressed("Jump"))
            _blackboard.JumpRequested.Value = true;

        _blackboard.IsGrounded.Value =
            _playerMotor.IsGrounded;

        _blackboard.VerticalVelocity.Value =
            _playerMotor.VerticalVelocity;
    }

    private static bool CanJump(FSM machine)
    {
        var data =
            machine.GetBlackboard<PlayerBlackboard>();

        return data.IsGrounded.Value &&
               data.JumpRequested.Value;
    }

    private static bool HasMovementInput(FSM machine)
    {
        var data =
            machine.GetBlackboard<PlayerBlackboard>();

        return MathF.Abs(
            data.MoveInputX.Value) > 0.01f;
    }

    private void HandleTransition(
        Type previousState,
        Type nextState,
        string reason)
    {
        Logger.Info(
            "Player state: {0} -> {1}. Reason: {2}",
            previousState?.Name ?? "None",
            nextState?.Name ?? "None",
            reason ?? "Guard");
    }

    public void ApplyDamage(int amount)
    {
        if (_blackboard.IsInvulnerable.Value)
            return;

        _blackboard.Health.Value -= amount;

        Fsm.Trigger("PlayerDamaged");
    }

    public override void OnDestroyed()
    {
        Fsm.OnTransition -= HandleTransition;

        base.OnDestroyed();
    }
}
```

The input names and `PlayerMotor` API are illustrative. Replace them with the project's actual input and movement APIs.

---

# Requesting transitions directly

A state or external component can explicitly request a destination state.

```csharp
Fsm.SetNextState<PlayerFallState>();
```

A transition reason can also be provided:

```csharp
Fsm.SetNextState<PlayerFallState>(
    "Walked off platform");
```

Direct transitions are useful when the destination is already known.

For example, a scripted launch pad could force the player into the jump state:

```csharp
public void ActivateLaunchPad()
{
    Fsm.SetNextState<PlayerJumpState>(
        "Launch pad activated");
}
```

From inside a state, use the protected `Go<TState>` helper:

```csharp
public override bool Reason()
{
    if (_recoveryFinished)
    {
        Go<PlayerIdleState>();
        return false;
    }

    return true;
}
```

Prefer guarded transitions for continuously observed conditions.

Prefer direct transitions for:

* Scripted actions
* Animation completion
* Ability completion
* Cutscene commands
* Explicit cancellation
* Known one-time outcomes

---

# Event-driven transitions

The FSM includes a string event queue.

Queue an event with:

```csharp
Fsm.Trigger("PlayerDamaged");
```

Consume it inside a guard:

```csharp
Fsm.AddAnyTransition<PlayerHurtState>(
    machine =>
    {
        var data =
            machine.GetBlackboard<PlayerBlackboard>();

        return data.Health.Value > 0 &&
               machine.TryConsumeEvent("PlayerDamaged");
    });
```

`TryConsumeEvent` removes the first matching event from the queue.

The flow is:

```text
Player takes damage
        |
        v
Health is reduced
        |
        v
"PlayerDamaged" is queued
        |
        v
FSM evaluates any-state transitions
        |
        v
PlayerHurtState guard consumes the event
        |
        v
FSM enters PlayerHurtState
```

## Use constants for event names

Avoid repeating raw string values throughout the game.

```csharp
public static class PlayerFsmEvents
{
    public const string Damaged = "PlayerDamaged";
    public const string Respawned = "PlayerRespawned";
}
```

Then use:

```csharp
Fsm.Trigger(PlayerFsmEvents.Damaged);
```

```csharp
machine.TryConsumeEvent(
    PlayerFsmEvents.Damaged);
```

This reduces spelling mistakes.

---

# Consuming events safely

Avoid consuming an event before validating persistent conditions.

Less safe:

```csharp
var damaged =
    machine.TryConsumeEvent("PlayerDamaged");

var alive =
    data.Health.Value > 0;

return damaged && alive;
```

This consumes the event even when the player is already dead.

Prefer:

```csharp
return data.Health.Value > 0 &&
       machine.TryConsumeEvent("PlayerDamaged");
```

Because `&&` short-circuits, the event is only consumed when the player is still alive.

---

# State history

The FSM can keep track of previously active states.

Return to the previous state with:

```csharp
Fsm.Revert();
```

For example:

```text
PlayerRunState
      |
      v
PlayerHurtState
      |
      | Revert()
      v
PlayerRunState
```

Set the maximum history capacity:

```csharp
Fsm.SetHistoryCapacity(8);
```

Clear the history:

```csharp
Fsm.ClearHistory();
```

History is useful for temporary interruption states such as:

* Pause
* Stun
* Dialogue
* Inspection mode
* Temporary lock-on mode

For platformer movement, explicit destination transitions are often easier to reason about than history.

For example, after recovering from damage, the player may need to enter:

* `PlayerIdleState` when grounded
* `PlayerFallState` when airborne

That decision is better represented by guards than by blindly reverting.

---

# Transition events

The FSM exposes events for observing state changes.

## `OnTransition`

Raised after a transition completes.

```csharp
Fsm.OnTransition += (
    previousState,
    nextState,
    reason) =>
{
    Logger.Info(
        "{0} -> {1}: {2}",
        previousState?.Name ?? "None",
        nextState.Name,
        reason ?? "No reason");
};
```

Use it for:

* Debug logging
* Analytics
* Animation coordination
* State visualizers
* Development overlays

---

## `OnStateEntered`

Raised when a state becomes active.

```csharp
Fsm.OnStateEntered += stateType =>
{
    Logger.Info(
        "Entered {0}",
        stateType.Name);
};
```

---

## `OnStateExited`

Raised when a state stops being active.

```csharp
Fsm.OnStateExited += stateType =>
{
    Logger.Info(
        "Exited {0}",
        stateType.Name);
};
```

Always unsubscribe when the owning component is destroyed:

```csharp
public override void OnDestroyed()
{
    Fsm.OnTransition -= HandleTransition;

    base.OnDestroyed();
}
```

---

# Transition guards

A transition guard should usually:

1. Read blackboard values.
2. Evaluate a condition.
3. Return `true` or `false`.

Good:

```csharp
machine =>
{
    var data =
        machine.GetBlackboard<PlayerBlackboard>();

    return data.IsGrounded.Value &&
           data.JumpRequested.Value;
}
```

Avoid guards that perform unrelated gameplay actions:

```csharp
machine =>
{
    SpawnParticleEffect();
    PlaySound();
    DealDamage();

    return true;
}
```

Those actions belong in:

* `OnEnter`
* `OnExecute`
* `OnEnd`
* The gameplay system that caused the condition

Consuming a queued FSM event is an acceptable guard-side effect because it is part of deciding whether the transition should occur.

---

# Unconditional transitions

A transition guard is optional.

```csharp
Fsm.AddTransition<
    PlayerHurtState,
    PlayerIdleState>();
```

A transition without a guard always passes whenever it is evaluated.

Use unconditional transitions carefully.

This setup is problematic:

```csharp
Fsm.AddTransition<
    PlayerIdleState,
    PlayerRunState>();

Fsm.AddTransition<
    PlayerIdleState,
    PlayerJumpState>(
        CanJump);
```

The unconditional transition always wins, so the jump transition is never reached.

Put unconditional transitions last, or use explicit state changes instead.

---

# Common movement transition patterns

## Idle to run

```csharp
Fsm.AddTransition<
    PlayerIdleState,
    PlayerRunState>(
    machine =>
    {
        var data =
            machine.GetBlackboard<PlayerBlackboard>();

        return MathF.Abs(
            data.MoveInputX.Value) > 0.01f;
    });
```

---

## Run to idle

```csharp
Fsm.AddTransition<
    PlayerRunState,
    PlayerIdleState>(
    machine =>
    {
        var data =
            machine.GetBlackboard<PlayerBlackboard>();

        return MathF.Abs(
            data.MoveInputX.Value) <= 0.01f;
    });
```

---

## Grounded to jump

Register this transition from every grounded movement state:

```csharp
Fsm.AddTransition<
    PlayerIdleState,
    PlayerJumpState>(
    CanJump);

Fsm.AddTransition<
    PlayerRunState,
    PlayerJumpState>(
    CanJump);
```

```csharp
private static bool CanJump(FSM machine)
{
    var data =
        machine.GetBlackboard<PlayerBlackboard>();

    return data.IsGrounded.Value &&
           data.JumpRequested.Value;
}
```

---

## Jump to fall

```csharp
Fsm.AddTransition<
    PlayerJumpState,
    PlayerFallState>(
    machine =>
    {
        var data =
            machine.GetBlackboard<PlayerBlackboard>();

        return data.VerticalVelocity.Value <= 0f;
    });
```

---

## Fall to idle

```csharp
Fsm.AddTransition<
    PlayerFallState,
    PlayerIdleState>(
    machine =>
    {
        var data =
            machine.GetBlackboard<PlayerBlackboard>();

        var hasMovement =
            MathF.Abs(data.MoveInputX.Value) > 0.01f;

        return data.IsGrounded.Value &&
               !hasMovement;
    });
```

---

## Fall to run

```csharp
Fsm.AddTransition<
    PlayerFallState,
    PlayerRunState>(
    machine =>
    {
        var data =
            machine.GetBlackboard<PlayerBlackboard>();

        var hasMovement =
            MathF.Abs(data.MoveInputX.Value) > 0.01f;

        return data.IsGrounded.Value &&
               hasMovement;
    });
```

---

## Any state to dead

```csharp
Fsm.AddAnyTransition<PlayerDeadState>(
    machine =>
    {
        var data =
            machine.GetBlackboard<PlayerBlackboard>();

        return data.Health.Value <= 0;
    });
```

---

# Debugging transitions

When a transition does not work, check the entire chain.

## Confirm the source state is active

```csharp
Logger.Info(
    "Current state: {0}",
    Fsm.CurrentState?.GetType().Name ?? "None");
```

A transition from `PlayerIdleState` is only considered while `PlayerIdleState` is active.

---

## Confirm all states are registered

```csharp
Fsm.Register(
    typeof(PlayerIdleState),
    typeof(PlayerRunState));
```

A transition cannot reference an unregistered state.

---

## Confirm the FSM has started

```csharp
Fsm.SetDefaultState<PlayerIdleState>();
Fsm.GoToDefault();
```

If `CurrentState` is `null`, no state-specific transition can run.

---

## Confirm the blackboard is installed

```csharp
_blackboard =
    Fsm.SetBlackboard<PlayerBlackboard>();
```

Then verify required references:

```csharp
_blackboard.Motor.Value =
    playerMotor;
```

A null movement component may cause the destination state's `OnEnter` or `OnExecute` to fail.

---

## Confirm the blackboard is updated before the FSM

The transition guard can only observe the latest values that have been written.

Incorrect order:

```text
1. FSM update
2. Read input
3. Update blackboard
```

Correct order:

```text
1. Read input
2. Update blackboard
3. FSM update
```

---

## Log guard values

Temporarily inspect the values used by a guard:

```csharp
Fsm.AddTransition<
    PlayerIdleState,
    PlayerJumpState>(
    machine =>
    {
        var data =
            machine.GetBlackboard<PlayerBlackboard>();

        Logger.Info(
            "Jump guard: Grounded={0}, Requested={1}",
            data.IsGrounded.Value,
            data.JumpRequested.Value);

        return data.IsGrounded.Value &&
               data.JumpRequested.Value;
    });
```

Remove per-frame guard logging after debugging. Otherwise, the log file may achieve sentience.

---

## Check transition ordering

The first passing transition in the same category wins.

```csharp
Fsm.AddTransition<
    PlayerIdleState,
    PlayerRunState>(
    CanRun);

Fsm.AddTransition<
    PlayerIdleState,
    PlayerJumpState>(
    CanJump);
```

If both pass, the player enters `PlayerRunState`.

Register jump first when it should have higher priority:

```csharp
Fsm.AddTransition<
    PlayerIdleState,
    PlayerJumpState>(
    CanJump);

Fsm.AddTransition<
    PlayerIdleState,
    PlayerRunState>(
    CanRun);
```

---

## Check the destination state's lifecycle methods

A transition may succeed but appear broken when `OnEnter` throws an exception.

Validate required references:

```csharp
public override void OnEnter()
{
    if (Motor == null)
    {
        Logger.Error(
            "Cannot enter PlayerJumpState: Motor is null.");

        Fsm.SetNextState<PlayerIdleState>(
            "Missing PlayerMotor");

        return;
    }

    Motor.BeginJump();
}
```

---

## Check one-frame flags

A jump request may be cleared too early.

Problematic:

```csharp
_blackboard.JumpRequested.Value =
    Input.IsActionPressed("Jump");

_blackboard.JumpRequested.Value = false;
```

The FSM never sees the request.

Prefer clearing it when the jump is accepted:

```csharp
public override void OnEnter()
{
    Data.JumpRequested.Value = false;
    Motor.BeginJump();
}
```

---

# Best practices

## Keep each state focused

Good platformer states:

```text
PlayerIdleState
PlayerRunState
PlayerJumpState
PlayerFallState
PlayerWallSlideState
PlayerDashState
PlayerHurtState
PlayerDeadState
```

Avoid one large `PlayerMovementState` containing every behavior and a forest of nested conditions. That is just an FSM wearing a trench coat.

---

## Separate movement from state decisions

The state should decide which movement behavior is active.

A dedicated movement component should perform the actual physics:

```text
FSM state
    |
    v
PlayerMotor
    |
    v
Velocity, collision, and movement
```

For example:

```csharp
public override void OnExecute()
{
    Motor.ApplyAirMovement(
        Data.MoveInputX.Value);

    Motor.ApplyGravity();
}
```

This keeps state classes small and testable.

---

## Keep shared facts in the blackboard

Good blackboard values include:

* Movement input
* Grounded status
* Vertical velocity
* Health
* Jump request
* Dash availability
* Current movement component

State-local values should remain private fields when no other state or guard needs them.

For example, a hurt-state recovery timer can remain inside `PlayerHurtState`.

---

## Consume transient requests once

Inputs such as jump, dash, or attack are requests rather than permanent facts.

Set the request:

```csharp
data.JumpRequested.Value = true;
```

Consume or clear it when the destination state accepts it:

```csharp
public override void OnEnter()
{
    Data.JumpRequested.Value = false;
}
```

This prevents the player from repeatedly entering the same action state.

---

## Use any-state transitions sparingly

Good uses:

* Death
* Forced stun
* Despawn
* Global disable
* Game over

Poor uses:

* Idle to run
* Run to idle
* Jump to fall
* Ordinary animation changes

Ordinary transitions should identify their valid source state.

---

## Give important transitions priority

Register urgent or specific conditions first.

Recommended:

```csharp
Fsm.AddTransition<
    PlayerIdleState,
    PlayerJumpState>(
    CanJump);

Fsm.AddTransition<
    PlayerIdleState,
    PlayerRunState>(
    CanRun);
```

Any-state transitions such as death should have higher priority than ordinary locomotion.

---

## Use transition reasons

Reasons make logs and debugging tools easier to understand.

```csharp
Fsm.SetNextState<PlayerFallState>(
    "Ground disappeared");
```

```csharp
Fsm.SetNextState<PlayerIdleState>(
    "Hurt recovery completed");
```

---

## Avoid hidden state changes

Do not update major blackboard facts inside unrelated transition guards.

Avoid:

```csharp
machine =>
{
    data.Health.Value--;
    return data.Health.Value <= 0;
}
```

Prefer:

```csharp
public void ApplyDamage(int amount)
{
    data.Health.Value -= amount;
}
```

Then let the transition guard observe health:

```csharp
return data.Health.Value <= 0;
```

---

# API quick reference

## Register states

```csharp
Fsm.Register(
    typeof(PlayerIdleState),
    typeof(PlayerRunState));
```

---

## Set a blackboard

```csharp
var data =
    Fsm.SetBlackboard<PlayerBlackboard>();
```

---

## Retrieve a blackboard

```csharp
var data =
    Fsm.GetBlackboard<PlayerBlackboard>();
```

---

## Set the initial state

```csharp
Fsm.SetDefaultState<PlayerIdleState>();
Fsm.GoToDefault();
```

---

## Add a state-specific transition

```csharp
Fsm.AddTransition<
    PlayerIdleState,
    PlayerRunState>(
    machine => condition);
```

---

## Add an any-state transition

```csharp
Fsm.AddAnyTransition<PlayerDeadState>(
    machine => condition);
```

---

## Request a direct transition

```csharp
Fsm.SetNextState<PlayerFallState>();
```

---

## Request a direct transition with a reason

```csharp
Fsm.SetNextState<PlayerFallState>(
    "Walked off platform");
```

---

## Queue an event

```csharp
Fsm.Trigger("PlayerDamaged");
```

---

## Consume an event

```csharp
machine.TryConsumeEvent("PlayerDamaged");
```

---

## Return to the previous state

```csharp
Fsm.Revert();
```

---

## Clear queued events

```csharp
Fsm.ClearEvents();
```

---

## Observe transitions

```csharp
Fsm.OnTransition += HandleTransition;
```

---

# Summary

The core pattern is:

```text
States perform behavior.
The blackboard stores shared facts.
Transition guards inspect those facts every frame.
The FSM changes state when a guard returns true.
```

A state-specific transition:

```csharp
Fsm.AddTransition<
    PlayerIdleState,
    PlayerRunState>(
    condition);
```

means:

> While `PlayerIdleState` is active, enter `PlayerRunState` when the condition returns `true`.

An any-state transition:

```csharp
Fsm.AddAnyTransition<PlayerDeadState>(
    condition);
```

means:

> Enter `PlayerDeadState` from any active state when the condition returns `true`.

A complete FSM requires:

1. A blackboard
2. Registered states
3. Registered transitions
4. A default or explicitly selected initial state
5. Updated blackboard data
6. An active FSM update loop
