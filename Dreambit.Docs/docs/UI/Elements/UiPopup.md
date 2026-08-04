# `UiPopup`

Topmost single-content control placed absolutely or relative to another element.

**Status:** XML tag: `Popup`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiPopup.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiContentControl` → `UiControl` → `UiPopup`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `IsOpen` | `bool` (read-only) | Current popup-layer state. |
| `StaysOpen` | `bool` | Whether outside pointer presses leave it open. |
| `PlacementTarget` | `UiElement` | Runtime placement reference. |
| `PlacementTargetId` | `string` | ID resolved when arranging/opening. |
| `Placement` | `UiPopupPlacement` | Bottom, Top, Left, Right, Center, or Absolute. |
| `HorizontalOffset`, `VerticalOffset` | `int` | Additional placement offsets. |

### Methods

| Member | Behavior |
|---|---|
| `Open()` | Moves/opens the popup on the layout popup layer; records a request if not attached yet. |
| `Close()` | Closes while retaining the popup instance for reuse. |

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
| `is-open` | `bool` | `false` | Requests opening after load. |
| `stays-open` | `bool` | `false` | Outside-click behavior. |
| `placement-target` | `string` | empty | Target element ID. |
| `placement` | `Bottom|Top|Left|Right|Center|Absolute` | `Bottom` | Placement mode. |
| `horizontal-offset` | `int` | `0` | X offset. |
| `vertical-offset` | `int` | `0` | Y offset. |

## XML example

```xml
<Button id="loadout-button" width="180" height="42" />

<Popup id="loadout-popup"
       width="260" height="*"
       placement-target="loadout-button"
       placement="Bottom"
       vertical-offset="6"
       stays-open="false"
       background-tint="#171E29">
    <Popup.Background><NineSliceBrush sprite="Ui/popup.sprite" slice="8" /></Popup.Background>
    <VerticalStackPanel width="100%" height="*" padding="8" spacing="4">
        <Button width="100%" height="36" />
        <Button width="100%" height="36" />
    </VerticalStackPanel>
</Popup>
```

## C# example

```csharp
UiPopup popup = layout.GetRequired<UiPopup>("loadout-popup");
popup.Open();
// ...
popup.Close();
```

## Input and focus

- Cancel closes a non-staying popup.
- Outside-pointer dismissal is coordinated by `UiLayout`/popup layer.

## Ownership and lifecycle

- An open popup is owned by `UiPopupLayer`, not its authored parent.
- Close retains content and state for later reuse.

## Production pitfalls

- Placement is not automatically clamped or flipped to remain inside the viewport.
- Relative placement uses current target bounds; do not open before the target has meaningful arranged geometry unless deferred opening is desired.
- Because opening reparents the popup, code that assumes its authored parent remains constant will be wrong.

## See also

- [`UiTooltip`](UiTooltip.md)
- [`UiComboBox`](UiComboBox.md)
- [`UiOverlay`](UiOverlay.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
