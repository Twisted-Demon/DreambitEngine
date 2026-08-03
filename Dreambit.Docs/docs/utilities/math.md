# Math and ranges

`Mathf` supplies common scalar helpers and constants: clamp/saturate, min/max,
round/floor/ceiling, degree/radian conversion, wrapping, smooth-step functions,
and other utility operations.

```csharp
float angle = Mathf.Radians(90);
float health01 = Mathf.Saturate(health / maxHealth);
float wrapped = Mathf.WrapRadians(angle);
```

`RectangleF` is the floating-point counterpart to MonoGame `Rectangle`, with
containment, intersection, union, inflation, offset, center, and conversion.
Drawable components use it for world bounds.

`Matrix2D` is a compact affine 2D matrix with translation, rotation, scale,
creation, multiplication, inversion, interpolation, and implicit conversion to
MonoGame `Matrix`.

`Curve1D` linearly interpolates sorted time/value keys. Presets include
`FadeIn`, `FadeOut`, `Bell`, `EaseInOut`, and `Flat`. Particle configurations use
curves over normalized lifetime.

Range record types include `FloatRange`, `IntRange`, `Vector2Range`,
`Vector3Range`, and `Vector4Range`. Particles also define `RangeF` and `Range2`.

