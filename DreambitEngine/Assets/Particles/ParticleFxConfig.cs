using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace Dreambit;

public class ParticleFxConfig : DreambitAsset
{
    // Emission
    public EmissionMode EmissionMode;
    public int EmissionRate { get;  init; } = 200;
    public List<Burst> Bursts { get; init; } = new();
    
    //Spawn
    public ParticleSpawnType SpawnType {get; init;} = ParticleSpawnType.Point;
    
    //Initial Properties
    public RangeF LifeTime { get; init; } = new(0.6f, 1.2f);
    public RangeF StartSpeed { get; init; } = new(80f, 160f);
    public Range2 StartSize { get; init; } = new(new(8, 8), new(24, 24));
    public RangeF StartRotationDeg { get; init; } = new(0f, 360f);
    public RangeF StartSpin { get; init; } = new(-2, 2f);
    public Range2 VelocityJitter { get; init; } = new(new(20, 10), new(40, 20));
    public Range2 PositionJitter { get; init; } = new(new(0, 0), new(0, 0));
    public Range2 StartAcceleration { get; init; } = new(new(0, 0), new(0, 0));
    public Color StartColor { get; init; } = Color.White;
    
    // Over-life curves
    public Curve1D AlphaOverLife { get; init; } = Curve1D.FadeOut();
    public Curve1D SizeOverLife { get; init; } = Curve1D.Bell();
    public Curve1D SpeedOverLife { get; init; } = Curve1D.Flat(1f);
    public Curve1D SpinOverLife { get; init; } = Curve1D.Flat(1f);

    //forces
    public Vector2 Gravity { get; set; } = new(0, 200);
    public float LinearDamping { get; set; } = 0.0f;
    
    // rendering
    public string Texture { get; set; }

    public void Validate()
    {
    }
}

public record struct RangeF(float Min, float Max);
public record struct Range2(Vector2 Min, Vector2 Max);

public sealed class Burst
{
    public float Time; 
    public int Count; 
    public int Cycles = 1; 
    public float Interval = 0.1f;
}

public enum EmissionMode
{
    Continuous,
    Burst
}

public enum ParticleSpawnType
{
    Point,
    Circular,
}