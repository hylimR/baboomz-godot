using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Spawns biome-colored debris chunks on every explosion. Uses a fixed-size pool
    /// of 40 Sprite2D nodes; recycles the oldest slot when full so there are no leaks.
    /// Equivalent of Unity's TerrainDebrisRenderer.
    /// </summary>
    public partial class TerrainDebrisRenderer : Node2D
    {
        private const int PoolSize = 40;
        private const int ChunksMin = 6;
        private const int ChunksMax = 10;

        private GameState _state;
        private readonly DebrisChunk[] _pool = new DebrisChunk[PoolSize];
        private int _nextSlot;
        private RandomNumberGenerator _rng;

        public void Init(GameState state)
        {
            _state = state;
            ProcessPriority = 55; // just after renderers, before camera
            ZIndex = 15;         // above terrain (0) + players, below explosions (20)
            _rng = new RandomNumberGenerator();
            _rng.Seed = (ulong)state.Seed ^ 0xDEB715;

            for (int i = 0; i < PoolSize; i++)
            {
                var chunk = new DebrisChunk();
                chunk.Visible = false;
                AddChild(chunk);
                _pool[i] = chunk;
            }
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;

            float dt = (float)delta;

            // Tick every chunk for gravity + fade.
            float gravity = _state.Config?.Gravity ?? 9.81f;
            for (int i = 0; i < PoolSize; i++)
                _pool[i].Tick(dt, gravity);

            // Spawn new chunks for explosions that occurred this frame.
            var biome = _state.Biome;
            foreach (var evt in _state.ExplosionEvents)
            {
                int count = Mathf.Clamp((int)(evt.Radius * 2f), ChunksMin, ChunksMax);
                for (int i = 0; i < count; i++)
                    SpawnChunk(evt.Position.ToGodot(), biome);
            }
        }

        private void SpawnChunk(Vector2 pos, TerrainBiome biome)
        {
            var chunk = _pool[_nextSlot];
            _nextSlot = (_nextSlot + 1) % PoolSize;

            // 70% earth / 30% surface, with +/- 5% RGB variation.
            bool earth = _rng.Randf() < 0.7f;
            float r = earth ? biome.EarthR : biome.SurfaceR;
            float g = earth ? biome.EarthG : biome.SurfaceG;
            float b = earth ? biome.EarthB : biome.SurfaceB;
            r = Mathf.Clamp(r + _rng.RandfRange(-0.05f, 0.05f), 0f, 1f);
            g = Mathf.Clamp(g + _rng.RandfRange(-0.05f, 0.05f), 0f, 1f);
            b = Mathf.Clamp(b + _rng.RandfRange(-0.05f, 0.05f), 0f, 1f);

            // Velocity: 60% upward-biased (30-150 deg in Y-up space), 40% full random.
            float angleDeg = _rng.Randf() < 0.6f
                ? _rng.RandfRange(30f, 150f)
                : _rng.RandfRange(0f, 360f);
            float speed = _rng.RandfRange(4f, 8f);
            float angleRad = Mathf.DegToRad(angleDeg);
            // Y-up velocity -> Godot Y-down by negating Y component.
            var velocity = new Vector2(
                Mathf.Cos(angleRad) * speed,
                -Mathf.Sin(angleRad) * speed);

            float size = _rng.RandfRange(0.1f, 0.3f);
            float lifetime = _rng.RandfRange(1.5f, 2.5f);

            chunk.Launch(pos, velocity, new Color(r, g, b, 1f), size, lifetime);
        }
    }

    /// <summary>Single pooled debris sprite. Does its own gravity + fade update.</summary>
    public partial class DebrisChunk : Sprite2D
    {
        private const float FadeTime = 0.5f;

        private Vector2 _velocity;
        private float _timeRemaining;
        private float _totalLifetime;
        private Color _baseColor;

        public override void _Ready()
        {
            Texture = ProceduralSprites.WhitePixel;
            Centered = true;
        }

        public void Launch(Vector2 pos, Vector2 velocity, Color color, float size, float lifetime)
        {
            GlobalPosition = pos;
            _velocity = velocity;
            _baseColor = color;
            _totalLifetime = lifetime;
            _timeRemaining = lifetime;
            // White pixel is 1x1; scale to produce desired world-unit chunk size.
            Scale = new Vector2(size, size);
            SelfModulate = color;
            Visible = true;
        }

        public void Tick(float dt, float gravity)
        {
            if (!Visible) return;

            _timeRemaining -= dt;
            if (_timeRemaining <= 0f)
            {
                Visible = false;
                return;
            }

            // Simulation Y-up: positive Y = up. Godot Y-down: velocity Y is already negated
            // in Launch(), so gravity pulls DOWN in screen space => +Y in Godot velocity.
            _velocity.Y += gravity * dt;
            GlobalPosition += _velocity * dt;

            // Fade out during final FadeTime seconds.
            if (_timeRemaining < FadeTime)
            {
                float alpha = _timeRemaining / FadeTime;
                SelfModulate = new Color(_baseColor.R, _baseColor.G, _baseColor.B, alpha);
            }
        }
    }
}
