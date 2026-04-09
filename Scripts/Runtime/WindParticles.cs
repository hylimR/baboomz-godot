using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>Drifting particles that visualize wind direction and strength.</summary>
    public partial class WindParticles : Node2D
    {
        private GameState _state;
        private CpuParticles2D _particles;

        public void Init(GameState state)
        {
            _state = state;

            _particles = new CpuParticles2D();
            _particles.Emitting = true;
            _particles.Amount = 30;
            _particles.Lifetime = 3f;
            _particles.Spread = 10f;
            _particles.InitialVelocityMin = 20f;
            _particles.InitialVelocityMax = 40f;
            _particles.ScaleAmountMin = 1f;
            _particles.ScaleAmountMax = 3f;
            _particles.Color = new Color(1f, 1f, 1f, 0.15f);
            _particles.EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle;
            _particles.EmissionRectExtents = new Vector2(1000, 500);
            _particles.ZIndex = -5;
            AddChild(_particles);

            ProcessPriority = 30;
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;

            // Wind is WindForce (magnitude) and WindAngle (degrees)
            float force = _state.WindForce;
            if (Mathf.Abs(force) < 0.01f)
            {
                _particles.InitialVelocityMin = 5f;
                _particles.InitialVelocityMax = 10f;
                return;
            }

            // WindAngle is in degrees for HUD; convert to radians
            float angleRad = Mathf.DegToRad(_state.WindAngle);
            // In Godot's coordinate system (Y-down), use angle directly
            _particles.Direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
            _particles.InitialVelocityMin = Mathf.Abs(force) * 5f;
            _particles.InitialVelocityMax = Mathf.Abs(force) * 10f;
        }
    }
}
