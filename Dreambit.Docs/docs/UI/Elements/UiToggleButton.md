# `UiToggleButton`

Button that retains a checked on/off state after activation.

**Status:** XML tag: `ToggleButton`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiToggleButton.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiContentControl` → `UiControl` → `UiButton` → `UiToggleButton`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `IsChecked` | `bool` | Current toggle state; setting it raises `CheckedChanged` when effective value changes. |

### Events

| Event | Type | Behavior |
|---|---|---|
| `CheckedChanged` | `Action<UiToggleButton, bool>` | Raised for user and programmatic state changes. |

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
| `is-checked` | `bool` | `false` | Initial checked state. |

## XML example

```xml
<ToggleButton id="fast-forward"
              width="160" height="40"
              is-checked="false"
              background-tint="#303944"
              checked-tint="#2E7D52">
    <ToggleButton.Background>
        <SolidColorBrush />
    </ToggleButton.Background>
    <Text text="2× Speed" font="monogram" font-size="19" />
</ToggleButton>
```

## C# example

```csharp
UiToggleButton speed = layout.GetRequired<UiToggleButton>("fast-forward");
speed.CheckedChanged += (_, enabled) =>
    Time.TimeScale = enabled ? 2f : 1f;
```

## Input and focus

- Valid clicks toggle `IsChecked` before the inherited `Clicked` event is raised.

## Production pitfalls

- If subscribed to both `CheckedChanged` and `Clicked`, remember that `CheckedChanged` fires first.
- Programmatic assignment raises `CheckedChanged`; guard against feedback loops when syncing settings.

## See also

- [`UiButton`](UiButton.md)
- [`UiCheckBox`](UiCheckBox.md)
- [`UiRadioButton`](UiRadioButton.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
