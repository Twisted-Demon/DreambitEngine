# UiTexture

`UiTexture` draws a `Sprite` asset inside its arranged bounds.

```xml
<Texture width="96" height="96"
         sprite="Ui/portraits/captain" tint="#FFFFFFFF" />
```

In code, set `SpritePath` and `Tint`. Changing the path invalidates dependencies;
the sprite is resolved through `Resources` before drawing.

Use a sprite asset when you need a source rectangle. For backgrounds that should
tile, nine-slice, or combine with outlines, put the corresponding brush on a
`Border` instead.

