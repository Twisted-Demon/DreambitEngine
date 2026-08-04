# `UiHorizontalStackPanel`

Concrete left-to-right stack panel.

**Status:** XML tag: `HorizontalStackPanel`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiStackPanel.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiStackPanelBase` → `UiHorizontalStackPanel`

## When to use

Use for toolbars, icon-label rows, resource counters, and button rows.

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
<HorizontalStackPanel width="100%" height="48"
                      spacing="8"
                      cross-alignment="Center"
                      grow-direction="Right">
    <Button width="120" height="40" />
    <Button width="120" height="40" />
</HorizontalStackPanel>
```

## C# example

```csharp
var row = new UiHorizontalStackPanel
{
    Width = UiLength.Percent(1f),
    Height = UiLength.Pixels(48),
    Spacing = 8,
    GrowDirection = StackGrowDirection.Right,
    CrossAlignment = StackCrossAlignment.Center
};
```

## Layout behavior

- Children are laid out left-to-right. Cross alignment controls vertical placement.

## See also

- [`UiStackPanelBase`](UiStackPanelBase.md)
- [`UiVerticalStackPanel`](UiVerticalStackPanel.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
