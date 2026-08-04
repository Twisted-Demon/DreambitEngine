# `UiBorder`

Non-specialized single-content panel with padding and a brush background.

**Status:** XML tag: `Border`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiBorder.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiContentControl` → `UiBorder`

## When to use

Use for panels, cards, frames, dimmers, and decorative containers that do not need button-like interaction states.

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
<Border width="420" height="*"
        padding="20"
        background-tint="#161C26"
        is-hit-test-visible="false">
    <Border.Background>
        <NineSliceBrush sprite="Ui/window.sprite" slice="10" />
    </Border.Background>
    <VerticalStackPanel width="100%" height="*" spacing="8">
        <Text text="Loadout" font="monogram" font-size="28" />
        <Text text="Choose a defense." font="monogram" font-size="18" />
    </VerticalStackPanel>
</Border>
```

## C# example

```csharp
var card = new UiBorder
{
    Width = UiLength.Pixels(420),
    Height = UiLength.Auto(),
    Padding = UiThickness.Uniform(20),
    Background = new NineSliceBrush
    {
        SpritePath = "Ui/window.sprite",
        SliceThickness = UiThickness.Uniform(10)
    },
    IsHitTestVisible = false
};
```

## Layout behavior

- All sizing and arrangement behavior comes from `UiContentControl`.

## Input and focus

- The inherited constructor makes it hit-test visible. Set `is-hit-test-visible="false"` for purely decorative borders that should not become pointer targets.

## Production pitfalls

- A decorative full-screen border can unintentionally intercept pointer hit testing unless disabled.

## See also

- [`UiContentControl`](UiContentControl.md)
- [`UiPanel`](UiPanel.md)
- [`NineSliceBrush`](../Brushes/NineSliceBrush.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
