# `UiUniformGrid`

Equal-cell grid with optional automatic row/column calculation.

**Status:** XML tag: `UniformGrid`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiUniformGrid.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiUniformGrid`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `Rows` | `int` | Requested row count; zero calculates it. |
| `Columns` | `int` | Requested column count; zero calculates it. |
| `ColumnSpacing`, `RowSpacing` | `int` | Cell gaps. |
| `Padding` | `UiThickness` | Inset around cells. |

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
| `rows` | `int` | `0` | Zero calculates from item count. |
| `columns` | `int` | `0` | Zero calculates from item count. |
| `spacing` | `int` | `0` | Fallback for both axes. |
| `column-spacing` | `int` | `spacing` | Horizontal gap. |
| `row-spacing` | `int` | `spacing` | Vertical gap. |
| `padding` | `UiThickness` | `0` | Outer inset. |

## XML example

```xml
<UniformGrid width="100%" height="*"
             columns="4"
             rows="0"
             spacing="8"
             padding="8">
    <Button width="100%" height="64" />
    <Button width="100%" height="64" />
    <Button width="100%" height="64" />
    <Button width="100%" height="64" />
</UniformGrid>
```

## C# example

```csharp
var grid = new UiUniformGrid
{
    Columns = 4,
    ColumnSpacing = 8,
    RowSpacing = 8,
    Padding = UiThickness.Uniform(8)
};
```

## Layout behavior

- Only visible children receive cells.
- When both dimensions are zero, columns are `ceil(sqrt(count))` and rows are calculated from columns.
- Each cell is equal size; children arrange within cells using their own geometry.

## Production pitfalls

- When both `Rows` and `Columns` are explicitly set, ensure `Rows * Columns` can contain all visible children. Extra items are arranged beyond the requested row count.
- Integer division can leave a few unused pixels at the right or bottom.
- No virtualization is provided.

## See also

- [`UiGrid`](UiGrid.md)
- [`UiWrapPanel`](UiWrapPanel.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
