using Microsoft.Xna.Framework;

namespace Dreambit.ECS;

[BlueprintType($"{nameof(ParticleSystemDrawer)}")]
public class ParticleSystemDrawer : DrawableComponent<ParticleSystemDrawer>
{
    private const float MinimumPixelsPerUnit = 0.0001f;

    private ParticleFxConfig _particleFx;
    private float _pixelsPerUnit = 1f;
    private bool _useLocalSpace;
    private TextureAsset _texture;

    [DreambitSerialize]
    public TextureAsset Texture
    {
        get => _texture;
        set
        {
            if (ReferenceEquals(_texture, value))
                return;

            _texture = value;
            UpdateRenderMetrics();
        }
    }

    [DreambitSerialize]
    public float PixelsPerUnit
    {
        get => _pixelsPerUnit;
        set
        {
            if (!float.IsFinite(value) || value < MinimumPixelsPerUnit)
                throw new System.ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    $"Pixels per unit must be finite and at least {MinimumPixelsPerUnit}.");

            _pixelsPerUnit = value;
            UpdateRenderMetrics();
        }
    }

    [DreambitSerialize]
    public bool UseLocalSpace
    {
        get => _useLocalSpace;
        set
        {
            _useLocalSpace = value;
            if (Simulation != null)
                Simulation.UseLocalSpace = value;
        }
    }

    [DreambitSerialize]
    public ParticleFxConfig ParticleFx
    {
        get => _particleFx;
        set
        {
            _particleFx = value;
            ApplyParticleFx();
        }
    }

    public Vector2 Origin
    {
        get
        {
            if (Texture?.Texture is null) return Vector2.Zero;
            return new Vector2(Texture.Width * 0.5f, Texture.Height * 0.5f);
        }
    }

    public ParticleSimulation2D Simulation { get; private set; }
    public override RectangleF Bounds => Simulation?.Bounds ?? RectangleF.Empty;

    public override void OnCreated()
    {
        Simulation = new ParticleSimulation2D(Transform);
        Simulation.UseLocalSpace = _useLocalSpace;
        UpdateRenderMetrics();
        ApplyParticleFx();
    }

    public override void OnUpdate()
    {
        Simulation?.Update();
    }

    protected override void OnDraw()
    {
        if (Texture?.Texture is null) return;

        var parts = Simulation.GetParticles();
        for (var i = 0; i < parts.Alive; i++)
        {
            var phys = parts.INDICES[i];

            var position = new Vector2(parts.PX[phys], parts.PY[phys]);
            var transformScale = Vector2.One;

            var sx = Mathf.Max(0.0001f, parts.SX[phys]);
            var sy = Mathf.Max(0.0001f, parts.SY[phys]);
            var rot = parts.ROT[phys];

            if (Simulation.UseLocalSpace)
            {
                position = Transform.TransformPoint2D(position);
                transformScale = Transform.WorldScale2D;
                rot += Transform.WorldRotation2D;
            }

            Core.SpriteBatch.DrawWorldSprite(
                Texture.Texture,
                position,
                null,
                parts.COLOR[phys],
                rot,
                Origin,
                new Vector2(sx, sy) * transformScale / PixelsPerUnit);
        }
    }

    public void Play()
    {
        EnsureSimulation();
        Simulation.Emit();
    }

    public void Stop()
    {
        Simulation?.StopEmit();
    }

    private void EnsureSimulation()
    {
        if (Simulation != null) return;

        Simulation = new ParticleSimulation2D(Transform) { UseLocalSpace = _useLocalSpace };
        UpdateRenderMetrics();
        ApplyParticleFx();
    }

    public override bool IsVisibleFromCamera(RectangleF cameraBounds)
    {
        return cameraBounds.Intersects(Bounds);
    }

    public void SetParticleFxConfig(ParticleFxConfig config)
    {
        ParticleFx = config;
    }

    private void ApplyParticleFx()
    {
        if (Simulation == null || _particleFx == null)
            return;

        Simulation.SetParticleFxConfig(_particleFx);
        if (_particleFx.Texture is not null)
            Texture = _particleFx.Texture;
    }

    private void UpdateRenderMetrics()
    {
        if (Simulation == null)
            return;

        Simulation.SetRenderMetrics(
            Texture?.Width ?? 1,
            Texture?.Height ?? 1,
            PixelsPerUnit);
    }
}
