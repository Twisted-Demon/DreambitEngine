# Core and the game loop

`Core` derives from MonoGame's `Game` and owns the only application loop. Create
one instance, select an initial scene, then call `Run`.

```csharp
using var game = new Core(1280, 720, "Sky Harbor");
Core.Level = LogLevel.Info;
Core.SetTargetFps(120);
Scene.SetNextScene<MainMenuScene>();
game.Run();
```

## Frame order

Each frame, `Core` updates time and window state, samples input, routes UI input,
updates input actions, changes a pending scene, runs fixed physics when due, and
ticks the current scene. Drawing is delegated to the scene's render pipeline.

The engine uses a 1/60-second physics step. `Core.SetFixedTimeStep` controls
MonoGame's outer loop; it does not change this physics interval.

## Scene changes

Scene changes are deferred until the update loop. When a change happens, the old
scene terminates, physics and audio registries are cleaned, and the new scene is
initialized. Never retain entities or components from the previous scene.

## Global access

`Core.Instance`, `Core.SpriteBatch`, and `Core.GraphicsDeviceManager` are exposed
for engine integrations. Gameplay should prefer the higher-level scene,
resource, rendering, and window APIs where possible.

