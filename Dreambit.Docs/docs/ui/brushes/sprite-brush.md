# SpriteBrush

`SpriteBrush` stretches one sprite source rectangle to fill the owning bounds.

```xml
<Border background-tint="#FFFFFFFF">
  <Border.Background>
    <SpriteBrush sprite="Ui/panel" />
  </Border.Background>
</Border>
```

The sprite path is required. The brush loads a `Sprite` asset through
`Resources`, uses its native source size as minimum size, and multiplies its
pixels by the supplied tint.

Stretching also stretches corners. Use `NineSliceBrush` for framed panels and
buttons whose corner pixels should stay fixed.

