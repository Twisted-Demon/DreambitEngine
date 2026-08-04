# `UiStackPanelBase`

Abstract sequential-layout base with spacing, padding, group placement, and cross-axis alignment.

**Status:** Abstract base type  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiStackPanel.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiStackPanelBase`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `CrossAlignment` | `StackCrossAlignment` field | Start, Center, or End on the non-stacking axis. |
| `GrowDirection` | `StackGrowDirection` field | Start, Center, or End placement of the complete group. |
| `Spacing` | `int` field | Gap between visible children. |
| `PaddingLeft/Top/Right/Bottom` | `int` fields | Inner padding. |

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
| `padding` | `UiThickness` | `0` | Inner inset. |
| `spacing` | `int` | `0` | Gap between visible children. |
| `cross-alignment` | `Start|Center|End` | `Start` | Cross-axis child alignment. |
| `grow-direction` | `Start|Center|End` plus axis aliases | `Start` | Group placement along stack axis. |

## C# example

```csharp
public sealed class CustomVerticalList : UiStackPanelBase
{
    protected override StackOrientation LayoutOrientation =>
        StackOrientation.Vertical;
}
```

## Layout behavior

- Only visible children contribute to desired size and spacing.
- Arrangement rewrites child position, anchor, and origin according to stack policy.
- Automatic panel size is the sum on the main axis and maximum child size on the cross axis, plus padding.

## Extending the type

- Override `LayoutOrientation` only; the base supplies measurement and arrangement.

## Production pitfalls

- Authored child `X`, `Y`, `Anchor`, and `Origin` are not preserved.
- `GrowDirection.End` aligns the whole group to the end; it does not reverse child order.
- Use the concrete vertical/horizontal classes in XML.

## See also

- [`UiVerticalStackPanel`](UiVerticalStackPanel.md)
- [`UiHorizontalStackPanel`](UiHorizontalStackPanel.md)
- [`UiStackPanel`](UiStackPanel.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
