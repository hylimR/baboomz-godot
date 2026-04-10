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
        private static Texture2D[] _frames;
        private static bool _framesLoaded;

        public void Init(GameState state, CameraTracker camera)
        {
            _state = state;
            _camera = camera;
            ProcessPriority = 70; // After other renderers

            LoadFrames();
        }

        private static void LoadFrames()
        {
            if (_framesLoaded) return;
            _framesLoaded = true;

            var list = new System.Collections.Generic.List<Texture2D>();
            for (int i = 1; i <= 6; i++)
            {
                var tex = SpriteLoader.Load($"VFX/Default/explosion_0{i}");
                if (tex == null) break;
                list.Add(tex);
            }
            _frames = list.Count > 0 ? list.ToArray() : null;
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
            fx.Frames = _frames;
            GetTree().Root.AddChild(fx);
        }
    }

    /// <summary>
    /// A single explosion: plays a 6-frame sprite sequence at 12 FPS,
    /// or falls back to expanding-circle draw calls when no frames loaded.
    /// </summary>
    public partial class ExplosionFX : Node2D
    {
        public float SimRadius = 2f;
        public Texture2D[] Frames;

        private const float FPS = 12f;
        private const float FrameDuration = 1f / FPS;
        private const float FallbackLifetime = 0.4f;

        private Sprite2D _sprite;
        private int _frameIndex;
        private float _frameTimer;
        private float _elapsed;
        private float _maxPixelRadius;

        public override void _Ready()
        {
            _maxPixelRadius = SimRadius * 25f;
            ZIndex = 20;

            if (Frames != null && Frames.Length > 0)
            {
                _sprite = new Sprite2D();
                _sprite.Texture = Frames[0];

                // Scale to match explosion radius: fit sprite diameter to pixel radius * 2.
                float srcWidth = Frames[0].GetWidth();
                if (srcWidth > 0f)
                {
                    float s = (_maxPixelRadius * 2f) / srcWidth;
                    _sprite.Scale = new Vector2(s, s);
                }
                AddChild(_sprite);
            }
        }

        public override void _Process(double delta)
        {
            float dt = (float)delta;
            _elapsed += dt;

            if (_sprite != null)
            {
                _frameTimer += dt;
                if (_frameTimer >= FrameDuration)
                {
                    _frameTimer -= FrameDuration;
                    _frameIndex++;
                    if (_frameIndex >= Frames.Length)
                    {
                        QueueFree();
                        return;
                    }
                    _sprite.Texture = Frames[_frameIndex];
                }
            }
            else
            {
                // Procedural fallback
                if (_elapsed >= FallbackLifetime)
                {
                    QueueFree();
                    return;
                }
                QueueRedraw();
            }
        }

        public override void _Draw()
        {
            if (_sprite != null) return;

            float t = _elapsed / FallbackLifetime;
            float currentRadius = _maxPixelRadius * Mathf.Sqrt(t);
            float alpha = 1f - t;

            var innerColor = new Color(1f, 1f, 0.8f, alpha * 0.8f);
            DrawCircle(Vector2.Zero, currentRadius * 0.6f, innerColor);

            var outerColor = new Color(1f, 0.4f, 0f, alpha * 0.5f);
            DrawCircle(Vector2.Zero, currentRadius, outerColor);

            var ringColor = new Color(0.5f, 0.2f, 0f, alpha * 0.3f);
            DrawArc(Vector2.Zero, currentRadius * 1.2f, 0f, Mathf.Tau, 32, ringColor, 3f);
        }
    }
}
