# `UiViewbox`

Single-content layout slot that fits content using configurable stretch behavior.

**Status:** XML tag: `Viewbox`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiViewbox.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiContentControl` → `UiViewbox`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `Stretch` | `UiStretch` | `Uniform` by default. |

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
| `stretch` | `None|Fill|Uniform|UniformToFill` | `Uniform` | Fit policy. |

## XML example

```xml
<Viewbox width="240" height="160"
         stretch="Uniform"
         clip-to-bounds="true">
    <Texture width="64" height="64"
             sprite="Ui/planet-preview.sprite" />
</Viewbox>
```

## C# example

```csharp
var preview = new UiViewbox
{
    Width = UiLength.Pixels(240),
    Height = UiLength.Pixels(160),
    Stretch = UiStretch.Uniform,
    ClipToBounds = true
};
preview.SetContent(texture);
```

## Layout behavior

- `None` keeps desired size; `Fill` independently fills both axes.
- `Uniform` preserves desired aspect ratio and fits inside.
- `UniformToFill` preserves aspect ratio and covers the bounds.
- Content is centered in the resulting slot.

## Production pitfalls

- This is not a general render-transform scaler. It assigns a fitted layout slot and temporarily stretches authored geometry.
- Text font size, stroke widths, and other non-layout pixel values do not automatically scale like a rendered bitmap.
- `UniformToFill` can extend the slot beyond the viewbox; set `clip-to-bounds="true"` when cropping is intended.

## See also

- [`UiContentControl`](UiContentControl.md)
- [`UiTexture`](UiTexture.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
