# UiViewbox

`UiViewbox` fits one child into its bounds using a stretch mode.

```xml
<Viewbox width="320" height="180" stretch="Uniform">
  <Border width="160" height="90">
    <Text text="16:9 content" />
  </Border>
</Viewbox>
```

`None` keeps desired size, `Fill` scales axes independently, `Uniform` preserves
aspect ratio and fits inside, and `UniformToFill` preserves aspect ratio while
covering the full box.

The viewbox creates a centered layout slot; stretchable brushes and percentage
content fill it naturally. Use it for previews, fixed-design widgets, and
aspect-preserving art.

