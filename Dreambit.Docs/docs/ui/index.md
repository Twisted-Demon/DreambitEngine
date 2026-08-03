# User interface

Dreambit UI is a retained visual tree loaded from XML. A `UiFrame` component owns
a `UiLayout`; the layout measures and arranges elements, routes mouse, keyboard,
and controller input, then draws ordinary content and a topmost popup layer.

```csharp
var menu = CreateEntity("menu")
    .AttachComponent<UiFrame>()
    .WithLayout("Ui/main-menu.xml");

menu.Layout.GetRequired<UiButton>("play-button").Clicked +=
    _ => Scene.SetNextScene<GameScene>();
```

Start with [XML layouts](xml-layouts.md) and
[Layout, sizing, and positioning](layout.md). Browse the element pages for a
focused example of every shipped type and the brush pages for background styles.

UI automatically claims the device channels it uses. Normal `Input` queries are
suppressed for captured pointer, keyboard, or gamepad channels, which prevents a
menu click from also firing gameplay input.

