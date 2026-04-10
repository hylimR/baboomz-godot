using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Watches state.DamageEvents each tick and spawns short-lived hit markers
    /// at the impact position. Each marker is a white-to-red cross + floating
    /// damage number that fades in ~0.5 s. Drawn in world space.
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
            ZIndex = 18;

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
        private const float Lifetime = 0.5f;
        private const float RiseSpeed = 4f; // world units / sec

        private float _timeRemaining;
        private float _amount;
        private Font _font;

        public override void _Ready()
        {
            ZIndex = 18;
            // ThemeDB provides a default font we can reuse for DrawString.
            _font = ThemeDB.FallbackFont;
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

            // White-to-red interpolation: bigger hits are redder.
            float redness = Mathf.Clamp(_amount / 50f, 0f, 1f);
            var crossColor = new Color(1f, 1f - redness * 0.5f, 1f - redness * 0.8f, alpha);

            // Cross (4 short lines from center).
            float radius = 0.6f * (1f - t * 0.4f); // shrinks slightly
            DrawLine(new Vector2(-radius, -radius), new Vector2(radius, radius), crossColor, 0.18f);
            DrawLine(new Vector2(-radius, radius),  new Vector2(radius, -radius), crossColor, 0.18f);

            // Damage number (issue #36): floating label to the upper-right of the cross.
            // Uses a DrawString pass rather than a child Label so the hit marker pool
            // stays cheap — we just redraw per frame.
            if (_font != null && _amount > 0f)
            {
                int damageInt = Mathf.RoundToInt(_amount);
                string text = damageInt.ToString();

                // Critical hits (>=40) show in bold red; normal hits in yellow-white.
                bool critical = _amount >= 40f;
                Color textColor = critical
                    ? new Color(1f, 0.25f, 0.15f, alpha)
                    : new Color(1f, 0.95f, 0.4f, alpha);
                Color outlineColor = new Color(0f, 0f, 0f, alpha * 0.9f);

                int fontSize = critical ? 18 : 14;
                Vector2 textPos = new Vector2(radius + 0.15f, -radius - 0.1f);

                // Outline: draw 4 offset copies behind for readability against any background.
                float o = 0.06f;
                DrawString(_font, textPos + new Vector2(-o, 0),  text, HorizontalAlignment.Left, -1f, fontSize, outlineColor);
                DrawString(_font, textPos + new Vector2( o, 0),  text, HorizontalAlignment.Left, -1f, fontSize, outlineColor);
                DrawString(_font, textPos + new Vector2(0, -o),  text, HorizontalAlignment.Left, -1f, fontSize, outlineColor);
                DrawString(_font, textPos + new Vector2(0,  o),  text, HorizontalAlignment.Left, -1f, fontSize, outlineColor);
                DrawString(_font, textPos, text, HorizontalAlignment.Left, -1f, fontSize, textColor);
            }
        }
    }
}
