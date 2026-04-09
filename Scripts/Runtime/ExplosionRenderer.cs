using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Watches for explosion events in GameState and spawns visual effects.
    /// Also triggers camera shake via CameraTracker.
    /// </summary>
    public partial class ExplosionRenderer : Node2D
    {
        private GameState _state;
        private CameraTracker _camera;

        public void Init(GameState state, CameraTracker camera)
        {
            _state = state;
            _camera = camera;
            ProcessPriority = 70; // After other renderers
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;

            foreach (var evt in _state.ExplosionEvents)
            {
                SpawnExplosion(evt.Position.ToGodot(), evt.Radius);
                _camera?.Shake(evt.Radius * 0.5f, 0.3f);
            }
        }

        private void SpawnExplosion(Vector2 pos, float simRadius)
        {
            var fx = new ExplosionFX();
            fx.GlobalPosition = pos;
            fx.SimRadius = simRadius;
            GetTree().Root.AddChild(fx);
        }
    }

    /// <summary>
    /// A single explosion effect: expanding circles that fade out.
    /// </summary>
    public partial class ExplosionFX : Node2D
    {
        public float SimRadius = 2f;
        private const float Lifetime = 0.4f;
        private float _elapsed;
        private float _maxPixelRadius;

        public override void _Ready()
        {
            // Convert sim radius to pixel radius with visual exaggeration
            _maxPixelRadius = SimRadius * 25f;
            ZIndex = 20; // Above everything
        }

        public override void _Process(double delta)
        {
            _elapsed += (float)delta;
            if (_elapsed >= Lifetime)
            {
                QueueFree();
                return;
            }
            QueueRedraw();
        }

        public override void _Draw()
        {
            float t = _elapsed / Lifetime;

            // Fast expand, slow end
            float currentRadius = _maxPixelRadius * Mathf.Sqrt(t);
            float alpha = 1f - t;

            // Inner flash (white-yellow)
            var innerColor = new Color(1f, 1f, 0.8f, alpha * 0.8f);
            DrawCircle(Vector2.Zero, currentRadius * 0.6f, innerColor);

            // Outer fireball (orange-red)
            var outerColor = new Color(1f, 0.4f, 0f, alpha * 0.5f);
            DrawCircle(Vector2.Zero, currentRadius, outerColor);

            // Expanding ring outline
            var ringColor = new Color(0.5f, 0.2f, 0f, alpha * 0.3f);
            DrawArc(Vector2.Zero, currentRadius * 1.2f, 0f, Mathf.Tau, 32, ringColor, 3f);
        }
    }
}
