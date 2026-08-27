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
Dreambit content root and cannot escape it. Author the source path as
`Ui/hud.xml`; the Asset Baker emits `Ui/hud.xmlb`, and `UiFrame` opens it from
the active blob manifest or `content.pak`.

`CreateComponent(path, idPrefix)` uses the same baked asset source, including
relative and `~/` references inside component XML, and returns one detached UI
subtree that you can add to a `UiContainer` in the current layout. See
[`UiContainer`](../../UI/Elements/UiContainer.md).
