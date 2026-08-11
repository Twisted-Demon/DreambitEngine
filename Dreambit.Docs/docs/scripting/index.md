# Scripting and cutscenes

Cutscenes are YAML-authored Dreambit assets made of parallel action groups.
Actions in one group update together; the next group starts after every current
action is complete.

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

Put the source at `Assets/Cutscenes/intro.yaml`. The asset baker writes it to the
content pak as `Cutscenes/intro.yamlb`, and `Resources.LoadAsset<Cutscene>` loads
and caches that data. The extension is not part of the asset name.

You can also load once and pass the asset directly:

```csharp
var intro = Resources.LoadAsset<Cutscene>("Cutscenes/intro");
Scene.StartCutscene(intro);
```

Only one cutscene can run at a time. Both `StartCutscene` overloads return
`false` when another cutscene is active or the named asset cannot be loaded.
Subscribe to `OnScriptingStart` and `OnScriptingEnd` on
`Scene.ScriptingManager` to adjust gameplay state.

Built-in action types are `WaitScript`, `MoveScript`, `EnableEntityScript`,
`SetAnimationScript`, and `CameraFollowScript`. The factory finds actions by
class name across loaded assemblies and fills constructor parameters from YAML.
Use a fully-qualified type name if two actions have the same class name.

Create a custom action by deriving from `ScriptAction`, implementing `OnUpdate`,
and setting `IsComplete = true` when finished. Optional hooks are `OnStart`,
`OnCompleted`, and `OnGroupEnd`.

Constructor parameter names are the YAML argument names. Missing required
arguments, unknown arguments, malformed vectors, and unknown action types are
reported when the cutscene starts. Scalars, enums, arrays, `Vector2`, and
`Vector3` are supported. Every playback creates fresh action instances, so an
asset can be replayed without carrying action state from the previous run.

