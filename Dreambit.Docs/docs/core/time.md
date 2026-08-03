# Time

Use `Time.DeltaTime` for frame-rate-independent movement:

```csharp
Transform.TranslateWorld2D(velocity * Time.DeltaTime);
```

`Time.DeltaTime` is scaled game time. `Time.UnscaledDeltaTime` continues to
represent real frame time when the game time scale changes. Prefer unscaled time
for menus, fades that must continue while paused, and diagnostics.

Physics callbacks run at the engine's nominal 1/60-second cadence. The current
game loop does not call `Time.UpdatePhysicsTime`, so `PhysicsDeltaTime` and the
physics clock properties are not advanced. Until that hook is connected, use an
explicit `1f / 60f` in fixed-step code or keep time-based movement in the normal
frame update. Do not read the current physics-time properties as live values.

For delayed sequences, prefer [coroutines](coroutines.md) over manually
accumulating timers in every component.
