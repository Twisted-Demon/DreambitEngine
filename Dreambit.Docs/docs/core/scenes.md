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
Scene.SetNextScene<GameplayScene>("Scenes/world");
```

The switch occurs on the next engine update.

## Creating an editor-authored scene

Use `CreateFromBlueprint<TScene>` when another system needs a fully authored Scene instance without
scheduling a local transition:

```csharp
var scene = Scene.CreateFromBlueprint<GameplayScene>("Scenes/world");
```

The factory constructs `GameplayScene`, loads the `.scene` asset immediately while the Scene is still
in `Created`, and returns it without running initialization. If materialization fails, it disposes the
partially constructed Scene before rethrowing the error.

For a Scene Blueprint linked to Tiled, `GameplayScene` must derive from `TiledScene`. Networking uses
this same factory through `NetworkSceneCatalog.RegisterBlueprint<TScene>` so every peer constructs the
same typed host before synchronization starts.

## Finding entities

```csharp
var player = FindEntity("player");
var enemies = GetActiveEntitiesByTag("enemy");
var all = GetAllActiveEntities();
```

Names are convenient for unique managers. Tags are better for groups. Keep the
returned references only while this scene remains active.

