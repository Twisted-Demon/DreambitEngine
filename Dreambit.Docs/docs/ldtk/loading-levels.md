# Loading worlds and levels

For the shortest renderable example, begin with the
[LDtk quick start](quick-start.md). This page describes the lower-level loading,
caching, and streaming APIs.

## Loading raw models

Load a baked project through Dreambit resources using its extensionless logical
asset name:

```csharp
using System;
using Dreambit;
using Dreambit.LDtk;

var project = Resources.LoadAsset<LDtkFile>("ldtk/dreambit")
    ?? throw new InvalidOperationException("LDtk project was not baked.");
var world = project.LoadWorld();
var level = world.LoadLevel("Level_0");
```

`LoadWorld()` succeeds for a legacy project or a project with one world. With
multiple worlds it throws `LdtkWorldSelectionRequiredException`. Use
`AvailableWorlds` to present a choice and select by identifier or IID:

```csharp
foreach (var available in project.AvailableWorlds)
    Console.WriteLine($"{available.Identifier}: {available.Iid}");

var world = project.LoadWorld(selectedWorldIid);
var level = world.LoadLevel(selectedLevelIid);
```

External `.ldtkl` files are loaded lazily and cached when `LoadLevel` is called.
Embedded levels are already attached to the project and use the same API.

For tools and tests, an unbaked project can be loaded directly from disk. This
API uses physical filesystem paths rather than content asset names:

```csharp
var project = LDtkFile.FromFile("Content/World.ldtk");
```

Resolved external resources are available without altering raw LDtk paths:

```csharp
var tileset = project.GetTileset(tilesetUid);
string? textureAsset = tileset.AssetName;       // baked resource name
string? textureSource = tileset.SourcePath;     // resolved source path

string? backgroundAsset = level.BackgroundAssetName;
```

Field instances retain their raw values while also carrying a project binding.
This is what allows FilePath and EntityRef helpers to resolve other project
data.

## The global manager

`LDtkManager.Instance` is the repository used by `LDtkScene`:

```csharp
var manager = LDtkManager.Instance;
manager.Initialize("ldtk/dreambit");

var world = manager.LoadWorld("Dreambit_World");
var level = manager.LoadLevel(world, "Village");
```

The manager stores one active project and caches raw worlds and levels by IID.
Calling `Initialize` with another project asset installs that project and clears
the caches. `ClearCache` retains the active project; `Reset` also removes it.

Normally a game should let `LDtkScene` call the manager. Direct manager access is
most useful to inspect project metadata without materializing rendering
entities.

## Rendering one world as a scene

Derive from `LDtkScene` to select one world and render all of its levels by
default:

```csharp
public sealed class OverworldScene : LDtkScene
{
    public OverworldScene()
        : base("ldtk/dreambit", "Dreambit_World")
    {
    }
}
```

An `LDtkScene` can also select a world by IID when identifiers are expected to
change:

```csharp
public OverworldScene(Guid worldIid)
    : base("ldtk/dreambit", worldIid)
{
}
```

At the end of initialization these properties are available:

| Property | Meaning |
| --- | --- |
| `Project` | The active raw `LDtkFile`. |
| `World` | The selected `LDtkLoadedWorld`. |
| `LoadedLevels` | Scene-owned runtime instances keyed by level IID. |

Use `OnBeforeLDtkLevelsLoaded` for scene setup that must happen before imports,
and `OnLDtkSceneReady` for logic that requires the initial levels. `LDtkScene`
owns the base initialization/end hooks, so derived scenes should not try to
override `OnInitialize` or `OnEnd`.

## Selected loading and streaming

For streaming, select only the initial levels and call `LoadLevel` and
`UnloadLevel` as the player moves:

```csharp
public OverworldScene()
    : base(
        "ldtk/dreambit",
        "Dreambit_World",
        LDtkLevelLoadMode.Selected,
        "Village")
{
}

// Later:
var forest = LoadLevel("Forest");
UnloadLevel("Village");
```

Identifiers and IIDs are both supported:

