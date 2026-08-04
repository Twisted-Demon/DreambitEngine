# `UiStackPanel`

Legacy/configurable stack panel whose orientation is selected by a property.

**Status:** XML tag: `StackPanel`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiStackPanel.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiStackPanelBase` → `UiStackPanel`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `Orientation` | `StackOrientation` field | Vertical by default. |

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
| `orientation` | `Vertical|Horizontal` | `Vertical` | Stack axis. |

## XML example

```xml
<StackPanel orientation="Horizontal"
            width="100%" height="48"
            spacing="8">
    <Button width="120" height="40" />
    <Button width="120" height="40" />
</StackPanel>
```

## C# example

```csharp
var stack = new UiStackPanel
{
    Orientation = StackOrientation.Horizontal,
    Spacing = 8
};
```

## Production pitfalls

- New XML should prefer `VerticalStackPanel` or `HorizontalStackPanel`; the element name then communicates layout intent.
- Retained for layouts that need orientation selected at runtime or backwards compatibility.

## See also

- [`UiVerticalStackPanel`](UiVerticalStackPanel.md)
- [`UiHorizontalStackPanel`](UiHorizontalStackPanel.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
