# `UiCanvas`

Explicitly positioned panel for freeform or overlapping UI.

**Status:** XML tag: `Canvas`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiCanvas.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiPanel` → `UiCanvas`

## When to use

Use for HUD overlays, screen-space markers, drag surfaces, and other layouts where each child owns its exact position.

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
<Canvas width="100%" height="100%">
    <Text id="wave-text"
          x="-24" y="24"
          width="240" height="*"
          anchor="TopRight" origin="TopRight"
          text="Wave 12" font="monogram" font-size="28" />
</Canvas>
```

## C# example

```csharp
var marker = new UiTexture
{
    X = UiLength.Percent(0.5f),
    Y = UiLength.Percent(0.5f),
    Width = UiLength.Pixels(24),
    Height = UiLength.Pixels(24),
    Anchor = UiAnchor.TopLeft,
    Origin = UiAnchor.Center,
    SpritePath = "Ui/target-marker.sprite"
};
canvas.AddChild(marker);
```

## Layout behavior

- Children retain `X`, `Y`, `Width`, `Height`, `Anchor`, `Origin`, and `ZIndex`.
- No flow, wrapping, or cell placement is applied.

## Production pitfalls

- Absolute positioning is easy to author but harder to adapt across aspect ratios. Combine percentage anchors with fixed offsets where possible.
- Use stack/grid containers for menus and forms; using a canvas for everything becomes coordinate spaghetti with better branding.

## See also

- [`UiPanel`](UiPanel.md)
- [`UiGrid`](UiGrid.md)
- [`UiStackPanel`](UiStackPanel.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
