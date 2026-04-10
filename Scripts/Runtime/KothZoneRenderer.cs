using Godot;
using Baboomz.Simulation;

namespace Baboomz
{
    /// <summary>
    /// Renders the King of the Hill capture zone in world space:
    ///   - filled disc at state.Koth.ZonePosition with the owner's tint
    ///   - outline ring with a brighter rim
    ///   - radial progress arc showing leading score (0..1 of points-to-win)
    ///   - contested state pulses between the two leading players' colors
    /// Spawns nothing for non-KOTH matches; safe to add unconditionally.
    /// </summary>
    public partial class KothZoneRenderer : Node2D
    {
        private GameState _state;
        private bool _active;

        public void Init(GameState state)
        {
            _state = state;
            ProcessPriority = 50;
            ZIndex = 11; // above terrain (0), below CTF flags / explosions
            _active = state?.Config != null
                      && state.Config.MatchType == MatchType.KingOfTheHill;
            // Hide entirely when inactive so it draws nothing.
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

            ref var koth = ref _state.Koth;
            Vector2 center = koth.ZonePosition.ToGodot();
            float radius = koth.ZoneRadius;

            int leader = ComputeLeader(koth.Scores);
            float progress = ComputeProgress(koth.Scores, leader);
            Color baseTint = TeamColor(leader);

            if (koth.IsContested)
            {
                // Pulse between yellow and the leader's color when contested.
                float pulse = 0.5f + 0.5f * Mathf.Sin(_state.Time * 6f);
                baseTint = baseTint.Lerp(new Color(1f, 0.85f, 0.2f), pulse * 0.7f);
            }

            // Filled disc with low alpha so the terrain is visible underneath.
            var fill = new Color(baseTint.R, baseTint.G, baseTint.B, 0.18f);
            DrawCircle(center, radius, fill);

            // Outline ring (slightly inside the radius so it doesn't bleed past).
            var rim = new Color(baseTint.R, baseTint.G, baseTint.B, 0.85f);
            DrawArc(center, radius * 0.97f, 0f, Mathf.Tau, 48, rim, 0.18f);

            // Radial progress arc — sweeps clockwise from the top to show how
            // close the leading player is to the points-to-win threshold.
            if (progress > 0f && leader >= 0)
            {
                var prog = new Color(1f, 1f, 1f, 0.9f);
                float startAngle = -Mathf.Pi * 0.5f;
                float endAngle = startAngle + Mathf.Tau * Mathf.Clamp(progress, 0f, 1f);
                DrawArc(center, radius * 1.08f, startAngle, endAngle, 48, prog, 0.22f);
            }

            // Relocation warning: flash a thin red ring while the zone is about to move.
            if (koth.RelocateWarningTimer > 0f)
            {
                float blink = (Mathf.Sin(_state.Time * 12f) + 1f) * 0.5f;
                var warn = new Color(1f, 0.25f, 0.25f, 0.5f + 0.4f * blink);
                DrawArc(center, radius * 1.18f, 0f, Mathf.Tau, 48, warn, 0.12f);
            }
        }

        private static int ComputeLeader(float[] scores)
        {
            if (scores == null || scores.Length == 0) return -1;
            int leader = 0;
            float best = scores[0];
            for (int i = 1; i < scores.Length; i++)
            {
                if (scores[i] > best) { best = scores[i]; leader = i; }
            }
            // -1 leader = neutral when nobody has scored.
            return best > 0f ? leader : -1;
        }

        private float ComputeProgress(float[] scores, int leader)
        {
            if (scores == null || leader < 0 || leader >= scores.Length) return 0f;
            float pointsToWin = _state.Config?.KothPointsToWin ?? 60f;
            if (pointsToWin <= 0f) return 0f;
            return scores[leader] / pointsToWin;
        }

        private static Color TeamColor(int playerIndex) => playerIndex switch
        {
            0  => new Color(0.30f, 0.55f, 1.00f), // blue
            1  => new Color(1.00f, 0.30f, 0.30f), // red
            2  => new Color(0.30f, 0.85f, 0.40f), // green
            3  => new Color(1.00f, 0.85f, 0.30f), // gold
            _  => new Color(0.85f, 0.85f, 0.85f), // neutral gray
        };
    }
}
