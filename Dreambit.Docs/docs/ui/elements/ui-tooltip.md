# UiTooltip

`UiTooltip` is a popup that opens after its owner remains hovered for `Delay`
seconds.

```xml
<Button id="help" width="40" height="40">
  <Text text="?" />
  <Button.Tooltip>
    <Tooltip width="240" height="*" delay="0.35"
             padding="10" background-tint="#11141AFF">
      <Tooltip.Background><SolidColorBrush /></Tooltip.Background>
      <Text text="Open the controls guide" multi-line="true" />
    </Tooltip>
  </Button.Tooltip>
</Button>
```

Tooltips use the layout's topmost popup layer and close when hover is lost. Keep
tooltip content nonessential because touch/controller users may not hover.

