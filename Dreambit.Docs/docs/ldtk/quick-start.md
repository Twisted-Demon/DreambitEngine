# LDtk quick start

This walkthrough renders one LDtk world with all of its levels. It intentionally
does not create gameplay entities.

## 1. Add the project to game content

Keep the LDtk project and everything it references beneath the game's content
source directory. Relative paths in the LDtk file must continue to point to the
corresponding source files.

```text
MyGame.Content/
  Assets/
    LDtk/
      Dreambit.ldtk
      Levels/
        Village.ldtkl
    World/
      Tilesets/
        terrain.png
```

The normal Dreambit content build processes the entire directory:

- `.ldtk` and `.ldtkl` become `.jsonb` resources;
- referenced images become Dreambit texture resources;
- the directory structure is retained as the logical asset name.

The project above is loaded with the extensionless, content-relative name
`"ldtk/dreambit"`. Rebuild the executable after changing the project, an
external level, or a referenced image.

## 2. Create an LDtk scene

Derive the game scene from `LDtkScene`. The first argument is the project asset
name and the second is the LDtk world identifier.

```csharp
using System.Collections.Generic;
using System.Linq;
using Dreambit;
using Dreambit.LDtk;
using Microsoft.Xna.Framework;

public sealed class OverworldScene : LDtkScene
{
    private const float LdtkPixelsPerUnit = 16f;

    public OverworldScene()
        : base("ldtk/dreambit", "Dreambit_World")
    {
    }

    protected override LDtkImportOptions CreateLDtkImportOptions() => new()
    {
        // Sixteen LDtk pixels become one Dreambit world unit.
        PixelsPerUnit = LdtkPixelsPerUnit,
        BaseDrawLayer = -100,
    };

    protected override void OnLDtkSceneReady()
    {
        // Project, World, and LoadedLevels are now ready. Because this scene
        // uses the default load mode, every level has been materialized.
        var first = LoadedLevels.Values.First();
        var levelPixelOrigin = World.GetLevelWorldPosition(first.Iid);

        MainCamera.SetTargetVerticalResolution(18f);
        MainCamera.ForcePosition(new Vector3(
            (levelPixelOrigin.X + first.Level.PxWid * 0.5f) / LdtkPixelsPerUnit,
            (levelPixelOrigin.Y + first.Level.PxHei * 0.5f) / LdtkPixelsPerUnit,
            0f));
    }

    protected override void OnLDtkEntityInstances(
        LDtkLevelInstance level,
        IReadOnlyList<EntityInstance> entityInstances)
    {
        // Deliberately empty. Tile layers and backgrounds still render.
    }
}
```

If one LDtk pixel should equal one Dreambit world unit, remove the import-options
override and do not divide the camera position by `PixelsPerUnit`.

## 3. Start the scene

Select the scene before entering the engine loop:

```csharp
using Dreambit;

using var engine = new Core(width: 1280, height: 720, title: "My Game");

Scene.SetNextScene(new OverworldScene());
engine.Run();
```

When the scene initializes, Dreambit loads the project, selects
`Dreambit_World`, imports every level, and renders its non-entity content.

## 4. Stream levels instead

Pass `LDtkLevelLoadMode.Selected` followed by the levels that should initially
exist:

```csharp
public OverworldScene()
    : base(
        "ldtk/dreambit",
        "Dreambit_World",
        LDtkLevelLoadMode.Selected,
        "Village")
{
}
```

Load and unload levels later from the scene:

```csharp
var forest = LoadLevel("Forest");

if (IsLevelLoaded("Village"))
    UnloadLevel("Village");
```

`LoadLevel` returns the existing runtime instance if the level is already
loaded. After `UnloadLevel`, a later load creates fresh rendering entities but
reuses the cached raw `LDtkLevel` model.

## Common first-run problems

!!! warning "A world must be selected"
    Omitting the world identifier is only valid when the project contains zero
    or one explicit world. With multiple worlds, select by identifier or IID.

!!! warning "A texture cannot be loaded"
    Keep referenced images under the content root, preserve their paths relative
    to the `.ldtk` file, and rebuild content. Dreambit resolves the path but the
    texture must also exist in the baked content.

!!! warning "The scene is empty"
    Position the camera in `OnLDtkSceneReady`. LDtk coordinates are expressed in
    pixels; divide camera and gameplay positions by the same `PixelsPerUnit`
    value used by the importer.
