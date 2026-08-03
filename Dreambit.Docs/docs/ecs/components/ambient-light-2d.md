# AmbientLight2D

`AmbientLight2D` represents scene-wide base illumination. Each scene creates one
and exposes it as `AmbientLight`.

```csharp
AmbientLight.Color = new Color(70, 80, 110);
AmbientLight.Intensity = 0.6f;
```

Bright white ambient light makes sprites appear close to unlit. Lower intensity
and a cool or warm tint leave room for point lights. Ambient light is global, so
its transform is not meaningful for normal use.

