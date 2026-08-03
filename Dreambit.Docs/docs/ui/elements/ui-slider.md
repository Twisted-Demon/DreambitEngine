# UiSlider

`UiSlider` lets the user edit a numeric range by pointer or navigation input.

```xml
<Slider id="volume" width="280" height="28"
        minimum="0" maximum="100" value="60" step="5"
        orientation="Horizontal" track-thickness="5" thumb-size="16"
        track-tint="#424A58FF" fill-tint="#55A5EEFF"
        thumb-tint="#FFFFFFFF" />
```

Optional `TrackBrush`, `FillBrush`, and `ThumbBrush` property elements replace
the built-in solid drawing. Subscribe to inherited `ValueChanged`.

Pointer dragging captures input until release. Keyboard/controller navigation
changes the value by `Step`. Vertical sliders are supported with
`orientation="Vertical"`.

