# UiCanvas

`UiCanvas` preserves each child's explicit `x`, `y`, size, anchor, origin, and
z-index. Use it for freeform HUD placement and overlays.

```xml
<Canvas width="100%" height="100%">
  <Texture x="24" y="24" width="48" height="48"
           sprite="Ui/heart" />
  <Text x="80" y="30" width="120" height="32"
        text="100" font="monogram" font-size="24" />
</Canvas>
```

Canvas does not create flow between siblings, so resizing can cause overlap.
Prefer percentages/anchors for responsive free placement, or use Grid and stack
panels when elements should affect one another.

