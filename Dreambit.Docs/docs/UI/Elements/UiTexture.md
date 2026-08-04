# `UiTexture`

Sprite element stretched into its arranged rectangle.

**Status:** XML tag: `Texture`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiTexture.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiTexture`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `SpritePath` | `string` | Resource path loaded during dependency resolution. |
| `Sprite` | `Sprite` | Direct sprite override. |
| `Tint` | `Color` | Multiplicative draw tint; default white. |

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
| `sprite` | `string` | empty | Sprite asset path. |
| `tint` | `Color` | white | Multiplicative tint. |

## XML example

```xml
<Texture width="64" height="64"
         sprite="Ui/icons/laser.sprite"
         tint="#FFFFFF" />
```

## C# example

```csharp
var icon = new UiTexture
{
    Width = UiLength.Pixels(64),
    Height = UiLength.Pixels(64),
    SpritePath = "Ui/icons/laser.sprite",
    Tint = Color.White
};
```

## Layout behavior

- Automatic size is the source rectangle size after dependencies resolve.

## Production pitfalls

- The sprite is stretched to `Bounds`; use `UiViewbox` for aspect-preserving layout or a nine-slice brush for scalable frames.
- Assigning `Sprite = null` does not clear the current sprite in the present implementation. Clear `SpritePath` and invalidate dependencies when removal is required.
- Asset-backed automatic size is unavailable until dependency resolution has loaded the sprite.

## See also

- [`SpriteBrush`](../Brushes/SpriteBrush.md)
- [`UiViewbox`](UiViewbox.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
