# UiWrapPanel

`UiWrapPanel` lays out children along one axis and begins a new line when the
available main-axis space is exhausted.

```xml
<WrapPanel width="100%" height="*" orientation="Horizontal"
           spacing="8" line-spacing="12"
           cross-alignment="Center" padding="8">
  <Button width="120" height="36" />
  <Button width="160" height="36" />
  <Button width="100" height="36" />
</WrapPanel>
```

Use vertical orientation for column-first wrapping. `CrossAlignment` controls
items within each line. Unlike `UniformGrid`, child sizes may differ.

Give the panel a bounded width or height on its main wrapping dimension; an
unbounded panel has no point at which to wrap.
