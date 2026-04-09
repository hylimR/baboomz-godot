using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Camera2D that follows the human player, tracks projectiles in flight,
    /// and supports screen shake. Attaches as a child of GameRunner.
    /// </summary>
    public partial class CameraTracker : Camera2D
    {
        private GameState _state;
        private RandomNumberGenerator _rng = new();

        // Shake
        private float _shakeIntensity;
        private float _shakeDuration;

        // Tracking
        private int _trackingProjectileId = -1;

        public void Init(GameState state)
        {
            _state = state;
            PositionSmoothingEnabled = true;
            PositionSmoothingSpeed = 4.0f;
            Zoom = new Vector2(0.8f, 0.8f);
            MakeCurrent();
            ProcessPriority = 60; // After renderers
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;

            Vector2 target = Vector2.Zero;
            bool foundTarget = false;

            // Track active projectile if we have one
            if (_trackingProjectileId >= 0)
            {
                foreach (var proj in _state.Projectiles)
                {
                    if (proj.Id == _trackingProjectileId && proj.Alive)
                    {
                        target = proj.Position.ToGodot();
                        foundTarget = true;
                        break;
                    }
                }
                if (!foundTarget) _trackingProjectileId = -1;
            }

            // Check for newly fired projectiles from human player
            if (_trackingProjectileId < 0)
            {
                foreach (var proj in _state.Projectiles)
                {
                    if (proj.Alive && proj.OwnerIndex == 0)
                    {
                        _trackingProjectileId = proj.Id;
                        target = proj.Position.ToGodot();
                        foundTarget = true;
                        break;
                    }
                }
            }

            // Default: follow player 0
            if (!foundTarget)
            {
                ref var p = ref _state.Players[0];
                if (!p.IsDead)
                    target = p.Position.ToGodot();
            }

            GlobalPosition = target;

            // Screen shake
            if (_shakeDuration > 0)
            {
                _shakeDuration -= (float)delta;
                Offset = new Vector2(
                    _rng.RandfRange(-_shakeIntensity, _shakeIntensity),
                    _rng.RandfRange(-_shakeIntensity, _shakeIntensity));
            }
            else
            {
                Offset = Vector2.Zero;
            }
        }

        public void Shake(float intensity, float duration)
        {
            _shakeIntensity = intensity;
            _shakeDuration = duration;
        }
    }
}
