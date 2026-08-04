# `NineSliceBrush`

Scalable sprite brush that preserves corner/edge regions while stretching the center.

**Status:** XML brush element: `<NineSliceBrush>`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Brushes/NineSliceBrush.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`IUiBrush` → `UiBrush` → `NineSliceBrush`

## Declared API

| Member | Type | Behavior |
|---|---|---|
| `SpritePath` | `string` | Source sprite asset. |
| `SliceThickness` | `UiThickness` | Source-pixel edge insets. |

## XML attributes

| Attribute | Type | Default | Meaning |
|---|---|---|---|
| `sprite` | `string` | required | Sprite path. |
| `slice` | `UiThickness` | `0` | Base inset for all edges. |
| `slice-left`, `slice-top`, `slice-right`, `slice-bottom` | `int` | `slice` edge | Per-edge overrides. |

## XML example

```xml
<Border.Background>
    <NineSliceBrush sprite="Ui/panel.sprite"
                    slice="8"
                    slice-bottom="10" />
</Border.Background>
```

## C# example

```csharp
border.Background = new NineSliceBrush
{
    SpritePath = "Ui/panel.sprite",
    SliceThickness = new UiThickness(8, 8, 8, 10)
};
```

## Rendering and lifecycle behavior

- Draws up to nine source regions.
- `MinimumSize` is the resolved left+right and top+bottom edge total.
- When source or destination is smaller than combined edges, opposing edges are reduced proportionally.

## Production pitfalls

- Insets are source pixels, not destination percentages.
- Choose source art whose center and edges are safe to stretch.
- A nine-slice may issue nine sprite draws per control; use it where scalable framing matters, not for every invisible wrapper.

## See also

- [`SpriteBrush`](./SpriteBrush.md)
- [`TiledSpriteBrush`](./TiledSpriteBrush.md)
- [`LayeredBrush`](./LayeredBrush.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
