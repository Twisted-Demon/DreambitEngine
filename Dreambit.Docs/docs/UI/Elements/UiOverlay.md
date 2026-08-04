# `UiOverlay`

Full-surface content control for dimmers and modal input barriers.

**Status:** XML tag: `Overlay`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiOverlay.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiContentControl` → `UiOverlay`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `BlocksInput` | `bool` | Maps to hit testing and keyboard capture; default true. |

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
| `blocks-input` | `bool` | `true` | Whether lower UI/gameplay input is blocked. |

## XML example

```xml
<Overlay id="pause-overlay"
         width="100%" height="100%"
         is-visible="false"
         blocks-input="true"
         background-tint="#000000A0">
    <Overlay.Background><SolidColorBrush /></Overlay.Background>

    <Border width="420" height="260"
            content-alignment="Center"
            background-tint="#1B2330">
        <Border.Background><NineSliceBrush sprite="Ui/window.sprite" slice="10" /></Border.Background>
        <VerticalStackPanel width="320" height="*" spacing="12">
            <Text text="Paused" font="monogram" font-size="32" />
            <Button id="resume-button" width="100%" height="44" />
        </VerticalStackPanel>
    </Border>
</Overlay>
```

## C# example

```csharp
UiOverlay pause = layout.GetRequired<UiOverlay>("pause-overlay");
UiButton resume = layout.GetRequired<UiButton>("resume-button");

pause.IsVisible = true;
resume.Focus();
```

## Input and focus

- When blocking, pointer presses and key presses routed to the overlay are handled.
- Focusable and keyboard-capturing by default.
- Layout focus logic keeps focus inside a visible blocking overlay.

## Production pitfalls

- A C#-constructed overlay is not automatically full-screen because programmatic width/height begin at zero; set both to 100%. XML defaults already do this.
- After showing a modal overlay, focus an intended child control so controller/keyboard navigation starts in the right place.
- `BlocksInput = false` makes it a visual overlay, but descendants can still be independently interactive.

## See also

- [`UiPopup`](UiPopup.md)
- [`UiContentControl`](UiContentControl.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
