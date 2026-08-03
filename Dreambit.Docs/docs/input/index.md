# Input

Dreambit offers two input levels:

- `Input` for direct keyboard, mouse, and primary-gamepad queries.
- `InputAction`, `InputActionMap`, and `InputBinding` for named, rebindable
  gameplay intent.

`Core` samples devices and updates action maps automatically. UI is routed before
gameplay actions; any device channel consumed by UI is suppressed from normal
queries for that frame.

Use direct input for a small prototype and action maps when several controls,
enable/disable contexts, or event-driven input will make the game easier to
maintain.

