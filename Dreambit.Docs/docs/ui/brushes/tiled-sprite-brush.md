# TiledSpriteBrush

`TiledSpriteBrush` repeats a sprite at native pixel size across the destination.
The final row and column are cropped to remain within bounds.

```xml
<Border background-tint="#FFFFFFFF">
  <Border.Background>
    <TiledSpriteBrush sprite="Ui/noise-tile" />
  </Border.Background>
</Border>
```

The sprite path is required and resolves as a Dreambit `Sprite` asset. Use a
white tint for original colors or tint the repeated pattern through the owner.

Choose textures whose opposite edges match when you need seamless repetition.

