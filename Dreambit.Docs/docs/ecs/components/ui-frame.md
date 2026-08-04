# UiFrame

`UiFrame` loads an XML UI document, routes pointer/keyboard/gamepad input, and
draws it with the scene's UI camera.

```csharp
var frame = CreateEntity("hud")
    .AttachComponent<UiFrame>()
    .WithLayout("Ui/hud.xml");

frame.Layout.GetRequired<UiText>("score").Text = "0";
```

`LayoutPath` and `WithLayout` load immediately. Paths must be relative to the
application's `Content` root and cannot escape it. Keep UI XML as copied content,
not only inside the pak.

`CreateComponent(path, idPrefix)` loads one detached UI subtree that you can add
to a `UiContainer` in the current layout. See
[`UiContainer`](../../UI/Elements/UiContainer.md).
