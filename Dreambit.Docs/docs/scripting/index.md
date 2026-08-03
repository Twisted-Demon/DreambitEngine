# Scripting and cutscenes

The scripting system loads YAML sequences of parallel action groups. Actions in
one group update together; the next group starts after every current action is
complete.

```yaml
- scriptGroup:
    - script: EnableEntityScript
      entity: captain
    - script: CameraFollowScript
      entity: captain
- scriptGroup:
    - script: WaitScript
      duration: 1.5
- scriptGroup:
    - script: MoveScript
      entity: captain
      speed: 80
      moveTo: [320, 180]
```

Start it through the scene:

```csharp
Scene.StartCutscene("Cutscenes/intro");
```

The manager reads `Content/<name>.yaml` directly, caches parsed sequences, and
allows only one active sequence. Subscribe to `OnScriptingStart` and
`OnScriptingEnd` on `Scene.ScriptingManager` to adjust gameplay state.

Built-in action types are `WaitScript`, `MoveScript`, `EnableEntityScript`,
`SetAnimationScript`, and `CameraFollowScript`. The factory finds actions by
class name across loaded assemblies and fills constructor parameters from YAML.

Create a custom action by deriving from `ScriptAction`, implementing `OnUpdate`,
and setting `IsComplete = true` when finished. Optional hooks are `OnStart`,
`OnCompleted`, and `OnGroupEnd`.

!!! warning "Current format limits"
    The factory supports scalar values, arrays, and two-element `Vector2` values.
    Its current `Vector3` conversion has an inconsistent length check and should
    not be used without fixing it. Cutscenes also bypass the pak and require
    loose YAML files under `Content`.

