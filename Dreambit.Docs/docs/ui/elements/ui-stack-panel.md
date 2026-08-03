# Stack panels

Stack panels place children sequentially along one axis with `spacing`,
`padding`, `cross-alignment`, and `grow-direction`.

```xml
<VerticalStackPanel width="300" height="100%" spacing="10"
                    padding="12" cross-alignment="Center"
                    grow-direction="Start">
  <Button width="100%" height="40" />
  <Button width="100%" height="40" />
</VerticalStackPanel>
```

Use `<VerticalStackPanel>` or `<HorizontalStackPanel>` for fixed orientation.
`<StackPanel orientation="Vertical|Horizontal">` is configurable.

Cross alignment is `Start`, `Center`, or `End`. Grow direction can use
`Start`, `Center`, `End`, or the axis aliases `Top`, `Bottom`, `Left`, `Right`.
The complete child group is positioned within remaining main-axis space.

