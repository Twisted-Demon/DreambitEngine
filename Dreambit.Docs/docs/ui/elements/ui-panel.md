# UiPanel

`UiPanel` is a general multi-child panel with the base container layout
behavior. It is useful as a neutral grouping node:

```xml
<Panel width="100%" height="100%">
  <Texture width="100%" height="100%" sprite="Ui/background" />
  <Text x="24" y="24" width="300" height="40" text="Overlay text" />
</Panel>
```

Children can overlap and use their own position, anchor, origin, and z-index.
`UiCanvas` is the explicitly named choice when the intent is free positioning;
use Grid or stack panels for structured layout.
