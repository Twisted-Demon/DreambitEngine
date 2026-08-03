# UiContentControl

`UiContentControl` accepts one visual child and adds `Padding`,
`ContentAlignment`, `Background`, and `BackgroundTint`.

```xml
<ContentControl width="260" height="80" padding="12"
                content-alignment="Center" background-tint="#26313DFF">
  <ContentControl.Background><SolidColorBrush /></ContentControl.Background>
  <Text text="One child" font="monogram" font-size="18" />
</ContentControl>
```

`AddChild` replaces the existing content behavior with a single-child contract;
use `SetContent` in code. For a semantic surface, prefer `Border`; for an
interactive control, use `UiControl` or one of its derived controls.

