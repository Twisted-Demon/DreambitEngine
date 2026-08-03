# LayeredBrush

`LayeredBrush` draws several brushes in XML order into the same bounds.

```xml
<Border background-tint="#34495EFF">
  <Border.Background>
    <LayeredBrush>
      <SolidColorBrush />
      <OutlineBrush thickness="2" />
    </LayeredBrush>
  </Border.Background>
</Border>
```

All child brushes receive the same tint. Put fill-like brushes first and detail
brushes afterward. The layered minimum size is the maximum width and height
required by any child.

In code, add implementations to `Brushes` and invalidate the owning element's
dependencies/layout if the set changes after attachment.
