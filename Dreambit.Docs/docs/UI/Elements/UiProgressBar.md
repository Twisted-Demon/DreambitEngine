# `UiProgressBar`

Non-interactive range visualization with track and filled-region brushes.

**Status:** XML tag: `ProgressBar`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiProgressBar.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiContentControl` → `UiControl` → `UiRangeBase` → `UiProgressBar`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `Orientation` | `StackOrientation` | Horizontal by default. |
| `TrackBrush`, `FillBrush` | `IUiBrush` | Track and fill visuals; default solid. |
| `TrackTint` | `Color` | Default `55,55,62`. |
| `FillTint` | `Color` | Default `80,190,120`. |

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
| `orientation` | `Horizontal|Vertical` | `Horizontal` | Fill axis. |
| `track-tint` | `Color` | `55,55,62` | Track tint. |
| `fill-tint` | `Color` | `80,190,120` | Fill tint. |

## XML example

```xml
<ProgressBar id="planet-health"
             width="320" height="24"
             minimum="0" maximum="100" value="100"
             orientation="Horizontal"
             track-tint="#3A2026"
             fill-tint="#55D17A">
    <ProgressBar.TrackBrush><SolidColorBrush /></ProgressBar.TrackBrush>
    <ProgressBar.FillBrush><NineSliceBrush sprite="Ui/health-fill.sprite" slice="4" /></ProgressBar.FillBrush>
</ProgressBar>
```

## C# example

```csharp
UiProgressBar health = layout.GetRequired<UiProgressBar>("planet-health");
health.Value = planet.CurrentHealth;
health.Maximum = planet.MaximumHealth;
```

## Layout behavior

- Horizontal fills left-to-right; vertical fills bottom-to-top.

## Input and focus

- Hit testing and focus are disabled by the constructor.

## Production pitfalls

- The inherited `UiControl.Background` draws first, followed by track and fill. Usually omit the control background unless a third visual layer is intended.
- `ValueChanged` still fires for programmatic changes even though the control is non-interactive.

## See also

- [`UiRangeBase`](UiRangeBase.md)
- [`UiSlider`](UiSlider.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
