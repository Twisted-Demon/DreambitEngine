# ParticleSystemDrawer

`ParticleSystemDrawer` owns a `ParticleSimulation2D` and renders its particles
with one texture.

```csharp
var particles = CreateEntity("smoke", createAt: position)
    .AttachComponent<ParticleSystemDrawer>();

particles.TexturePath = "Particles/smoke";
particles.Simulation.SetParticleFxConfig(config);
particles.Play();
```

`Play` begins emission and `Stop` stops new particles while existing particles
finish. `Simulation.UseLocalSpace` controls whether particles follow the emitter
after spawning. The component defaults to world space when created normally.

See [Particles](../../assets/particles.md) for all configuration ranges, curves,
burst timing, and a complete example.

