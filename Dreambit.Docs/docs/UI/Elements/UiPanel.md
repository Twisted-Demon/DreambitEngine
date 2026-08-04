# `UiPanel`

General-purpose multi-child panel that preserves authored child geometry.

**Status:** XML tag: `Panel`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiPanel.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiPanel`

## When to use

Use as a neutral composition layer when children should retain their own positions, anchors, origins, sizes, and z-order.

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

## XML example

```xml
<Panel width="100%" height="100%">
    <Border x="32" y="32" width="260" height="120" z="0" />
    <Text x="48" y="48" width="220" height="*" z="1"
          text="Layered content" font="monogram" />
</Panel>
```

## C# example

```csharp
var panel = new UiPanel
{
    Width = UiLength.Percent(1f),
    Height = UiLength.Percent(1f)
};
panel.AddChild(statusBorder);
panel.AddChild(statusText);
```

## Layout behavior

- It uses `UiContainer` measurement and arrangement unchanged.
- Children can overlap; `ZIndex` determines draw and hit-test precedence.

## Production pitfalls

- `UiPanel` and `UiCanvas` currently behave the same. Prefer `Canvas` when the intent is explicit absolute positioning and `Panel` for neutral grouping.
- Automatic panel size depends on child offsets and desired sizes; negative/anchored extents are not a general bounding-box calculation.

## See also

- [`UiContainer`](UiContainer.md)
- [`UiCanvas`](UiCanvas.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
