# NineSliceBrush

`NineSliceBrush` splits a sprite into four corners, four edges, and a center.
Corners remain pixel-sized while edges and center stretch.

```xml
<Button background-tint="#FFFFFFFF">
  <Button.Background>
    <NineSliceBrush sprite="Ui/button-frame" slice="6" />
  </Button.Background>
</Button>
```

`slice` accepts one inset or left, top, right, bottom values. Override individual
edges with `slice-left`, `slice-top`, `slice-right`, and `slice-bottom`.

Design the sprite so the center can stretch cleanly. The combined corner insets
form the brush's minimum size; an undersized destination proportionally reduces
opposing edges to fit.

