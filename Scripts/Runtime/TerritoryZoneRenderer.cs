using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Renders Territories capture zones — colored circles at each zone
    /// position with owner tint and contested pulse animation.
    /// Extends the KothZoneRenderer pattern to multiple zones.
    /// </summary>
    public partial class TerritoryZoneRenderer : Node2D
    {
        private GameState _state;
        private bool _active;

        public void Init(GameState state)
        {
            _state = state;
            ProcessPriority = 50;
            ZIndex = 11;
            _active = state?.Config != null
                      && state.Config.MatchType == MatchType.Territories;
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
            ref var territory = ref _state.Territory;
            if (territory.ZonePositions == null) return;

            float radius = _state.Config.TerritoryZoneRadius;

            for (int z = 0; z < territory.ZonePositions.Length; z++)
            {
                Vector2 center = territory.ZonePositions[z].ToGodot();
                int owner = territory.ZoneOwner[z];
                bool contested = territory.ZoneContested[z];

                Color baseTint = TeamColor(owner);

                if (contested)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(_state.Time * 6f);
                    baseTint = baseTint.Lerp(new Color(1f, 0.85f, 0.2f), pulse * 0.7f);
                }

                var fill = new Color(baseTint.R, baseTint.G, baseTint.B, 0.15f);
                DrawCircle(center, radius, fill);

                var rim = new Color(baseTint.R, baseTint.G, baseTint.B, 0.7f);
                DrawArc(center, radius * 0.97f, 0f, Mathf.Tau, 48, rim, 0.15f);

                // Zone label (A, B, C)
                char label = (char)('A' + z);
                var font = ThemeDB.FallbackFont;
                if (font != null)
                {
                    DrawString(font, center + new Vector2(-6f, -radius - 8f),
                        label.ToString(), HorizontalAlignment.Center, -1, 18, baseTint);
                }
            }
        }

        private static Color TeamColor(int index) => index switch
        {
            0 => new Color(0.30f, 0.55f, 1.00f),
            1 => new Color(1.00f, 0.30f, 0.30f),
            2 => new Color(0.30f, 0.85f, 0.40f),
            3 => new Color(1.00f, 0.85f, 0.30f),
            _ => new Color(0.5f, 0.5f, 0.5f, 0.4f), // neutral
        };
    }
}
