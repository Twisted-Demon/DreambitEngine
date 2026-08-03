# UiBorder

`UiBorder` is a single-child surface with padding, alignment, tint, and a brush.
It adds no properties beyond `UiContentControl`; its name communicates intent.

```xml
<Border width="320" height="180" padding="16"
        background-tint="#242832FF">
  <Border.Background>
    <LayeredBrush>
      <SolidColorBrush />
      <OutlineBrush thickness="2" />
    </LayeredBrush>
  </Border.Background>
  <Text text="Panel content" font="monogram" font-size="20" />
</Border>
```

The element's `background-tint` is passed to every brush. Use a layered brush to
combine fill and outline without nesting extra borders.

