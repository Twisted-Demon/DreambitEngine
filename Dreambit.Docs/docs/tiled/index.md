# Tiled integration

Dreambit imports Tiled maps from the XML-based `.tmx` format and external
`.tsx` tilesets. A Tiled map is treated as a tile-authoring source: tile layers
become transient Dreambit entities and `TilemapRenderer` components, while
gameplay entities remain authored in Dreambit Editor.

## Supported maps

The importer supports:

- orthogonal fixed-size and infinite maps;
- embedded and external tilesets;
- atlas tilesets and image-collection tilesets;
- XML tile elements, CSV, and Base64 layer data;
- uncompressed, gzip, and zlib Base64 data;
- Tiled horizontal, vertical, and diagonal tile transforms;
- all four orthogonal render orders;
- nested groups, offsets, visibility, opacity, tint, and map background color;
- animated tiles using each frame's Tiled duration.

Object layers and image layers are intentionally not materialized. Create
gameplay entities, triggers, colliders, and decorations in Dreambit Editor
instead. Isometric, staggered, and hexagonal maps are rejected. Non-normal
layer blend modes, embedded image data, and zstd-compressed layer data are not
currently supported.

## Content layout

Keep the map, tilesets, and referenced images beneath the game's raw `Assets`
directory. Paths remain relative to the `.tmx` or `.tsx` file that owns them.

```text
Assets/
  maps/
    world.tmx
    tiles/
      terrain.tsx
  textures/
    terrain.png
```

The Asset Baker writes `.tmx` and `.tsx` sources as `.xmlb` resources and
preserves their logical directory structure. The map above is loaded with the
extensionless logical name `"maps/world"`.

## Dreambit Editor workflow

Choose **File > New Tiled Scene** and select a `.tmx` asset. The creation popup
and linked-scene Inspector expose `TiledImportOptions`:

- `PixelsPerUnit`;
- `BaseDrawLayer` and `DrawLayerStep`;
- `WorldDepth` and `WorldDepthDrawLayerStride`;
- `RenderMapBackgroundColor`;
- `IncludeInvisibleLayers`.

Generated nodes appear with a `[Tiled]` prefix. Their hierarchy and renderer
structure remain owned by the source map, but Dreambit-side names, enabled
state, tags, transforms, and serialized component values are stored as stable
overrides and re-applied after import. Saving the `.tmx` or a referenced `.tsx`
triggers live reimport; **Reimport Tiled Now** performs the same rebuild
manually. Dreambit-authored entities remain unchanged.

## Runtime scene

Derive from `TiledScene` when a code-authored scene owns one map:

```csharp
using Dreambit.Tiled;

public sealed class OverworldScene : TiledScene
{
    public OverworldScene()
        : base("maps/world")
    {
    }

    protected override TiledImportOptions CreateTiledImportOptions() => new()
    {
        PixelsPerUnit = 16f,
        BaseDrawLayer = -100,
        DrawLayerStep = 10,
        WorldDepth = 0,
        WorldDepthDrawLayerStride = 1000
    };

    protected override void OnTiledMapLoaded(TiledMapInstance map)
    {
        if (map.TryGetTileLayer("Foreground", out var foreground))
        {
            var foregroundDrawLayer = map.GetDrawLayer(foreground);
        }
    }
}
```

`TiledMapInstance` owns the generated entities and renderers. `Unload` removes
only that imported hierarchy. `TrackEntity` can attach a runtime-created
entity to the same lifetime, and `ApplyDrawLayer` copies a Tiled layer's
resolved Dreambit draw layer to an entity hierarchy.

Editor-authored scenes store a `TiledSceneReference` in the scene blueprint.
Runtime `SceneBlueprint` loading resolves that reference, imports fresh tile
data, and applies the saved Dreambit overrides before the scene starts.
