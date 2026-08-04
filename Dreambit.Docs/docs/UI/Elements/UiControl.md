# `UiControl`

Interactive single-content base with reusable templates and combined visual states.

**Status:** XML tag: `Control`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiControl.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiContentControl` → `UiControl`

## When to use

Derive interactive controls from this class to reuse focus, background composition, templates, selected state, and state-specific tint resolution.

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `Style` | `UiControlStyle` | Stores optional state tints. |
| `Template` | `UiControlTemplate` | Function that creates arbitrary content for this control. |
| `VisualState` | `UiControlState` (read-only) | Bitwise combined state: disabled, hovered, focused, pressed, checked, selected, and open. |
| `IsSelected` | `bool` (internal setter) | Set by selectors for selected visual state. |
| `HoverTint`, `PressedTint`, `FocusedTint`, `DisabledTint` | `Color` | State tint with normal-tint fallback. |
| `CheckedTint`, `SelectedTint` | `Color` | Checked/selected state tint with fallback. |

### Methods

| Member | Behavior |
|---|---|
| `ApplyTemplate()` | Recreates content by invoking `Template`; does nothing when no template is assigned. |

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
| `hover-tint` | `Color` | normal tint | Hovered state. |
| `pressed-tint` | `Color` | normal tint | Pressed/dragging state. |
| `focused-tint` | `Color` | normal tint | Focused state. |
| `disabled-tint` | `Color` | normal tint | Disabled state. |
| `checked-tint` | `Color` | normal tint | Checked state. |
| `selected-tint` | `Color` | normal tint | Selector-selected state. |
| `open-tint` | `Color` | normal tint | Open/expanded state. |

## XML example

```xml
<Control width="280" height="52"
         background-tint="#33445A"
         hover-tint="#426181"
         focused-tint="#4D73A0"
         disabled-tint="#222831">
    <Control.Background>
        <SolidColorBrush />
    </Control.Background>
    <Text text="Custom control content" font="monogram" font-size="20" />
</Control>
```

## C# example

```csharp
var control = new UiControl
{
    Width = UiLength.Pixels(280),
    Height = UiLength.Pixels(52),
    Background = new SolidColorBrush(),
    BackgroundTint = new Color(51, 68, 90),
    HoverTint = new Color(66, 97, 129)
};

control.Template = owner => new UiText
{
    Width = UiLength.Percent(1f),
    Text = owner.IsEnabled ? "Ready" : "Disabled"
};
```

## Runtime behavior

- Setting `BackgroundTint` also updates `Style.NormalTint`.
- State flags may coexist; `UiControlStyle.Resolve` determines precedence.

## Production pitfalls

- `Template` is a C# delegate and is not a general XML style/template system.
- Applying a template replaces existing content. Any references or event subscriptions tied to the old content must be treated as stale.

## See also

- [`UiContentControl`](UiContentControl.md)
- [`UiButton`](UiButton.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
