# `UiGrid`

Track-based layout with fixed, percentage, automatic, and weighted rows and columns.

**Status:** XML tag: `Grid`  
**Namespace:** `Dreambit.UI`  
**Source:** `DreambitEngine/UI/Elements/UiGrid.cs`  
**Validated against:** DreambitEngine `main` / `ef6e5b9c600ad6e215c53ea287a0c7858884ce00`

## Inheritance

`UiElement` → `UiContainer` → `UiGrid`

## Declared API

### Properties and fields

| Member | Type | Behavior |
|---|---|---|
| `RowDefinitions` | `IList<UiGridLength>` | Top-to-bottom tracks; starts with one star track. |
| `ColumnDefinitions` | `IList<UiGridLength>` | Left-to-right tracks; starts with one star track. |
| `Padding` | `UiThickness` | Inset around tracks. |

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
| `rows` / `row-definitions` | track list | `*` | Comma-separated row definitions. |
| `columns` / `column-definitions` | track list | `*` | Comma-separated column definitions. |
| `padding` | `UiThickness` | `0` | Inset around all tracks. |

## XML example

```xml
<Grid width="100%" height="100%"
      rows="Auto,*,64"
      columns="240,2*,1*"
      padding="16">
    <Text grid-row="0" grid-column="0" grid-column-span="3"
          width="100%" height="*"
          text="Tower Loadout" font="monogram" font-size="30" />

    <ListBox grid-row="1" grid-column="0"
             width="100%" height="100%" />

    <Panel grid-row="1" grid-column="1" grid-column-span="2"
           width="100%" height="100%" />

    <Button grid-row="2" grid-column="2"
            width="100%" height="48" />
</Grid>
```

## C# example

```csharp
var grid = new UiGrid
{
    Width = UiLength.Percent(1f),
    Height = UiLength.Percent(1f),
    Padding = UiThickness.Uniform(16)
};
grid.RowDefinitions.Clear();
grid.RowDefinitions.Add(UiGridLength.Auto());
grid.RowDefinitions.Add(UiGridLength.Star());
grid.InvalidateLayout();
```

## Layout behavior

- Fixed pixels and percentages are initialized first.
- Auto tracks grow from visible child desired sizes, including spans.
- Star tracks divide remaining space by weight after fixed/percentage/auto use.
- Children arrange in their resolved cell but retain their own anchor/origin/size within that cell.

## Production pitfalls

- `*` means automatic size for element dimensions but star weight inside grid track definitions. Context matters.
- Empty track entries throw `XmlException`.
- Track definitions changed programmatically through the exposed lists do not automatically call `InvalidateLayout()`; invalidate the grid after mutation.
- Complex auto/span layouts perform multiple child measurement passes. Keep grids understandable before trying to make them clever enough to file taxes.

## See also

- [`UiUniformGrid`](UiUniformGrid.md)
- [`UiCanvas`](UiCanvas.md)

---

_Source reviewed 2026-08-03. This page documents current implemented behavior, not a proposed API._
