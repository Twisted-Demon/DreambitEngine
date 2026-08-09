# Loading worlds and levels

Load a baked LDtk project through Dreambit resources:

```csharp
using Dreambit.LDtk;

var project = Resources.LoadAsset<LDtkFile>("ldtk/dreambit");
var world = project.LoadWorld();
var level = world.LoadLevel("Level_0");
```

`LoadWorld()` succeeds for a single-world project. For a project containing
multiple worlds, it throws `LdtkWorldSelectionRequiredException`; use
`AvailableWorlds` to present a choice and then select by identifier or IID:

```csharp
foreach (var available in project.AvailableWorlds)
    Console.WriteLine($"{available.Identifier}: {available.Iid}");

var world = project.LoadWorld(selectedWorldIid);
var level = world.LoadLevel(selectedLevelIid);
```

External `.ldtkl` files are loaded lazily and cached when `LoadLevel` is called.
For tools and tests, load an unbaked project directly:

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
