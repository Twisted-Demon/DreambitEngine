# OutlineBrush

`OutlineBrush` draws an inset rectangle outline with the owning control's tint.

```xml
<Border background-tint="#8FC7FFFF">
  <Border.Background>
    <OutlineBrush thickness="2" />
  </Border.Background>
</Border>
```

`thickness` accepts one value or left, top, right, bottom values. Individual
`left`, `top`, `right`, and `bottom` attributes override those edges:

```xml
<OutlineBrush thickness="1" bottom="4" />
```

When the requested edges exceed the available bounds, the brush scales opposing
edges to fit rather than drawing outside.

