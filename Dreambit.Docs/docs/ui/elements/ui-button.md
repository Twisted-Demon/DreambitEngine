# UiButton

`UiButton` is a focusable content control that raises `Clicked` for pointer,
keyboard, and gamepad activation.

```xml
<Button id="continue" width="220" height="44"
        background-tint="#365775FF"
        hover-tint="#4D789EFF"
        pressed-tint="#29445DFF">
  <Button.Background><SolidColorBrush /></Button.Background>
  <Text text="Continue" font="monogram" font-size="20" />
</Button>
```

```csharp
layout.GetRequired<UiButton>("continue").Clicked +=
    button => ContinueGame();
```

`IsHovered` and `IsPressed` expose transient state. Put one visual child inside;
use a Grid when the button needs an icon and label.

