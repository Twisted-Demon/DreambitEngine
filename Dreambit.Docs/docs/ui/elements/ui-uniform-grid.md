# UiUniformGrid

`UiUniformGrid` gives every child an equal-size cell.

```xml
<UniformGrid width="360" height="240" columns="3"
             spacing="8" padding="8">
  <Button /><Button /><Button /><Button /><Button /><Button />
</UniformGrid>
```

Set `Rows`, `Columns`, or both. When one dimension is zero, the panel infers a
reasonable count from the number of children. Use `spacing` for both axes or
`column-spacing` and `row-spacing` separately.

This panel is ideal for inventory slots, keypads, portrait pickers, and galleries
whose cells should remain consistent.

