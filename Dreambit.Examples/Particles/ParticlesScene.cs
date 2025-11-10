using System.Collections.Generic;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.Examples.Particles;

public class ParticlesScene : Scene<ParticlesScene>
{
    private ParticleSystemDrawer pSystem;
    private ParticleFxConfig _config;
    
    protected override void OnInitialize()
    {
        InitializeSettings();

        var particleSystem = CreateEntity(createAt: new Vector3(
            640, 360, 0)).AttachComponent<ParticleSystemDrawer>();

        particleSystem.TexturePath = "ParticlesScene/Textures/particle";
        
        pSystem = particleSystem;

        Logger.Info(MainCamera.Transform.Position.ToString());
        Logger.Info(particleSystem.Entity.Transform.WorldPosition.ToVector2().ToString());

        _config = new ParticleFxConfig
        {
            // Emit continuously; keep it modest so it looks natural
            EmissionMode = EmissionMode.Burst,
            EmissionRate = 100,

            // Spawn from a small area a bit above the flame center
            SpawnType = ParticleSpawnType.Point,

            // Lifetimes: short puffs mixed with longer wisps
            LifeTime      = new RangeF(1.8f, 5.2f),

            // Initial motion: mostly upward, with a bit of random speed
            StartSpeed    = new RangeF(0f, 0f),

            // Sizes: start small, grow via SizeOverLife
            StartSize     = new Range2(new(1f, 1f), new(1f, 1f)),

            // Randomized orientation and a little spin
            StartRotationDeg = new RangeF(0f, 360f),
            StartSpin     = new RangeF(-0.6f, 0.6f),

            // Soft gray-brown—adjust in your texture or here if you want warmer smoke
            StartColor    = new Color(110, 105, 100, 180),

            // Spawn jitter: spread around the ember bed a tiny bit
            PositionJitter = new Range2(new(-2f, -2f), new(2f, 2f)),

            // Velocity jitter: gentle sideways curls
            VelocityJitter = new Range2(new(-8f, -22f), new(8f, -10f)),

            // Small buoyant acceleration upward; add some side drift
            StartAcceleration = new Range2(new(-6f, -2f), new(4f, -4f)),

            // “Buoyancy”: negative Y gravity for upward lift (MonoGame Y+ is down)
            Gravity       = new Vector2(0f, -220f),

            // Light damping to slow over time without killing motion instantly
            LinearDamping = 1.2f,

            // Curves (t = 0..1 of particle life)
            // Alpha: quick fade-in, hold, then fade out
            AlphaOverLife = new Curve1D(
                new Curve1D.Key(0.00f, 0.00f),
                new Curve1D.Key(0.10f, 0.90f),
                new Curve1D.Key(0.70f, 0.90f),
                new Curve1D.Key(1.00f, 0.00f)
            ),

            // Size: expand as it rises, then stabilize a bit near the end
            SizeOverLife = new Curve1D(
                new Curve1D.Key(0.00f, 0.55f),
                new Curve1D.Key(0.15f, 0.90f),
                new Curve1D.Key(0.60f, 1.30f),
                new Curve1D.Key(1.00f, 1.60f)
            ),

            // Speed: strongest at birth, then slows as the puff diffuses
            SpeedOverLife = new Curve1D(
                new Curve1D.Key(0.00f, 1.00f),
                new Curve1D.Key(0.40f, 0.65f),
                new Curve1D.Key(1.00f, 0.35f)
            ),

            // Spin: slight decay to avoid aggressive twirling
            SpinOverLife = new Curve1D(
                new Curve1D.Key(0.00f, 1.00f),
                new Curve1D.Key(1.00f, 0.40f)
            ),
            Bursts = new List<Burst>
            {
                new Burst
                {
                    Time = 0.5f,
                    Count = 25,
                    Cycles = 10000,
                    Interval = 0.0f
                },
            }
        };

        pSystem.Simulation.SetParticleFxConfig(_config);
        pSystem.Simulation.Emit();
    }

    protected override void OnUpdate()
    {

    }

    private void InitializeSettings()
    {
        var windowWidth = 1280;
        var windowHeight = 720;
        
        Window.SetSize(windowWidth, windowHeight);

        MainCamera.PixelsPerUnit = 2;
        MainCamera.SetTargetVerticalResolution(720 );
        MainCamera.ForcePosition(new Vector3(
            windowWidth * 0.5f,
            windowHeight * 0.5f,
            0));

        UICamera.SetTargetVerticalResolution(windowHeight);
    }
}