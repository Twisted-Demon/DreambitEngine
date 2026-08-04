# `OutlineBrush`

Draws an inset rectangular outline using the owner tint.

**Status:** XML brush element: `<OutlineBrush>`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Brushes/OutlineBrush.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`IUiBrush` → `UiBrush` → `OutlineBrush`

## Declared API

| Member | Type | Behavior |
|---|---|---|
| `Thickness` | `UiThickness` | Per-edge widths; defaults to one. |

## XML attributes

| Attribute | Type | Default | Meaning |
|---|---|---|---|
| `thickness` | `UiThickness` | `1` | Base edge thickness. |
| `left`, `top`, `right`, `bottom` | `int` | base edge | Per-edge overrides. |

## XML example

```xml
<TextBox.Background>
    <OutlineBrush thickness="2"
                  bottom="3" />
</TextBox.Background>
```

## C# example

```csharp
textBox.Background = new OutlineBrush
{
    Thickness = new UiThickness(2, 2, 2, 3)
};
```

## Rendering and lifecycle behavior

- `MinimumSize` is horizontal and vertical thickness totals.
- Draws up to four filled rectangles.
- When opposing edges exceed available size, they shrink proportionally.

## Production pitfalls

- The outline is inset, so it consumes interior pixels rather than expanding outside bounds.
- One `Color` tint applies to all edges.

## See also

- [`SolidColorBrush`](./SolidColorBrush.md)
- [`LayeredBrush`](./LayeredBrush.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
