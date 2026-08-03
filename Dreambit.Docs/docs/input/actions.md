# Actions, maps, and bindings

An action describes intent; bindings describe devices that can produce it.

```csharp
public sealed class PlayerActions : InputActionMap<PlayerActions>
{
    public InputAction Move { get; }
    public InputAction Fire { get; }

    public PlayerActions()
    {
        Move = Add(new InputAction("Move", InputActionType.Value2D)
            .AddBinding(InputBinding.Composite2DKeys(
                Keys.W, Keys.S, Keys.A, Keys.D)));

        Fire = Add(new InputAction("Fire", InputActionType.Button)
            .AddBinding(InputBinding.KeyType(Keys.Space))
            .AddBinding(InputBinding.MouseType(MouseButton.Left)));
    }
}
```

Register a map, subscribe, and unregister it when the owning scene ends:

```csharp
_actions = new PlayerActions();
_actions.Move.Performed += context => _move = context.Value2D;
_actions.Move.Canceled += _ => _move = Vector2.Zero;
_actions.Fire.Started += _ => Fire();
InputSystem.Instance.Register(_actions);
```

Action types are `Button`, `Value1D`, and `Value2D`. Events are `Started`,
`Performed`, and `Canceled`; performed fires every held/nonzero frame. Bindings
support keys, mouse buttons, chords, mouse axes/scroll, composite WASD-style
keys, and composite mouse movement. Joystick `Axis2D` is declared but not read by
the current binding implementation; use direct gamepad input for it.

Disable an action or whole map for paused or context-specific controls.

