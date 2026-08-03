# RectDrawer

`RectDrawer` draws a filled world-space rectangle and is useful for prototypes,
debug geometry, and simple games.

```csharp
var rect = entity.AttachComponent<RectDrawer>();
rect.Width = 32;
rect.Height = 16;
rect.Color = Color.CornflowerBlue;
rect.PivotType = PivotType.Center;
rect.DrawLayer = 5;
```

Choose a standard `PivotType`, or set `Pivot` when using `PivotType.Custom`.
The component's `Bounds` is used for camera culling.

For an outlined shape, use `CircleDrawer` for circles or write a custom
`DrawableComponent` with `SpriteBatch` extension methods.

