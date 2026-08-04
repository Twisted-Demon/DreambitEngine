# `UiScrollBar`

Slider whose thumb length represents a viewport within a larger extent.

**Status:** XML tag: `ScrollBar`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiScrollBar.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiContentControl` → `UiControl` → `UiRangeBase` → `UiSlider` → `UiScrollBar`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `ViewportSize` | `float` | Visible extent represented by the thumb. |
| `LargeChange` | `float` | Wheel/page increment; default 10. |
| `MinimumThumbSize` | `int` | Minimum thumb length; default 10. |

## Common inherited XML attributes

| XML attribute | Type | XML default | Meaning |
|---|---|---|---|
| `id` | `string` | empty | Case-sensitive lookup ID used by `UiLayout.Find` and `GetRequired<T>`. |
| `x` | `UiLength` | `0%` | Horizontal offset resolved against the parent width. |
| `y` | `UiLength` | `0%` | Vertical offset resolved against the parent height. |
| `width` | `UiLength` | `100%` | Fixed pixels, percentage, or `*` for automatic desired width. |
| `height` | `UiLength` | `100%` | Fixed pixels, percentage, or `*` for automatic desired height. |
| `anchor` | `UiAnchor` | `TopLeft` | Reference point on the parent. |
| `origin` | `UiAnchor` | `TopLeft` | Point on this element placed at the anchored position. |
| `z` | `int` | `0` | Sibling draw order. Higher values draw and hit-test above lower values. |
| `grid-row` | `int` | `0` | Zero-based row used by a `UiGrid` parent. |
| `grid-column` | `int` | `0` | Zero-based column used by a `UiGrid` parent. |
| `grid-row-span` | `int` | `1` | Grid row span, clamped to at least one. |
| `grid-column-span` | `int` | `1` | Grid column span, clamped to at least one. |
| `is-visible` | `bool` | `true` | Removes the element subtree from layout, drawing, and input when false. |
| `is-enabled` | `bool` | `true` | Disables input for the element subtree when false. |
| `is-hit-test-visible` | `bool` | type default | Controls whether this element can be the direct pointer target. |
| `is-focusable` | `bool` | type default | Controls keyboard/controller focus eligibility. |
| `captures-keyboard-input` | `bool` | type default | Consumes keyboard availability while focused. |
| `clip-to-bounds` | `bool` | `false` | Clips descendants to this element's bounds using the UI scissor stack. |

## Type-specific XML

| Attribute | Type | Default | Meaning |
|---|---|---|---|
| `viewport-size` | `float` | `0` | Visible extent. |
| `large-change` | `float` | `10` | Wheel increment. |
| `minimum-thumb-size` | `int` | `10` | Clamped to at least one. |

## XML example

```xml
<ScrollBar id="inventory-scroll"
           width="18" height="260"
           orientation="Vertical"
           minimum="0" maximum="740" value="0"
           viewport-size="260"
           large-change="52"
           minimum-thumb-size="16" />
```

## C# example

```csharp
UiScrollBar scroll = layout.GetRequired<UiScrollBar>("inventory-scroll");
scroll.ValueChanged += (_, offset) =>
    inventoryContent.Y = UiLength.Pixels(-(int)offset);
```

## Input and focus

- Pointer behavior is inherited from slider. Wheel direction changes value by one `LargeChange` regardless of raw wheel magnitude.

## Runtime behavior

- Thumb ratio is `ViewportSize / (Maximum - Minimum + ViewportSize)`.
- When viewport or extent is non-positive, thumb sizing falls back to slider `ThumbSize`.

## Production pitfalls

- This control is not automatically connected to a scrollable content host. Game code must synchronize value, viewport, extent, and content offset.
- Set orientation and range consistently with the direction your content moves.

## See also

- [`UiSlider`](UiSlider.md)
- [`UiListBox`](UiListBox.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
