# `UiWrapPanel`

Sequential panel that wraps into a new row or column when space is exhausted.

**Status:** XML tag: `WrapPanel`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiWrapPanel.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiWrapPanel`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `Orientation` | `StackOrientation` | Main flow axis; default horizontal. |
| `Spacing` | `int` | Gap within each line. |
| `LineSpacing` | `int` | Gap between wrapped lines. |
| `CrossAlignment` | `StackCrossAlignment` | Item alignment inside each line. |
| `Padding` | `UiThickness` | Outer inset. |

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
| `orientation` | `Horizontal|Vertical` | `Horizontal` | Main flow axis. |
| `spacing` | `int` | `0` | Item gap. |
| `line-spacing` | `int` | `spacing` | Wrapped-line gap. |
| `cross-alignment` | `Start|Center|End` | `Start` | Within-line alignment. |
| `padding` | `UiThickness` | `0` | Outer inset. |

## XML example

```xml
<WrapPanel width="100%" height="*"
           orientation="Horizontal"
           spacing="8"
           line-spacing="10"
           cross-alignment="Center"
           padding="8">
    <Button width="120" height="44" />
    <Button width="180" height="44" />
    <Button width="140" height="44" />
</WrapPanel>
```

## C# example

```csharp
var wrap = new UiWrapPanel
{
    Orientation = StackOrientation.Horizontal,
    Spacing = 8,
    LineSpacing = 10,
    Padding = UiThickness.Uniform(8)
};
```

## Layout behavior

- Children are measured against the available inner size.
- A line wraps before the next item when the required main-axis length exceeds available space.
- Arrangement rewrites child position, anchor, and origin.

## Performance notes

- Layout builds line/item objects during measurement and arrangement; avoid using it for thousands of frequently changing items.

## Production pitfalls

- One child larger than available space remains on a line and can overflow.
- Wrapping depends on the measured available width/height; auto-size inside unconstrained parents can produce unexpected single-line layouts.
- No scrolling or virtualization is built in.

## See also

- [`UiUniformGrid`](UiUniformGrid.md)
- [`UiStackPanelBase`](UiStackPanelBase.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
