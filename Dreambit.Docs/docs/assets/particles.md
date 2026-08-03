# Particles

Create a `ParticleSystemDrawer`, assign a texture, configure its simulation, and
start emission:

```csharp
var fx = entity.AttachComponent<ParticleSystemDrawer>();
fx.TexturePath = "Particles/smoke";
fx.Simulation.UseLocalSpace = false;
fx.Simulation.SetParticleFxConfig(new ParticleFxConfig
{
    EmissionMode = EmissionMode.Continuous,
    EmissionRate = 80,
    SpawnType = ParticleSpawnType.Point,
    LifeTime = new RangeF(0.8f, 1.4f),
    StartSpeed = new RangeF(20f, 50f),
    StartSize = new Range2(new(0.5f), new(1.2f)),
    StartColor = Color.White,
    Gravity = new Vector2(0, -20),
    AlphaOverLife = Curve1D.FadeOut()
});
fx.Play();
```

Initial ranges cover lifetime, speed, size, rotation, spin, position/velocity
jitter, acceleration, and color. Curves modify alpha, size, speed, and spin over
normalized lifetime. `Gravity` and `LinearDamping` affect motion.

Burst mode uses `Burst` entries with `Time`, `Count`, `Cycles`, and `Interval`.
`Stop` stops emission without deleting live particles.

With `UseLocalSpace = false`, emitted particles remain in world space when the
emitter moves. Local space applies the emitter position during drawing.
