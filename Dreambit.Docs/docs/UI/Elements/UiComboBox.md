# `UiComboBox`

Focusable string selector that opens a generated list box on the popup layer.

**Status:** XML tag: `ComboBox`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiComboBox.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiContentControl` → `UiControl` → `UiComboBox`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `Items` | `IList<string>` | Backing item list. |
| `SelectedIndex` | `int` | Selected item or -1. |
| `SelectedItem` | `string` (read-only) | Selected string or empty. |
| `IsDropDownOpen` | `bool` (read-only) | Popup state. |
| `FontPath`, `FontSize` | `string`, `float` | Header/generated-item font. |
| `TextColor` | `Color` | Header and generated item text color. |
| `ItemHeight` | `int` | Generated row height; default 26. |
| `PopupTint` | `Color` | Generated popup/list background. |

### Events

| Event | Type | Behavior |
|---|---|---|
| `SelectionChanged` | `Action<UiComboBox, int, string>` | Raised when effective selection changes. |

### Methods

| Member | Behavior |
|---|---|
| `SetItems(IEnumerable<string>)` | Replaces choices, normalizes selection, rebuilds popup items, and invalidates layout. |
| `OpenDropDown()` | Opens if attached and non-empty. |
| `CloseDropDown()` | Closes the popup. |

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
| `items` | comma-separated string | empty | Whitespace-trimmed; empty entries removed. |
| `selected-index` | `int` | 0 when items exist | Initial selection. |
| `font` | `string` | `monogram` | Font path. |
| `font-size` | `float` | `18` | Font size. |
| `item-height` | `int` | `26` | Generated row height. |
| `text-color` | `Color` | white | Text color. |
| `popup-tint` | `Color` | `36,39,48` | Popup background. |

## XML example

```xml
<ComboBox id="resolution"
          width="240" height="38"
          items="1280x720,1920x1080,2560x1440"
          selected-index="1"
          font="monogram" font-size="19"
          item-height="30"
          background-tint="#202936"
          hover-tint="#2C3B50"
          open-tint="#344C68"
          popup-tint="#171D27">
    <ComboBox.Background><NineSliceBrush sprite="Ui/input.sprite" slice="5" /></ComboBox.Background>
</ComboBox>
```

## C# example

```csharp
UiComboBox resolutions = layout.GetRequired<UiComboBox>("resolution");
resolutions.SetItems(displayModes.Select(mode =>
    $"{mode.Width}x{mode.Height}"));
resolutions.SelectionChanged += (_, index, _) =>
    ApplyDisplayMode(index);
```

## Input and focus

- Pointer activation toggles the popup using capture-safe press/release semantics.
- Activate toggles; Cancel closes.
- Up/Down changes selection directly, whether or not the popup is open.

## Production pitfalls

- Mutating `Items` directly does not rebuild an already created popup. Prefer `SetItems`.
- `FontPath`, `FontSize`, `TextColor`, `ItemHeight`, and `PopupTint` are plain properties; changing them after popup creation does not automatically rebuild every generated item. Update them before `SetItems`, then call `SetItems` again.
- XML items cannot represent commas or intentional empty strings.
- Supplying custom `Content` suppresses built-in header text/arrow drawing but does not replace popup generation.

## See also

- [`UiPopup`](UiPopup.md)
- [`UiListBox`](UiListBox.md)
- [`UiControl`](UiControl.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
