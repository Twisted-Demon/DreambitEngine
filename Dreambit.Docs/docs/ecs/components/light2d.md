# Light2D

`Light2D` is the abstract base for 2D lights. It supplies world `Position`,
`Color`, and `Intensity` and registers the light with the scene's lighting list.

```csharp
light.Color = new Color(255, 180, 100);
light.Intensity = 1.25f;
light.Transform.WorldPosition2D = torchPosition;
```

Use [`PointLight2D`](point-light-2d.md) for local lights. A scene already creates
one [`AmbientLight2D`](ambient-light-2d.md) for global illumination.

Custom lights should derive from `Light2D`, implement visible `Bounds`, and work
with the effect uniforms expected by the 2D lighting render pass.

