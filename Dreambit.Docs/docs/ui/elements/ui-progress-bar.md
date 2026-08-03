# UiProgressBar

`UiProgressBar` displays the inherited range value without accepting user input.

```xml
<ProgressBar id="health" width="280" height="18"
             minimum="0" maximum="100" value="75"
             orientation="Horizontal"
             track-tint="#303641FF" fill-tint="#55C478FF">
  <ProgressBar.TrackBrush><SolidColorBrush /></ProgressBar.TrackBrush>
  <ProgressBar.FillBrush><SolidColorBrush /></ProgressBar.FillBrush>
</ProgressBar>
```

Set `Value` in code; it is clamped between `Minimum` and `Maximum`. Use
`NormalizedValue` when you need the 0–1 fraction. Both horizontal and vertical
orientation are supported.

Use brushes for textured or nine-sliced bars; their tints remain separate.
