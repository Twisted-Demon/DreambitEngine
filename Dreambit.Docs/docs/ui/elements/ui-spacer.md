# UiSpacer

`UiSpacer` is an invisible layout item with no drawing or input. Use it inside a
stack, wrap, or grid when a fixed or flexible gap should participate as a child.

```xml
<HorizontalStackPanel width="100%" height="40">
  <Text width="120" text="Score" />
  <Spacer width="*" height="1" />
  <Text width="80" text="1000" />
</HorizontalStackPanel>
```

In code, `new UiSpacer(24, 1)` creates a pixel-sized spacer. Prefer a panel's
`spacing` for uniform gaps between all children.

