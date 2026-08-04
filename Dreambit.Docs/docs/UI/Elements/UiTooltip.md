# `UiTooltip`

Delayed, non-interactive popup attached through an element's `Tooltip` property.

**Status:** XML tag: `Tooltip`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiTooltip.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiContentControl` → `UiControl` → `UiPopup` → `UiTooltip`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `Delay` | `float` | Hover delay in unscaled seconds; default 0.5. |

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
| `delay` | `float` | `0.5` | Clamped non-negative. |

## XML example

```xml
<Button id="upgrade-button" width="180" height="42">
    <Button.Tooltip>
        <Tooltip width="260" height="*"
                 delay="0.4"
                 background-tint="#111722">
            <Tooltip.Background>
                <NineSliceBrush sprite="Ui/tooltip.sprite" slice="6" />
            </Tooltip.Background>
            <Text width="100%" height="*"
                  text="Upgrade damage by 20%."
                  font="monogram" font-size="17"
                  horizontal-alignment="Left" />
        </Tooltip>
    </Button.Tooltip>
    <Text text="Upgrade" font="monogram" />
</Button>
```

## C# example

```csharp
button.Tooltip = new UiTooltip
{
    Delay = 0.4f,
    Background = new SolidColorBrush(),
    BackgroundTint = new Color(17, 23, 34)
};
button.Tooltip.SetContent(new UiText { Text = "Upgrade damage by 20%." });
```

## Input and focus

- Disabled and non-hit-test-visible by design.
- Uses unscaled delta time, so tooltips still appear while gameplay time is paused.
- Closes immediately when the target leaves the pointer route.

## Production pitfalls

- Author as a property element of the target rather than an unrelated top-level popup.
- Tooltip content cannot receive input.

## See also

- [`UiPopup`](UiPopup.md)
- [`UiElement`](UiElement.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
