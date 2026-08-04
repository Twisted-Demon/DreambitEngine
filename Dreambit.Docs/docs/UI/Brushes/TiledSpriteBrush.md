# `TiledSpriteBrush`

Repeats a sprite at native pixel size and crops the final row/column.

**Status:** XML brush element: `<TiledSpriteBrush>`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Brushes/TiledSpriteBrush.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`IUiBrush` → `UiBrush` → `TiledSpriteBrush`

## Declared API

| Member | Type | Behavior |
|---|---|---|
| `SpritePath` | `string` | Tile sprite asset. |

## XML attributes

| Attribute | Type | Default | Meaning |
|---|---|---|---|
| `sprite` | `string` | required | Non-empty tile path. |

## XML example

```xml
<Border.Background>
    <TiledSpriteBrush sprite="Ui/noise-tile.sprite" />
</Border.Background>
```

## C# example

```csharp
panel.Background = new TiledSpriteBrush
{
    SpritePath = "Ui/noise-tile.sprite"
};
```

## Rendering and lifecycle behavior

- `MinimumSize` is one tile's source size.
- Tiles are not scaled; partial edge tiles use cropped source rectangles.

## Production pitfalls

- Draw calls scale with tile count: approximately `ceil(width/tileWidth) × ceil(height/tileHeight)`.
- Use a reasonably sized tile and avoid full-screen tiny-pixel tiling unless profiling confirms the cost is acceptable.

## See also

- [`SpriteBrush`](./SpriteBrush.md)
- [`NineSliceBrush`](./NineSliceBrush.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
