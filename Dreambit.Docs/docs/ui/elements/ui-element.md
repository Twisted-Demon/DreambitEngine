# UiElement

`UiElement` is the abstract base of every visual. It owns identity, parent and
children references, common layout values, visibility, enabled state, focus,
pointer capture, routed events, measurement, arrangement, and drawing.

Use its common attributes on every concrete tag:

```xml
<Text id="status" x="50%" y="20" width="320" height="*"
      anchor="TopCenter" origin="TopCenter" z="2"
      is-visible="true" is-enabled="true"
      clip-to-bounds="false" />
```

In code, `Bounds` is the final arranged rectangle and `DesiredSize` is the result
of measurement. `IsEffectivelyVisible` and `IsEffectivelyEnabled` include the
ancestor chain.

Derive from this type for leaf visuals. Derive from `UiContainer` when the type
owns visual children.

