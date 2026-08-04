# `UiText`

FontStash-backed text element with optional wrapping and automatic height.

**Status:** XML tag: `Text`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiText.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiText`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `Font` | `SpriteFontBase` (read-only) | Resolved font instance. |
| `Text` | `string` | Displayed text; null assignments become empty. |
| `FontPath` | `string` | Font resource path; default `monogram`. |
| `FontSize` | `float` | Requested pixel size; default 12. |
| `TextColor` | `Color` | Draw color; default white. |
| `HorizontalAlignment` | `HorizontalAlignment` | Per-line alignment; default center. |
| `MultiLine` | `bool` | Whether wrapping is allowed; default true. |
| `AutoResizeHeight` | `bool` | Whether XML parsing forces automatic height; default true. |

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
| `text` | `string` | empty | Displayed text. |
| `font` | `string` | `monogram` | Font resource path. |
| `font-size` | `float` | `12` | Font size. |
| `text-color` | `Color` | white | Text color. |
| `horizontal-alignment` | `Left|Center|Right` | `Center` | Line alignment. |
| `multi-line` | `bool` | `true` | Enables wrapping. |
| `auto-resize-height` | `bool` | `true` | Forces `Height = Auto` after common XML parsing. |

## XML example

```xml
<Text id="description"
      width="360" height="*"
      text="Build defenses around the planet and survive."
      font="monogram"
      font-size="20"
      text-color="#E9F0FF"
      horizontal-alignment="Left"
      multi-line="true"
      auto-resize-height="true" />
```

## C# example

```csharp
UiText waveText = layout.GetRequired<UiText>("wave-text");

public void SetWave(int wave)
{
    waveText.Text = $"Wave {wave}";
}
```

## Layout behavior

- Constructor height is automatic.
- Single-line desired size uses measured text width and line height.
- Multiline layout wraps against the available or arranged width and vertically centers the complete line block.

## Performance notes

- Wrapped text caches line layout by width and invalidates only when relevant values change.
- Frequently changing long wrapped strings still performs measurement and line splitting; update only when the displayed value changes.

## Production pitfalls

- With the default `auto-resize-height="true"`, an explicit XML `height` is overwritten by automatic height. Set it false when fixed-height clipping/centering is intentional.
- Changing text, font, size, or wrapping invalidates layout; changing font or size also invalidates dependencies.
- This is text layout, not rich text: no spans, markup, or inline images.

## See also

- [`UiTextBox`](UiTextBox.md)
- [`UiContentControl`](UiContentControl.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
