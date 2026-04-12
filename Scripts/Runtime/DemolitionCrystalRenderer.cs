using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Renders Demolition mode crystals — large colored diamonds at crystal
    /// positions with HP bars. Damage flash on CrystalDamageEvents.
    /// </summary>
    public partial class DemolitionCrystalRenderer : Node2D
    {
        private GameState _state;
        private bool _active;
        private float _flashTimer;

        public void Init(GameState state)
        {
            _state = state;
            ProcessPriority = 50;
            ZIndex = 12;
            _active = state?.Config != null
                      && state.Config.MatchType == MatchType.Demolition
                      && state.Demolition.Crystals != null;
            Visible = _active;
        }

        public override void _Process(double delta)
        {
            if (!_active) return;

            // Flash on crystal damage events
            if (_state.CrystalDamageEvents.Count > 0)
                _flashTimer = 0.2f;
            if (_flashTimer > 0f)
                _flashTimer -= (float)delta;

            QueueRedraw();
        }

        public override void _Draw()
        {
            if (!_active || _state == null) return;
            ref var demo = ref _state.Demolition;

            for (int c = 0; c < demo.Crystals.Length; c++)
            {
                ref var crystal = ref demo.Crystals[c];
                Vector2 pos = crystal.Position.ToGodot();
                Color tint = c == 0
                    ? new Color(0.3f, 0.55f, 1f)  // blue
                    : new Color(1f, 0.3f, 0.3f);   // red

                // Crystal body (diamond shape)
                float size = 1.5f;
                var points = new Vector2[]
                {
                    pos + new Vector2(0f, -size * 1.5f),  // top
                    pos + new Vector2(size, 0f),            // right
                    pos + new Vector2(0f, size * 0.8f),     // bottom
                    pos + new Vector2(-size, 0f)            // left
                };

                var fillColor = new Color(tint.R, tint.G, tint.B, 0.6f);
                if (_flashTimer > 0f)
                    fillColor = fillColor.Lerp(Colors.White, 0.5f);

                DrawColoredPolygon(points, fillColor);

                // Crystal outline
                var outlineColor = new Color(tint.R, tint.G, tint.B, 0.9f);
                for (int i = 0; i < points.Length; i++)
                {
                    int next = (i + 1) % points.Length;
                    DrawLine(points[i], points[next], outlineColor, 0.12f);
                }

                // HP bar above crystal
                float hpRatio = crystal.MaxHP > 0f ? crystal.HP / crystal.MaxHP : 0f;
                float barW = 3f;
                float barH = 0.3f;
                Vector2 barPos = pos + new Vector2(-barW / 2f, -size * 2f);

                DrawRect(new Rect2(barPos, new Vector2(barW, barH)),
                    new Color(0.2f, 0.2f, 0.2f, 0.8f));
                DrawRect(new Rect2(barPos, new Vector2(barW * hpRatio, barH)),
                    new Color(tint.R, tint.G, tint.B, 0.9f));
            }
        }
    }
}
