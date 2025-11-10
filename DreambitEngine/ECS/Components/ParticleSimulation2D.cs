using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit.ECS;

/// <summary>
/// Simulation only, particle system
/// </summary>
public class ParticleSimulation2D : ICanLog<ParticleSimulation2D>
    {
        // -------- Capacity / version ----------
        private int Capacity { get; set; }
        public int Version { get; private set; }

        // -------- World / transform -----------
        public bool UseLocalSpace { get; set; } = false;
        private Transform Transform { get; set; }

        // -------- Global AABB -----------------
        public Rectangle Bounds => _aabb;
        private Rectangle _aabb;
        private const float AabbPad = 1.0f;
        private const float MinExtent = 0.5f;

        // -------- Config ----------------------
        private ParticleFxConfig _config;

        // -------- SoA: simulation state -------
        private float[] _px, _py;          // position
        private float[] _vx, _vy;          // current velocity
        private float[] _ax, _ay;          // per-particle constant accel (from StartAcceleration)
        private float[] _rot;              // rotation (radians)
        private float[] _spin;             // current spin (rads/sec) — derived each step from base*curve
        private float[] _life, _maxLife;   // remaining life, initial life
        private Color[] _color;            // current color (alpha will be curve-applied)
        private float[] _sx, _sy;          // current size
        // Bases for over-life curves
        private float[] _sx0, _sy0;        // base start size
        private float[] _spin0;            // base spin
        private float[] _speed0;           // base speed magnitude (before SpeedOverLife)
        private float[] _dirx, _diry;      // normalized initial direction (for speed curve scaling)
        private float[] _alpha0;           // base alpha (from StartColor.A)

        // -------- Alive set compaction --------
        private int _alive;                // [0..Capacity)
        private int[] _indices;            // logical -> physical
        private int[] _rev;                // physical -> logical

        // -------- Emission control ------------
        private readonly Random _rng = new();
        private bool _isPlaying;
        private TimeSpan _spawnDelta = TimeSpan.Zero;
        private TimeSpan _targetSpawnTime;
        private double _playheadSec;                      // time since Emit() for scheduling bursts
        private int _burstCycle;                          // which cycle of bursts we're on
        private double _nextBurstWindowStartSec;          // start time for current cycle window
        private int[] _burstCounters;                     // how many spawned so far in current cycle per burst
        private double[] _burstNextTimes;                 // next scheduled fire time within a cycle per burst

        public ParticleSimulation2D(Transform transform) => Transform = transform;

        // ------------------- Public API -------------------

        public void SetParticleFxConfig(ParticleFxConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _config.Validate(); // enforces sane values, e.g., non-negative rates  :contentReference[oaicite:4]{index=4}

            // Capacity heuristic:
            // Continuous: rate * maxLife
            // Burst: sum of max burst counts across cycles (first cycle worst-case) + a small pad
            int capacity = 64;
            if (_config.EmissionMode == EmissionMode.Continuous)
            {
                float maxLife = Math.Max(1e-3f, _config.LifeTime.Max);
                capacity = Math.Max(64, (int)Math.Ceiling(_config.EmissionRate * maxLife));
            }
            else // Burst
            {
                int peak = 0;
                foreach (var b in _config.Bursts)
                {
                    int perCycle = Math.Max(0, b.Count);
                    // In case multiple bursts overlap, sum their per-cycle counts
                    peak += perCycle;
                }
                // Allow a couple of overlapping cycles if Interval is tiny
                capacity = Math.Max(64, peak * 2);
            }

            Capacity = capacity;

            // Allocate SoA
            _px = new float[capacity]; _py = new float[capacity];
            _vx = new float[capacity]; _vy = new float[capacity];
            _ax = new float[capacity]; _ay = new float[capacity];
            _rot = new float[capacity];
            _spin = new float[capacity];
            _life = new float[capacity]; _maxLife = new float[capacity];
            _color = new Color[capacity];
            _sx = new float[capacity]; _sy = new float[capacity];

            _sx0 = new float[capacity]; _sy0 = new float[capacity];
            _spin0 = new float[capacity];
            _speed0 = new float[capacity];
            _dirx = new float[capacity]; _diry = new float[capacity];
            _alpha0 = new float[capacity];

            _indices = new int[capacity];
            _rev = new int[capacity];
            for (int i = 0; i < capacity; i++) { _indices[i] = i; _rev[i] = i; }
            _alive = 0;
            _aabb = Rectangle.Empty;
            Version++;

            // Continuous timing
            _targetSpawnTime = _config.EmissionRate > 0
                ? TimeSpan.FromSeconds(1.0 / Math.Max(1, _config.EmissionRate))
                : TimeSpan.MaxValue;

            // Burst scheduling
            SetupBurstSchedule();

            // Override globals from config (forces)
            // (Gravity and damping are applied in Simulate each frame)
            // These come from config each update to allow swapping configs live.
        }

        public void Emit()
        {
            _isPlaying = true;
            _spawnDelta = TimeSpan.Zero;
            _playheadSec = 0.0;
            _burstCycle = 0;
            ResetBurstCycleState();
            // If EmissionMode == Burst and a burst starts at t=0, we'll catch it in Update() immediately.
        }

        public void StopEmit() => _isPlaying = false;

        public void Update()
        {
            if (_config == null) return;

            // Drive emission (continuous / burst)
            EmitStep();

            // Integrate simulation & apply curves
            SimulateStep();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyParticles GetParticles() =>
            new(_indices, _px, _py, _vx, _vy, _sx, _sy, _rot, _life, _maxLife, _color, _spin, _ax, _ay, _alive);

        public ILogger Logger { get; } = new Logger<ParticleSimulation2D>();

        // ------------------- Emission -------------------

        private void EmitStep()
        {
            if (!_isPlaying) return;

            float dt = Time.DeltaTime;
            if (dt <= 0f) return;

            // continuous
            switch (_config.EmissionMode)
            {
                case EmissionMode.Continuous:
                    EmitContinuous();
                    break;
                case EmissionMode.Burst:
                    EmitBurst();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void EmitContinuous()
        {
            _spawnDelta += Time.GameTime.ElapsedGameTime;
            while (_spawnDelta >= _targetSpawnTime)
            {
                _spawnDelta -= _targetSpawnTime;
                SpawnOne();
            }
        }

        private void EmitBurst()
        {
            _playheadSec += Time.DeltaTime;

            // Handle burst cycles rolling over
            double cycleStart = _nextBurstWindowStartSec;
            bool anyPending = false;

            for (int i = 0; i < _config.Bursts.Count; i++)
            {
                ref int fired = ref _burstCounters[i];
                var b = _config.Bursts[i];
                if (_burstCycle >= b.Cycles && b.Cycles > 0) continue;

                anyPending = true;

                double nextT = _burstNextTimes[i];
                // Fire if we've reached the next burst time
                while (_playheadSec >= nextT && fired < b.Count && _alive < Capacity)
                {
                    SpawnOne();
                    fired++;
                    nextT += Math.Max(0.0, b.Interval);
                }

                _burstNextTimes[i] = nextT;
            }

            // If all bursts for this cycle have fired, and at least one had limited count, advance cycle
            bool cycleDone = true;
            for (int i = 0; i < _config.Bursts.Count; i++)
            {
                var b = _config.Bursts[i];
                if (b.Cycles <= 0 || _burstCycle < b.Cycles) // still active
                {
                    if (_burstCounters[i] < b.Count) { cycleDone = false; break; }
                }
            }

            if (anyPending && cycleDone)
            {
                _burstCycle++;
                if (HasMoreCycles())
                {
                    _nextBurstWindowStartSec = _playheadSec;
                    ResetBurstCycleState();
                }
                else
                {
                    // Completed all cycles; stop emitting but keep simulating live particles
                    _isPlaying = false;
                }
            }
        }

        private void SpawnOne()
        {
            if (_alive >= Capacity) return;

            // Lifetime
            float life = RandRange(_config.LifeTime.Min, _config.LifeTime.Max);

            // Start size
            Vector2 size = RandRange2(_config.StartSize.Min, _config.StartSize.Max);

            // Rotation / spin
            float startRotDeg = RandRange(_config.StartRotationDeg.Min, _config.StartRotationDeg.Max);
            float startRot = MathHelper.ToRadians(startRotDeg);
            float spin = RandRange(_config.StartSpin.Min, _config.StartSpin.Max);

            // Start acceleration
            Vector2 accel = RandRange2(_config.StartAcceleration.Min, _config.StartAcceleration.Max);

            // Base color
            Color color = _config.StartColor;

            // Position (spawn type + jitter)
            Vector2 pos = SpawnPointBase();
            pos += RandRange2(_config.PositionJitter.Min, _config.PositionJitter.Max);

            // Velocity direction + jitter + start speed
            float speed = RandRange(_config.StartSpeed.Min, _config.StartSpeed.Max);
            Vector2 dir = RandomUnitDirection2D();
            Vector2 vel = dir * speed;

            // Apply velocity jitter additively
            Vector2 vj = RandRange2(_config.VelocityJitter.Min, _config.VelocityJitter.Max);
            vel += vj;

            // Insert
            InsertNewParticle(pos, vel, size, startRot, spin, life, color, accel, dir, speed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InsertNewParticle(
            Vector2 position, Vector2 velocity, Vector2 size,
            float rotation, float spin, float lifetimeSeconds,
            Color color, Vector2 acceleration, Vector2 dir, float baseSpeed)
        {
            int phys = _indices[_alive++];
            _px[phys] = position.X;
            _py[phys] = position.Y;

            _vx[phys] = velocity.X;
            _vy[phys] = velocity.Y;

            _ax[phys] = acceleration.X;
            _ay[phys] = acceleration.Y;

            _rot[phys] = rotation;

            _sx[phys] = size.X;
            _sy[phys] = size.Y;
            _sx0[phys] = Math.Max(0f, size.X);
            _sy0[phys] = Math.Max(0f, size.Y);

            _spin0[phys] = spin;
            _spin[phys] = spin;

            _life[phys] = lifetimeSeconds;
            _maxLife[phys] = Math.Max(1e-6f, lifetimeSeconds);

            _color[phys] = color;
            _alpha0[phys] = color.A / 255f;

            // Store base speed & direction to re-scale by SpeedOverLife
            var len = (float)Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
            if (len > 1e-6f) { dir /= len; }
            _dirx[phys] = dir.X;
            _diry[phys] = dir.Y;
            _speed0[phys] = baseSpeed;

            Version++;
        }

        // ------------------- Simulation -------------------

        private void SimulateStep()
        {
            float dt = Time.DeltaTime;
            if (dt <= 0f || _alive == 0) { _aabb = Rectangle.Empty; return; }

            // adopt config forces each frame (so swapping configs mid-play is OK)
            Vector2 gravity = _config.Gravity;
            float damping = Math.Max(0f, _config.LinearDamping);
            float dampFactor = damping > 0f ? Mathf.Exp(-damping * dt) : 1f;

            // AABB
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;

            int i = 0;
            while (i < _alive)
            {
                int phys = _indices[i];

                // lifetime
                float life = _life[phys] - dt;
                if (life <= 0f)
                {
                    KillAt(i);
                    continue;
                }
                _life[phys] = life;

                float tNorm = 1f - (life / _maxLife[phys]); // 0..1
                tNorm = Math.Clamp(tNorm, 0f, 1f);

                // Over-life curves (evaluate once)
                float sizeScale = _config.SizeOverLife.Evaluate(tNorm);   // scales base size  :contentReference[oaicite:5]{index=5}
                float speedScale = _config.SpeedOverLife.Evaluate(tNorm); // scales base speed
                float spinScale  = _config.SpinOverLife.Evaluate(tNorm);  // scales base spin
                float alphaScale = _config.AlphaOverLife.Evaluate(tNorm); // scales alpha

                // Spin update from base * curve
                float spin = _spin0[phys] * spinScale;
                _spin[phys] = spin;
                _rot[phys] += spin * dt;

                // Velocity = (baseDir * baseSpeed * speedScale) + per-particle jitter already embedded
                float vx = _dirx[phys] * _speed0[phys] * speedScale;
                float vy = _diry[phys] * _speed0[phys] * speedScale;

                // Add whatever jitter component existed initially by blending toward stored current velocity magnitude
                // (This keeps the initial jitter influence while allowing speed curve control.)
                // Compute delta between target and current, damp toward target:
                float vcx = _vx[phys];
                float vcy = _vy[phys];
                const float followRate = 12f; 
                float s = 1f - Mathf.Exp(-followRate * dt);
                vcx += (vx - vcx) * s;
                vcy += (vy - vcy) * s;

                // integrate with accel + gravity + damping
                float ax = _ax[phys] + gravity.X;
                float ay = _ay[phys] + gravity.Y;

                vcx = (vcx + ax * dt) * dampFactor;
                vcy = (vcy + ay * dt) * dampFactor;

                float px = _px[phys] + vcx * dt;
                float py = _py[phys] + vcy * dt;

                _vx[phys] = vcx; _vy[phys] = vcy;
                _px[phys] = px;  _py[phys] = py;

                // Size from base * curve
                float sx = Math.Max(0f, _sx0[phys] * sizeScale);
                float sy = Math.Max(0f, _sy0[phys] * sizeScale);
                _sx[phys] = sx; _sy[phys] = sy;

                // Alpha from base * curve
                var c = _color[phys];
                byte aByte = (byte)Math.Clamp(_alpha0[phys] * alphaScale * 255f, 0f, 255f);
                _color[phys] = new Color(c.R, c.G, c.B, aByte);

                // Expand AABB (pad by size)
                float halfX = Math.Max(MinExtent, sx * 0.5f) + AabbPad;
                float halfY = Math.Max(MinExtent, sy * 0.5f) + AabbPad;

                if (px - halfX < minX) minX = px - halfX;
                if (py - halfY < minY) minY = py - halfY;
                if (px + halfX > maxX) maxX = px + halfX;
                if (py + halfY > maxY) maxY = py + halfY;

                i++;
            }

            if (_alive == 0)
            {
                _aabb = Rectangle.Empty;
            }
            else
            {
                float ox = 0f, oy = 0f;
                if (UseLocalSpace && Transform is not null)
                {
                    var p = Transform.WorldPosToVec2;
                    ox = p.X; oy = p.Y;
                }

                int x = (int)Mathf.Floor(minX + ox);
                int y = (int)Mathf.Floor(minY + oy);
                int w = (int)Mathf.Max(1, Mathf.Ceil(maxX - minX));
                int h = (int)Mathf.Max(1, Mathf.Ceil(maxY - minY));
                _aabb = new Rectangle(x, y, w, h);
            }
        }

        // ------------------- Spawn helpers -------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector2 SpawnPointBase()
        {
            Vector2 origin = Transform?.WorldPosition.ToVector2() ?? Vector2.Zero;

            switch (_config.SpawnType)
            {
                case ParticleSpawnType.Point:
                    return origin;

                case ParticleSpawnType.Circular:
                {
                    // Use PositionJitter as radius extents; choose random point in a circle/ellipse.
                    // If Min/Max differ, treat as ellipse radii ranges.
                    var rMin = _config.PositionJitter.Min;
                    var rMax = _config.PositionJitter.Max;

                    float rx = RandRange(rMin.X, rMax.X);
                    float ry = RandRange(rMin.Y, rMax.Y);

                    // uniform in disk via sqrt for radius
                    float ang = RandRange(0f, MathHelper.TwoPi);
                    float mag = (float)Math.Sqrt(_rng.NextDouble());

                    var offset = new Vector2((float)Math.Cos(ang) * rx * mag,
                                             (float)Math.Sin(ang) * ry * mag);
                    return origin + offset;
                }

                default:
                    return origin;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector2 RandomUnitDirection2D()
        {
            // Low-branch random unit vector
            // Use golden angle-based hash for better distribution if desired; here: simple uniform angle
            float ang = (float)(RandomShared.NextDoubleStatic() * MathHelper.TwoPi);
            return new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));
        }

        // ------------------- Burst scheduling -------------------

        private void SetupBurstSchedule()
        {
            if (_config.EmissionMode != EmissionMode.Burst || _config.Bursts.Count == 0)
            {
                _burstCounters = Array.Empty<int>();
                _burstNextTimes = Array.Empty<double>();
                _nextBurstWindowStartSec = 0.0;
                _burstCycle = 0;
                return;
            }

            _burstCounters = new int[_config.Bursts.Count];
            _burstNextTimes = new double[_config.Bursts.Count];
            _nextBurstWindowStartSec = 0.0;
            _burstCycle = 0;
            ResetBurstCycleState();
        }

        private void ResetBurstCycleState()
        {
            for (int i = 0; i < _config.Bursts.Count; i++)
            {
                _burstCounters[i] = 0;
                var b = _config.Bursts[i];
                _burstNextTimes[i] = _nextBurstWindowStartSec + Math.Max(0.0f, b.Time);
            }
        }

        private bool HasMoreCycles()
        {
            bool any = false;
            for (int i = 0; i < _config.Bursts.Count; i++)
            {
                var b = _config.Bursts[i];
                if (b.Cycles <= 0 || _burstCycle < b.Cycles) { any = true; break; }
            }
            return any;
        }

        // ------------------- Alive set ops -------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void KillAt(int logicalIdx)
        {
            int last = _alive - 1;
            int deadPhys = _indices[logicalIdx];

            int movedPhys = _indices[last];
            _indices[logicalIdx] = movedPhys;
            _indices[last] = deadPhys;

            _rev[movedPhys] = logicalIdx;
            _rev[deadPhys] = last;

            _alive = last;
            Version++;
        }

        // ------------------- Rand helpers -------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float RandRange(float min, float max)
        {
            var t = (float)_rng.NextDouble();
            return min + (max - min) * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector2 RandRange2(in Vector2 min, in Vector2 max)
        {
            float tx = (float)_rng.NextDouble();
            float ty = (float)_rng.NextDouble();
            return new Vector2(min.X + (max.X - min.X) * tx,
                               min.Y + (max.Y - min.Y) * ty);
        }

        private static class RandomShared
        {
            private static readonly Random _r = new();
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double NextDoubleStatic() => _r.NextDouble();
        }

        // ------------------- Read-only view -------------------

        public readonly ref struct ReadOnlyParticles
        {
            public readonly ReadOnlySpan<int> INDICES;
            public readonly ReadOnlySpan<float> PX, PY, VX, VY, SX, SY, ROT, LIFE, MAXLIFE, SPIN, AX, AY;
            public readonly ReadOnlySpan<Color> COLOR;
            public readonly int Alive;
            public ReadOnlyParticles(
                int[] indices,
                float[] px, float[] py, float[] vx, float[] vy, float[] sx, float[] sy, float[] rot,
                float[] life, float[] maxLife, Color[] color, float[] spin, float[] ax, float[] ay, int alive)
            {
                INDICES = indices;
                PX = px; PY = py; VX = vx; VY = vy; SX = sx; SY = sy; ROT = rot;
                LIFE = life; MAXLIFE = maxLife; COLOR = color; SPIN = spin; AX = ax; AY = ay; Alive = alive;
            }
        }
    }