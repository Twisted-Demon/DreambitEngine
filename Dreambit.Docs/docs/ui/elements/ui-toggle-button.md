# UiToggleButton

`UiToggleButton` is a button with persistent `IsChecked` state and a
`CheckedChanged` event.

```xml
<ToggleButton id="mute" width="160" height="40"
              is-checked="false" checked-tint="#9B4F4FFF">
  <Text text="Mute" font="monogram" font-size="18" />
</ToggleButton>
```

Activation toggles the state, whether it came from pointer, keyboard, or
gamepad. Subscribe with:

```csharp
toggle.CheckedChanged += (_, isChecked) => AudioMuted = isChecked;
```

Use `UiCheckBox` when you want the standard indicator and label arrangement.

