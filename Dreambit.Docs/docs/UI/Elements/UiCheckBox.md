# `UiCheckBox`

Toggle button with a brush-composed square indicator and checked mark.

**Status:** XML tag: `CheckBox`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiCheckBox.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiContentControl` → `UiControl` → `UiButton` → `UiToggleButton` → `UiCheckBox`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `IndicatorBrush` | `IUiBrush` | Brush for the outer indicator; defaults to `SolidColorBrush`. |
| `MarkBrush` | `IUiBrush` | Brush for the checked mark; defaults to `SolidColorBrush`. |
| `IndicatorSize` | `int` | Square size; default 18. |
| `IndicatorSpacing` | `int` | Gap before content; default 6. |
| `IndicatorTint` | `Color` | Outer indicator tint; default gray. |
| `MarkTint` | `Color` | Checked mark tint; default white. |

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
| `indicator-size` | `int` | `18` | Clamped to at least one. |
| `indicator-spacing` | `int` | `6` | Clamped to non-negative. |
| `indicator-tint` | `Color` | gray | Indicator color. |
| `mark-tint` | `Color` | white | Checked mark color. |

## XML example

```xml
<CheckBox id="music-enabled"
          width="260" height="36"
          is-checked="true"
          indicator-size="18"
          indicator-spacing="8"
          indicator-tint="#4E5968"
          mark-tint="#7CFFB2">
    <CheckBox.IndicatorBrush>
        <OutlineBrush thickness="2" />
    </CheckBox.IndicatorBrush>
    <CheckBox.MarkBrush>
        <SolidColorBrush />
    </CheckBox.MarkBrush>
    <Text width="100%" height="*"
          horizontal-alignment="Left"
          text="Music" font="monogram" font-size="20" />
</CheckBox>
```

## C# example

```csharp
UiCheckBox music = layout.GetRequired<UiCheckBox>("music-enabled");
music.CheckedChanged += (_, enabled) =>
    audioSettings.MusicEnabled = enabled;
```

## Layout behavior

- Natural width reserves indicator size plus spacing in addition to normal padded content.
- Content is arranged to the right of the indicator.

## Production pitfalls

- The indicator is drawn after the inherited background/content draw, so design custom content with the reserved left-side space in mind.
- Brush properties require property-element syntax; tint attributes alone do not replace the brush type.

## See also

- [`UiToggleButton`](UiToggleButton.md)
- [`UiRadioButton`](UiRadioButton.md)
- [`OutlineBrush`](../Brushes/OutlineBrush.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
