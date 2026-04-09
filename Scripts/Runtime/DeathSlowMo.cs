using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Slow-motion effect when a player dies.
    /// Reduces Engine.TimeScale to 0.3x for 1.5 real seconds on death.
    /// </summary>
    public partial class DeathSlowMo : Node
    {
        private GameState _state;
        private float _slowMoTimer;
        private bool[] _wasAlive;

        public void Init(GameState state)
        {
            _state = state;
            _wasAlive = new bool[state.Players.Length];
            for (int i = 0; i < state.Players.Length; i++)
                _wasAlive[i] = !state.Players[i].IsDead;
        }

        public override void _Process(double delta)
        {
            if (_state == null) return;

            // Detect new deaths
            for (int i = 0; i < _state.Players.Length; i++)
            {
                if (_wasAlive[i] && _state.Players[i].IsDead)
                {
                    _slowMoTimer = 1.5f;
                    Engine.TimeScale = 0.3;
                }
                _wasAlive[i] = !_state.Players[i].IsDead;
            }

            if (_slowMoTimer > 0f)
            {
                // Count down in real time (delta is already scaled, so divide by TimeScale)
                _slowMoTimer -= (float)(delta / Engine.TimeScale);
                if (_slowMoTimer <= 0f)
                    Engine.TimeScale = 1.0;
            }
        }
    }
}
