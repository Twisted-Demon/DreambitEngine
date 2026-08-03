# Window

`Window` exposes the current back buffer and client sizes and applies common
display changes.

```csharp
Window.SetSize(1280, 720);
Window.SetAllowUserResizing(true);
Window.SetVsync(true);
Window.CenterOnPrimaryDisplay();
```

Useful read-only values include `Width`, `Height`, `BackBufferSize`,
`ClientWidth`, `ClientHeight`, `ScreenSize`, and `AspectRatio`.

## Display modes

```csharp
Window.SetFullscreen(true);
Window.SetBorderlessFullscreen(true);
Window.ToggleBorderlessFullscreen();
Window.SetBorderless(false);
```

Use one mode transition at a time and test it on every target platform.

## Coordinate conversion

Mouse input is reported in client coordinates. Convert it when the client and
back buffer differ:

```csharp
Vector2 backBufferPoint = Window.ClientToBackBuffer(Input.GetMousePosition());
Vector2 clientPoint = Window.BackBufferToClient(backBufferPoint);
```

`UiFrame` performs this conversion internally before routing pointer input.

