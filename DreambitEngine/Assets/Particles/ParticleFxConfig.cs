using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Dreambit;

public class ParticleFxConfig : DreambitAsset
{
    // Emission
    public EmissionMode EmissionMode;
    public int EmissionRate { get; init; } = 200;
    public List<Burst> Bursts { get; init; } = new();

    //Spawn
    public ParticleSpawnType SpawnType { get; init; } = ParticleSpawnType.Point;

    //Initial Properties
    public RangeF LifeTime { get; init; } = new(0.6f, 1.2f);
    public RangeF StartSpeed { get; init; } = new(80f, 160f);
    public Range2 StartSize { get; init; } = new(new Vector2(8, 8), new Vector2(24, 24));
    public RangeF StartRotationDeg { get; init; } = new(0f, 360f);
    public RangeF StartSpin { get; init; } = new(-2, 2f);
    public Range2 VelocityJitter { get; init; } = new(new Vector2(20, 10), new Vector2(40, 20));
    public Range2 PositionJitter { get; init; } = new(new Vector2(0, 0), new Vector2(0, 0));
    public Range2 StartAcceleration { get; init; } = new(new Vector2(0, 0), new Vector2(0, 0));
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
        if (EmissionRate < 0)
            throw new System.InvalidOperationException("Particle emission rate cannot be negative.");

        ValidateRange(LifeTime, nameof(LifeTime), true);
        ValidateRange(StartSpeed, nameof(StartSpeed));
        ValidateRange(StartRotationDeg, nameof(StartRotationDeg));
        ValidateRange(StartSpin, nameof(StartSpin));
        ValidateRange(StartSize, nameof(StartSize), true);
        ValidateRange(VelocityJitter, nameof(VelocityJitter));
        ValidateRange(PositionJitter, nameof(PositionJitter));
        ValidateRange(StartAcceleration, nameof(StartAcceleration));

        if (!float.IsFinite(LinearDamping) || LinearDamping < 0f)
            throw new System.InvalidOperationException("Particle linear damping must be finite and non-negative.");

        if (!IsFinite(Gravity))
            throw new System.InvalidOperationException("Particle gravity must be finite.");

        if (Bursts == null)
            throw new System.InvalidOperationException("Particle burst collection cannot be null.");

        foreach (var burst in Bursts)
        {
            if (burst == null || burst.Count < 0 || burst.Cycles < 0 ||
                !float.IsFinite(burst.Interval) || burst.Interval < 0f ||
                !float.IsFinite(burst.Time) || burst.Time < 0f)
                throw new System.InvalidOperationException("Particle burst values must be finite and non-negative.");
        }

        if (AlphaOverLife == null || SizeOverLife == null ||
            SpeedOverLife == null || SpinOverLife == null)
            throw new System.InvalidOperationException("Particle over-life curves cannot be null.");
    }

    private static void ValidateRange(RangeF range, string name, bool positive = false)
    {
        if (!float.IsFinite(range.Min) || !float.IsFinite(range.Max) ||
            range.Max < range.Min || positive && range.Min <= 0f)
            throw new System.InvalidOperationException($"Particle {name} range is invalid.");
    }

    private static void ValidateRange(Range2 range, string name, bool nonNegative = false)
    {
        if (!IsFinite(range.Min) || !IsFinite(range.Max) ||
            range.Max.X < range.Min.X || range.Max.Y < range.Min.Y ||
            nonNegative && (range.Min.X < 0f || range.Min.Y < 0f))
            throw new System.InvalidOperationException($"Particle {name} range is invalid.");
    }

    private static bool IsFinite(Vector2 value)
    {
        return float.IsFinite(value.X) && float.IsFinite(value.Y);
    }
}

public record struct RangeF(float Min, float Max);

public record struct Range2(Vector2 Min, Vector2 Max);

public sealed class Burst
{
    public int Count;
    public int Cycles = 1;
    public float Interval = 0.1f;
    public float Time;
}

public enum EmissionMode
{
    Continuous,
    Burst
}

public enum ParticleSpawnType
{
    Point,
    Circular
}
