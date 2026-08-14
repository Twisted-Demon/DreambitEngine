using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Dreambit.ECS;

namespace Dreambit;

[DreambitAssetType(
    "dreambit.particle-fx-config",
    FileExtension = DreambitAssetFileExtensions.ParticleFx)]
public class ParticleFxConfig : DreambitAsset
{
    // Emission
    [DreambitSerialize]
    public EmissionMode EmissionMode;
    [DreambitSerialize]
    public int EmissionRate { get; init; } = 200;
    [DreambitSerialize]
    public List<Burst> Bursts { get; init; } = new();

    //Spawn
    [DreambitSerialize]
    public ParticleSpawnType SpawnType { get; init; } = ParticleSpawnType.Point;

    //Initial Properties
    [DreambitSerialize]
    public RangeF LifeTime { get; init; } = new(0.6f, 1.2f);
    [DreambitSerialize]
    public RangeF StartSpeed { get; init; } = new(80f, 160f);
    [DreambitSerialize]
    public Range2 StartSize { get; init; } = new(new Vector2(8, 8), new Vector2(24, 24));
    [DreambitSerialize]
    public RangeF StartRotationDeg { get; init; } = new(0f, 360f);
    [DreambitSerialize]
    public RangeF StartSpin { get; init; } = new(-2, 2f);
    [DreambitSerialize]
    public Range2 VelocityJitter { get; init; } = new(new Vector2(20, 10), new Vector2(40, 20));
    [DreambitSerialize]
    public Range2 PositionJitter { get; init; } = new(new Vector2(0, 0), new Vector2(0, 0));
    [DreambitSerialize]
    public Range2 StartAcceleration { get; init; } = new(new Vector2(0, 0), new Vector2(0, 0));
    [DreambitSerialize]
    public Color StartColor { get; init; } = Color.White;

    // Over-life curves
    [DreambitSerialize]
    public Curve1D AlphaOverLife { get; init; } = Curve1D.FadeOut();
    [DreambitSerialize]
    public Curve1D SizeOverLife { get; init; } = Curve1D.Bell();
    [DreambitSerialize]
    public Curve1D SpeedOverLife { get; init; } = Curve1D.Flat(1f);
    [DreambitSerialize]
    public Curve1D SpinOverLife { get; init; } = Curve1D.Flat(1f);

    //forces
    [DreambitSerialize]
    public Vector2 Gravity { get; set; } = new(0, 200);
    [DreambitSerialize]
    public float LinearDamping { get; set; } = 0.0f;

    // rendering
    [DreambitSerialize]
    public TextureAsset Texture { get; set; }

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

public record struct RangeF(
    [property: DreambitSerialize] float Min,
    [property: DreambitSerialize] float Max);

public record struct Range2(
    [property: DreambitSerialize] Vector2 Min,
    [property: DreambitSerialize] Vector2 Max);

public sealed class Burst
{
    [DreambitSerialize]
    public int Count;
    [DreambitSerialize]
    public int Cycles = 1;
    [DreambitSerialize]
    public float Interval = 0.1f;
    [DreambitSerialize]
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
