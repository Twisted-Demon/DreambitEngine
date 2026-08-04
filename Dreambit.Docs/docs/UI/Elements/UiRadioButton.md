# `UiRadioButton`

Mutually exclusive check-box-style button within an exact named group.

**Status:** XML tag: `RadioButton`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiRadioButton.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiContentControl` → `UiControl` → `UiButton` → `UiToggleButton` → `UiCheckBox` → `UiRadioButton`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `GroupName` | `string` | Exact ordinal group key; default empty string. |

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
| `group` | `string` | empty | Buttons with the same exact value are exclusive. |

## XML example

```xml
<VerticalStackPanel width="240" height="*" spacing="6">
    <RadioButton id="quality-low" group="quality">
        <Text text="Low" font="monogram" font-size="18" />
    </RadioButton>
    <RadioButton id="quality-high" group="quality" is-checked="true">
        <Text text="High" font="monogram" font-size="18" />
    </RadioButton>
</VerticalStackPanel>
```

## C# example

```csharp
UiRadioButton high = layout.GetRequired<UiRadioButton>("quality-high");
high.CheckedChanged += (_, isChecked) =>
{
    if (isChecked)
        graphicsSettings.Quality = GraphicsQuality.High;
};
```

## Input and focus

- Click always checks the radio button; clicking an already checked radio does not uncheck it.

## Runtime behavior

- Setting `IsChecked = true` programmatically unchecks peers before changing this button.
- Peer discovery traverses `Layout.Root` and compares group names with `StringComparison.Ordinal`.

## Production pitfalls

- Every radio with the default empty group belongs to the same root-level group. Assign explicit group names.
- Group names are case-sensitive.
- Exclusivity is implemented by traversing the root tree, so very large radio-heavy layouts should avoid unnecessary programmatic rechecks.

## See also

- [`UiCheckBox`](UiCheckBox.md)
- [`UiSelector`](UiSelector.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
