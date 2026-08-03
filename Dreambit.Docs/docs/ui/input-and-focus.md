# Input, focus, and events

Interactive controls handle pointer, keyboard, and primary-gamepad navigation.
Tab moves sequential focus; arrows and the D-pad move directionally; Enter,
Space, or gamepad A activates; Escape or gamepad B cancels.

Common element events route from the source toward its ancestors:

```csharp
var button = layout.GetRequired<UiButton>("save");
button.Clicked += _ => Save();
button.GotFocus += (_, _) => ShowHint();
button.KeyPressed += (_, args) =>
{
    if (args.Key == Keys.F1)
        args.Handled = true;
};
```

Pointer events include pressed, released, moved, and wheel. Controls can call
`CapturePointer()` during dragging and `ReleasePointerCapture()` afterward.

`is-visible`, `is-enabled`, `is-hit-test-visible`, `is-focusable`, and
`captures-keyboard-input` configure interaction. Effective visibility and
enabled state include ancestors.

`UiLayout.FocusedElement` and `PointerCapturedElement` expose current ownership.
`Focus()` fails when an element is unavailable or not focusable. Blocking
overlays restrict focus candidates to their own subtree.

