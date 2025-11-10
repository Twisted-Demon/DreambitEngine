using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.ECS;

public class ParticleSystemDrawer : DrawableComponent<ParticleSystemDrawer>
{
    private string _texturePath;
    private Texture2D Texture { get; set; }

    public string TexturePath
    {
        get => _texturePath;
        set
        {
            if (_texturePath == value)
                return;

            _texturePath = value;
            Texture = Resources.LoadAsset<Texture2D>(value);
        }
    }

    public Vector2 Origin
    {
        get
        {
            if (Texture is null) return Vector2.Zero;
            return new Vector2(Texture.Width * 0.5f, Texture.Height * 0.5f);
        }
    }

    public ParticleSimulation2D Simulation { get; private set; }
    public override Rectangle Bounds => Simulation.Bounds;

    public override void OnCreated()
    {
        Simulation = new ParticleSimulation2D(Transform);
        Simulation.UseLocalSpace = false;
    }

    public override void OnUpdate()
    {
        Simulation?.Update();
    }

    public override void OnDraw()
    {
        if (Texture is null) return;

        var parts = Simulation.GetParticles();
        float ox = 0f, oy = 0f;
        if (Simulation.UseLocalSpace)
        {
            var wp = Transform.WorldPosToVec2;
            ox = wp.X;
            oy = wp.Y;
        }

        for (var i = 0; i < parts.Alive; i++)
        {
            var phys = parts.INDICES[i];

            //position
            var px = parts.PX[phys] + ox;
            var py = parts.PY[phys] + oy;

            // size
            var sx = Mathf.Max(0.0001f, parts.SX[phys]);
            var sy = Mathf.Max(0.0001f, parts.SY[phys]);

            // rotation
            var rot = parts.ROT[phys];

            var color = parts.COLOR[phys];

            Core.SpriteBatch.Draw(
                Texture,
                new Vector2(px, py),
                null,
                color,
                rot,
                Origin,
                new Vector2(sx, sy),
                SpriteEffects.None,
                0f
            );
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
        if (Simulation == null) Simulation = new ParticleSimulation2D(Transform) { UseLocalSpace = true };
    }

    public override bool IsVisibleFromCamera(Rectangle cameraBounds)
    {
        return true;
    }
}