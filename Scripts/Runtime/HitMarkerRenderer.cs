using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Watches state.DamageEvents each tick and spawns short-lived hit markers
    /// at the impact position. Each marker is a white-to-red cross that fades in
    /// ~0.3 s. Drawn in world space; safe to add unconditionally.
    /// </summary>
    public partial class HitMarkerRenderer : Node2D
    {
        private const int PoolSize = 32;

        private GameState _state;
        private readonly HitMarker[] _pool = new HitMarker[PoolSize];
        private int _next;

        public void Init(GameState state)
        {
            _state = state;
            ProcessPriority = 55;
            ZIndex = 18; // above explosions (20)? actually slightly lower so explosions cover

            for (int i = 0; i < PoolSize; i++)
            {
                var marker = new HitMarker();
                marker.Visible = false;
                AddChild(marker);
                _pool[i] = marker;
            }
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;

            float dt = (float)delta;
            for (int i = 0; i < PoolSize; i++)
                _pool[i].Tick(dt);

            foreach (var evt in _state.DamageEvents)
            {
                if (evt.Amount <= 0f) continue;
                Spawn(evt.Position.ToGodot(), evt.Amount);
            }
        }

        private void Spawn(Vector2 pos, float amount)
        {
            var marker = _pool[_next];
            _next = (_next + 1) % PoolSize;
            marker.Launch(pos, amount);
        }
    }

    /// <summary>Single hit marker: rotating cross + rising damage number.</summary>
    public partial class HitMarker : Node2D
    {
        private const float Lifetime = 0.3f;
        private const float RiseSpeed = 4f; // world units / sec

        private float _timeRemaining;
        private float _amount;

        public override void _Ready()
        {
            ZIndex = 18;
        }

        public void Launch(Vector2 pos, float amount)
        {
            GlobalPosition = pos;
            _amount = amount;
            _timeRemaining = Lifetime;
            Visible = true;
            QueueRedraw();
        }

        public void Tick(float dt)
        {
            if (!Visible) return;
            _timeRemaining -= dt;
            if (_timeRemaining <= 0f)
            {
                Visible = false;
                return;
            }
            // Rise upward in screen space (Godot Y-down -> negative Y).
            Position = new Vector2(Position.X, Position.Y - RiseSpeed * dt);
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (!Visible) return;
            float t = _timeRemaining / Lifetime;
            float alpha = Mathf.Clamp(t, 0f, 1f);

            // Cross (4 short lines from center).
            float radius = 0.6f * (1f - t * 0.4f); // shrinks slightly
            // White-to-red interpolation: bigger hits are redder.
            float redness = Mathf.Clamp(_amount / 50f, 0f, 1f);
            var color = new Color(1f, 1f - redness * 0.5f, 1f - redness * 0.8f, alpha);

            DrawLine(new Vector2(-radius, -radius), new Vector2(radius, radius), color, 0.18f);
            DrawLine(new Vector2(-radius, radius),  new Vector2(radius, -radius), color, 0.18f);
        }
    }
}
