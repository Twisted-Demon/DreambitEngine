# `UiButton`

Focusable single-content control that raises a click after a complete activation gesture.

**Status:** XML tag: `Button`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiButton.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiContentControl` → `UiControl` → `UiButton`

## When to use

Use for actions that should activate through pointer, keyboard, or controller input.

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `IsHovered` | `bool` (read-only) | Whether the pointer route currently includes the button. |
| `IsPressed` | `bool` (read-only) | Whether a press that began inside is still held. |

### Events

| Event | Type | Behavior |
|---|---|---|
| `Clicked` | `Action<UiButton>` | Raised on valid pointer release or activation command. |

### Methods

| Member | Behavior |
|---|---|
| `OnClick()` | `protected virtual` | Override point that raises `Clicked`. |

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
<Button id="launch-button"
        width="240" height="48"
        background-tint="#31506C"
        hover-tint="#426E96"
        pressed-tint="#243C52"
        focused-tint="#4A7DAA">
    <Button.Background>
        <NineSliceBrush sprite="Ui/button.sprite" slice="6" />
    </Button.Background>
    <Text text="Launch" font="monogram" font-size="22" />
</Button>
```

## C# example

```csharp
UiButton launch = layout.GetRequired<UiButton>("launch-button");
launch.Clicked += OnLaunchClicked;

private void OnLaunchClicked(UiButton button)
{
    button.IsEnabled = false;
    StartMission();
}
```

## Input and focus

- Pointer press inside captures the pointer and marks the event handled.
- Click fires only when the gesture started inside and releases while `IsPointerOver` is true.
- Enter/Space or controller A routes an activation command and invokes the same click path.
- Losing pointer capture cancels the pressed state.

## Production pitfalls

- `Clicked` is not raised when press begins outside and ends inside.
- Keep game action logic in the handler; do not poll `IsPressed` as a substitute for activation.
- Unsubscribe scene-owned handlers when replacing/reloading the layout or when the subscriber outlives the button.

## See also

- [`UiControl`](UiControl.md)
- [`UiToggleButton`](UiToggleButton.md)
- [`UiComboBox`](UiComboBox.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
