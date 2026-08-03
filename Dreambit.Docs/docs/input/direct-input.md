# Direct input

Query transitions in `OnUpdate`:

```csharp
if (Input.IsKeyPressed(Keys.Space)) Fire();
if (Input.IsKeyHeld(Keys.A)) MoveLeft();
if (Input.IsKeyReleased(Keys.Escape)) ClosePause();

Vector2 mouse = Input.GetMousePosition();
Vector2 delta = Input.GetMouseDelta();
int wheel = Input.GetScrollDelta();

if (Input.IsMousePressed(MouseButton.Left)) Select(mouse);
if (Input.IsGamePadButtonPressed(Buttons.A)) Confirm();
Vector2 stick = Input.GetGamePadLeftStick();
```

Pressed and released are true for one sampled frame; held stays true. Mouse
position is in window client coordinates. Convert through `Window` and the
camera for world picking:

```csharp
var backBuffer = Window.ClientToBackBuffer(Input.GetMousePosition());
var world = Scene.MainCamera.ScreenToWorld(backBuffer);
```

Inspect `Input.IsPointerCapturedByUi`, `IsKeyboardCapturedByUi`, and
`IsGamePadCapturedByUi` when debugging why a gameplay query is suppressed.
This suppression is intentional whenever a UI layout owns that channel.

