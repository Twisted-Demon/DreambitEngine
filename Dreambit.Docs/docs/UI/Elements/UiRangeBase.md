# `UiRangeBase`

Abstract numeric range control with clamping, normalization, stepping, and change events.

**Status:** Abstract base type  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiRangeBase.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiContentControl` → `UiControl` → `UiRangeBase`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `Minimum` | `float` | Inclusive lower bound; default 0. |
| `Maximum` | `float` | Inclusive upper bound; default 100 and never below minimum. |
| `Value` | `float` | Current clamped value. |
| `Step` | `float` | Interaction increment; default 1 and clamped non-negative. |
| `NormalizedValue` | `float` (read-only) | Value mapped to 0..1. |

### Events

| Event | Type | Behavior |
|---|---|---|
| `ValueChanged` | `Action<UiRangeBase, float>` | Raised when the effective clamped value changes. |

### Methods

| Member | Behavior |
|---|---|
| `SetNormalizedValue(float)` | Maps 0..1 into the range and snaps to `Step` when step is positive. |

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
| `minimum` | `float` | `0` | Lower bound. |
| `maximum` | `float` | `100` | Upper bound. |
| `step` | `float` | `1` | Interaction step. |
| `value` | `float` | `minimum` | Initial value. |

## C# example

```csharp
range.Minimum = 0f;
range.Maximum = 100f;
range.Step = 5f;
range.ValueChanged += (_, value) => ApplyValue(value);
```

## Runtime behavior

- Raising minimum can raise maximum and clamp value.
- Lowering maximum clamps value.
- Direct assignment to `Value` clamps but does not snap to `Step`; normalized interaction does.

## Extending the type

- Use `NormalizedValue` for rendering and `SetNormalizedValue` for pointer-position mapping.

## Production pitfalls

- `ValueChanged` can fire while changing bounds because the effective value is reclamped.

## See also

- [`UiSlider`](UiSlider.md)
- [`UiScrollBar`](UiScrollBar.md)
- [`UiProgressBar`](UiProgressBar.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
