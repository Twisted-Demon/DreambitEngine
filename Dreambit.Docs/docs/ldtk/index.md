# LDtk integration

Dreambit loads LDtk projects without `LDtkMonoGame`. `LDtkScene` turns one LDtk
world into camera-culled Dreambit backgrounds and tilemaps. Entity instances
remain raw LDtk data until the game chooses how to map them to Dreambit
entities.

The loader supports embedded and external levels, legacy single-world projects,
multi-world projects, tileset and background paths, file-path fields, and LDtk
entity references. Paths are resolved relative to the project file without
mutating the imported JSON values.

Start with the [quick-start guide](quick-start.md) to render a project. Then see
[loading worlds and levels](loading-levels.md) for streaming and caching, or
[mapping LDtk entities](entities.md) when the game is ready to instantiate its
own components and blueprints. The
[MonoGame conversion reference](monogame-conversions.md) covers points, vectors,
colors, rectangles, tile flags, and custom fields.

## Runtime structure

`LDtkManager` owns the active raw project and caches deserialized worlds and
levels. This cache survives level unloads and scene changes. Installing a
different project clears it.

Each `LDtkScene` selects exactly one world and owns the runtime
`LDtkLevelInstance` objects created for that scene. A level instance owns its
backgrounds, tilemap renderers, and any game entities registered with
`TrackEntity`. Unloading the instance removes those runtime objects while the
raw level remains cached.

The import path is:

1. The content build bakes `.ldtk` and `.ldtkl` files to `.jsonb` and images to
   Dreambit textures.
2. `LDtkManager` loads and caches the raw `LDtkFile` model.
3. `LDtkScene` selects one world and decides which levels to materialize.
4. `LDtkLevelImporter` creates Dreambit background and tilemap components.
5. `OnLDtkEntityInstances` receives the raw entity instances and creates
   generic or Blueprint-field-driven gameplay entities by default.

`TilemapRenderer` and `TilemapLayerData` are Dreambit rendering types with no
dependency on LDtk. They can also be populated by another map format. The
renderer culls the complete layer, visible grid cells, and individual tiles
against the active camera.

## What is rendered automatically

The default importer creates:

- a root entity at each level's world position;
- the level background color;
- the optional level background image;
- one `TilemapRenderer` for every visible LDtk tile or auto layer;
- Dreambit `DrawLayer` values preserving LDtk's visual layer order.

The scene hook creates entities, but it does not infer collisions, IntGrid
gameplay data, navigation, or custom identifier mappings. Those remain
game-specific.
