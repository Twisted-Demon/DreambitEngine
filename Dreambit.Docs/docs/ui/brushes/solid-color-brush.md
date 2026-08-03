# SolidColorBrush

`SolidColorBrush` fills the complete bounds with the tint supplied by the owning
control.

```xml
<Border background-tint="#263545FF">
  <Border.Background><SolidColorBrush /></Border.Background>
</Border>
```

The brush has no XML properties. Change `BackgroundTint` or the relevant tint
on the control to choose its color. Its minimum size is one pixel in each axis.

This is the cheapest and most predictable UI background. Combine it with an
`OutlineBrush` through `LayeredBrush` for a bordered panel.

