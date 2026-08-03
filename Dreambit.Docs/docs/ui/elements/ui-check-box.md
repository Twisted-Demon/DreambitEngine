# UiCheckBox

`UiCheckBox` combines a toggle button with an indicator and one content child.

```xml
<CheckBox id="music" width="240" height="30"
          is-checked="true" indicator-size="18"
          indicator-spacing="8"
          indicator-tint="#596575FF" mark-tint="#FFFFFFFF">
  <Text text="Music" font="monogram" font-size="18"
        horizontal-alignment="Left" />
</CheckBox>
```

Customize the indicator with `CheckBox.IndicatorBrush` and mark with
`CheckBox.MarkBrush`; tints are applied separately. Subscribe to inherited
`CheckedChanged` or read `IsChecked`.

Use `UiRadioButton` for mutually exclusive choices.

