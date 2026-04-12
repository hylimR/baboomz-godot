using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Renders Headhunter mode tokens in world space and shows a HUD
    /// counter for collected tokens per player. Token pickups drawn as
    /// golden circles at their map positions.
    /// </summary>
    public partial class HeadhunterTokenRenderer : Node2D
    {
        private GameState _state;
        private bool _active;

        public void Init(GameState state)
        {
            _state = state;
            ProcessPriority = 50;
            ZIndex = 13;
            _active = state?.Config != null
                      && state.Config.MatchType == MatchType.Headhunter;
            Visible = _active;
        }

        public override void _Process(double delta)
        {
            if (!_active) return;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (!_active || _state == null) return;
            ref var hh = ref _state.Headhunter;

            // Draw dropped token pickups as golden circles
            if (hh.Tokens != null)
            {
                for (int t = 0; t < hh.TokenCount; t++)
                {
                    if (!hh.Tokens[t].Active) continue;
                    Vector2 pos = hh.Tokens[t].Position.ToGodot();

                    // Golden circle with glow
                    float pulse = 0.8f + 0.2f * Mathf.Sin(_state.Time * 4f + t);
                    var gold = new Color(1f, 0.84f, 0f, pulse);
                    DrawCircle(pos, 0.6f, gold);
                    DrawArc(pos, 0.8f, 0f, Mathf.Tau, 24,
                        new Color(1f, 0.9f, 0.3f, 0.4f), 0.08f);
                }
            }

            // Draw token count above each player
            if (hh.TokensCollected != null)
            {
                var font = ThemeDB.FallbackFont;
                for (int p = 0; p < _state.Players.Length; p++)
                {
                    if (_state.Players[p].IsDead || _state.Players[p].IsMob) continue;
                    int tokens = hh.TokensCollected[p];
                    if (tokens <= 0) continue;

                    Vector2 playerPos = _state.Players[p].Position.ToGodot();
                    Vector2 textPos = playerPos + new Vector2(-8f, -30f);

                    if (font != null)
                    {
                        Color textColor = p == 0
                            ? new Color(0.3f, 0.55f, 1f)
                            : new Color(1f, 0.3f, 0.3f);
                        DrawString(font, textPos, $"{tokens}",
                            HorizontalAlignment.Center, -1, 16, textColor);
                    }
                }
            }
        }
    }
}
