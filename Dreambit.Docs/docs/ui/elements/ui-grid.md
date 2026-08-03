# UiGrid

`UiGrid` arranges children into row and column tracks.

```xml
<Grid width="100%" height="100%"
      rows="Auto,*,64" columns="180,2*,1*" padding="12">
  <Text grid-row="0" grid-column="0" grid-column-span="3"
        height="40" text="Inventory" />
  <ListBox grid-row="1" grid-column="0" width="100%" height="100%" />
  <Border grid-row="1" grid-column="1" grid-column-span="2" />
</Grid>
```

Tracks accept pixels (`80`), percentages (`25%`), `Auto`, `*`, or weighted star
values (`2*`). Auto tracks measure content; star tracks share remaining space.
Children use `grid-row`, `grid-column`, and span attributes.

In code, edit `RowDefinitions` and `ColumnDefinitions` with `UiGridLength`
values, then invalidate layout.

