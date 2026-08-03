# UiText

`UiText` draws text with a FontStash font loaded by logical path.

```xml
<Text id="message" width="320" height="*"
      text="Welcome to Dreambit" font="monogram" font-size="24"
      text-color="#FFFFFFFF" horizontal-alignment="Center"
      multi-line="true" auto-resize-height="true" />
```

Set `Text`, `FontPath`, `FontSize`, `TextColor`, and `HorizontalAlignment` in code.
`MultiLine` enables line wrapping/line handling in the element. With
`AutoResizeHeight`, height becomes automatic and follows measured text.

Changing text or font values invalidates layout and dependencies as needed.
Keep frequently changing counters at a stable fixed size to avoid reflowing a
large tree every frame.