```csharp
LDtkLevelInstance level = LoadLevel(levelIid);
bool present = IsLevelLoaded(levelIid);
bool unloaded = UnloadLevel(levelIid);
```

The returned `LDtkLevelInstance` owns every rendering entity created for that
level. Raw `LDtkLevel` models remain cached by `LDtkManager`, so reloading a
streamed level avoids deserializing it again.

Important members on a loaded instance are:

| Member | Purpose |
| --- | --- |
| `Level` | Raw LDtk level model. |
| `RootEntity` | Parent positioned at the level's world origin. |
| `TilemapRenderers` | Dreambit renderers created for tile layers. |
| `EntityInstances` | Flat list passed to the entity hook. |
| `ImportOptions` / `PixelsPerUnit` | Effective settings used for this materialized level. |
| `GetLocalPosition(entity)` | Scaled, level-local entity pivot including its layer offset. |
| `GetDrawLayer(entity)` | Draw layer computed from the owning LDtk layer. |
| `ApplyDrawLayer(entity, instance)` | Assigns that layer to existing drawable components. |
| `OwnedEntities` | Everything removed when the level unloads. |
| `TrackEntity(entity)` | Adds a game-created entity to that ownership set. |

`LoadAllLevels()` can materialize everything after a scene started in selected
mode. Unloading is scene-local: it never removes the raw model from the global
cache.

## Level world positions

Use `World.GetLevelWorldPosition(levelIid)` instead of reading `WorldX` and
`WorldY` directly. For free and GridVania layouts it returns the exported
coordinates. For linear layouts it derives the correct position from level
order and size because LDtk may export `-1` coordinates there.

The returned position is in LDtk pixels. Divide it by `PixelsPerUnit` when
working in Dreambit world units.

## Import options

Override `CreateLDtkImportOptions` to change pixel scaling or draw-layer ranges:

```csharp
protected override LDtkImportOptions CreateLDtkImportOptions() => new()
{
    PixelsPerUnit = 16f,
    BaseDrawLayer = -100,
    DrawLayerStep = 1,
    WorldDepthDrawLayerStride = 1000,
    RenderLevelBackgroundColor = true,
    RenderLevelBackgroundImage = true,
    IncludeInvisibleLayers = false,
};
```

`PixelsPerUnit` applies to level origins, layer offsets, tile sizes, backgrounds,
background images, and entities created by the default hook. Custom entity hooks
should use `LDtkLevelInstance.GetLocalPosition` or `CreateEntityData` so they use
the same value automatically.

`BaseDrawLayer` is the background-color layer for world depth zero. The
background image and LDtk visual layers are placed above it. `DrawLayerStep`
controls spacing between those layers, and `WorldDepthDrawLayerStride` reserves
a separate range for each LDtk `worldDepth` value.

When `IncludeInvisibleLayers` is false, entities on invisible layers are also
omitted from the flat `EntityInstances` callback list. They remain available in
the raw `Level.LayerInstances` data for tools that intentionally inspect hidden
content.

## Rendering behavior

Tile layers are converted to the LDtk-independent `TilemapRenderer` and
`TilemapLayerData` types. The renderer first culls the complete layer against
the camera and then submits only intersecting grid cells and tiles. It respects
the owning entity's translation, rotation, and scale.

LDtk's top-to-bottom layer definitions are mapped onto Dreambit `DrawLayer`
values so tiles in different streamed levels use a consistent global order.
Invisible LDtk layers are skipped unless `IncludeInvisibleLayers` is enabled.

## Scene lifecycle hooks

`LDtkScene` exposes these customization points in order:

1. `CreateLDtkImportOptions`
2. `OnBeforeLDtkLevelsLoaded`
3. `OnLDtkEntityInstances` for each imported level
4. `OnLDtkLevelLoaded` for each imported level
5. `OnLDtkSceneReady` after the initial set is complete

During unload it calls `OnLDtkLevelUnloading`, destroys the level-owned runtime
objects, and then calls `OnLDtkLevelUnloaded`. Scene shutdown begins with
`OnLDtkSceneEnding` and unloads every remaining level.
