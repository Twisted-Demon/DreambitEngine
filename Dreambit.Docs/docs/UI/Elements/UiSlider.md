# `UiSlider`

Focusable range control adjusted through pointer dragging or directional navigation.

**Status:** XML tag: `Slider`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiSlider.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiContentControl` → `UiControl` → `UiRangeBase` → `UiSlider`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `Orientation` | `StackOrientation` | Horizontal by default. |
| `TrackBrush`, `FillBrush`, `ThumbBrush` | `IUiBrush` | Composable visuals; each defaults to solid color. |
| `TrackThickness` | `int` | Default 4. |
| `ThumbSize` | `int` | Main-axis thumb length; default 14. |
| `TrackTint`, `FillTint`, `ThumbTint` | `Color` | Default visual tints. |

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
| `orientation` | `Horizontal|Vertical` | `Horizontal` | Slider axis. |
| `track-thickness` | `int` | `4` | Clamped to at least one. |
| `thumb-size` | `int` | `14` | Clamped to at least one. |
| `track-tint` | `Color` | `70,70,78` | Unfilled track. |
| `fill-tint` | `Color` | `78,150,235` | Filled range. |
| `thumb-tint` | `Color` | white | Thumb. |

## XML example

```xml
<Slider id="master-volume"
        width="280" height="28"
        minimum="0" maximum="100" value="75" step="1"
        orientation="Horizontal"
        track-thickness="4"
        thumb-size="14"
        track-tint="#343B46"
        fill-tint="#4F9DED"
        thumb-tint="#FFFFFF">
    <Slider.TrackBrush><SolidColorBrush /></Slider.TrackBrush>
    <Slider.FillBrush><SolidColorBrush /></Slider.FillBrush>
    <Slider.ThumbBrush><NineSliceBrush sprite="Ui/thumb.sprite" slice="3" /></Slider.ThumbBrush>
</Slider>
```

## C# example

```csharp
UiSlider volume = layout.GetRequired<UiSlider>("master-volume");
volume.ValueChanged += (_, value) =>
    Audio.MasterVolume = value / 100f;
```

## Input and focus

- Pointer press immediately maps position to value, captures the pointer, and enters pressed visual state.
- Horizontal Left/Right subtract/add `Step`.
- Vertical Up increases and Down decreases; minimum is at the bottom.

## Production pitfalls

- Track/fill/thumb brush property elements are separate from the inherited control background.
- A thumb larger than the control is clamped to the control's main-axis length.
- Use an appropriate `Step`; zero disables navigation movement even though pointer mapping remains continuous.

## See also

- [`UiRangeBase`](UiRangeBase.md)
- [`UiScrollBar`](UiScrollBar.md)
- [`UiProgressBar`](UiProgressBar.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
