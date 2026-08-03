# Scenes

A scene is a level, menu, or self-contained game state. Override its protected
hooks; do not call `Tick`, `PhysicsTick`, or `OnDraw` yourself.

```csharp
public sealed class ArenaScene : Scene<ArenaScene>
{
    protected override void OnInitialize()
    {
        BackgroundColor = Color.Black;
        CreateEntity("arena-manager").AttachComponent<ArenaManager>();
    }

    protected override void OnBegin() { }
    protected override void OnUpdate() { }
    protected override void OnPhysicsUpdate() { }
    protected override void OnEnd() { }
}
```

`OnInitialize` is the normal place to configure cameras and create entities.
`OnBegin` runs when the scene starts. `OnUpdate` runs every game frame,
`OnPhysicsUpdate` on the fixed physics tick, and `OnEnd` during termination.

Every initialized scene creates a `MainCamera`, `UiCamera`, and `AmbientLight`.
It also owns rendering options, post-process settings, a coroutine service, and
the scripting manager.

## Switching scenes

```csharp
Scene.SetNextScene<GameScene>();
Scene.SetNextScene(new ResultsScene(score));
```

The switch occurs on the next engine update. For LDtk levels, use the GUID-based
helpers described in [Loading LDtk levels](../ldtk/loading-levels.md).

## Finding entities

```csharp
var player = FindEntity("player");
var enemies = GetActiveEntitiesByTag("enemy");
var all = GetAllActiveEntities();
```

Names are convenient for unique managers. Tags are better for groups. Keep the
returned references only while this scene remains active.

