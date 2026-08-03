# Lighting

The default `Basic2dLightingRenderPass` combines ambient light with visible point
lights for drawable effects that use the lighting uniforms.

```csharp
AmbientLight.Color = new Color(70, 80, 110);
AmbientLight.Intensity = 0.5f;

var lamp = CreateEntity("lamp", createAt: position)
    .AttachComponent<PointLight2D>();
lamp.Radius = 140f;
lamp.Color = new Color(255, 190, 110);
lamp.Intensity = 1.2f;
```

The lighting uniform path supports at most 32 point lights in a batch. Cull or
disable low-value lights, especially when many temporary effects overlap.

Lights follow entity transforms. Radius determines a point light's bounds;
ambient light has scene-wide bounds. Set `Scene.DebugMode = true` to visualize
point-light bounds through their debug hook.

Custom effects must expose the uniform names expected by `LightingUniforms` to
receive Dreambit's light array, camera data, and ambient values.

