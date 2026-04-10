using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Full-screen red vignette that pulses when the local player drops below
    /// LowHealthFraction of max HP. Disappears on heal/respawn or match end.
    /// Lives on a CanvasLayer above the game world but below the HUD.
    /// </summary>
    public partial class LowHealthOverlay : CanvasLayer
    {
        private const float LowHealthFraction = 0.25f;

        private GameState _state;
        private ColorRect _rect;

        public void Init(GameState state)
        {
            _state = state;
            // Layer just below the HUD layer (HUD = 10).
            Layer = 9;
            ProcessPriority = 60;

            _rect = new ColorRect();
            _rect.Name = "LowHpRect";
            _rect.Color = new Color(0.85f, 0.05f, 0.05f, 0f);
            _rect.MouseFilter = Control.MouseFilterEnum.Ignore;
            _rect.AnchorLeft = 0f;
            _rect.AnchorTop = 0f;
            _rect.AnchorRight = 1f;
            _rect.AnchorBottom = 1f;
            AddChild(_rect);
        }

        public override void _Process(double delta)
        {
            if (_state == null || _rect == null) return;
            if (_state.Players == null || _state.Players.Length == 0)
            {
                SetAlpha(0f);
                return;
            }

            ref var p = ref _state.Players[0];
            if (p.IsDead || p.MaxHealth <= 0f || _state.Phase != MatchPhase.Playing)
            {
                SetAlpha(0f);
                return;
            }

            float hpFrac = p.Health / p.MaxHealth;
            if (hpFrac >= LowHealthFraction)
            {
                SetAlpha(0f);
                return;
            }

            // Stronger pulse the lower the HP. Range: ~0.15..0.55 alpha.
            float severity = 1f - (hpFrac / LowHealthFraction);
            float pulse = (Mathf.Sin(_state.Time * 6f) + 1f) * 0.5f;
            float alpha = 0.15f + 0.4f * Mathf.Clamp(severity, 0f, 1f) * pulse;
            SetAlpha(alpha);
        }

        private void SetAlpha(float alpha)
        {
            var c = _rect.Color;
            _rect.Color = new Color(c.R, c.G, c.B, alpha);
        }
    }
}
