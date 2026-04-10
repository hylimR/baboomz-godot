using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// King of the Hill mode logic: zone scoring, contesting, and relocation.
    /// Partial class extension of GameSimulation.
    /// </summary>
    public static partial class GameSimulation
    {
        public static void InitKoth(GameState state, Random rng)
        {
            var config = state.Config;
            state.Koth = new KothState
            {
                ZoneRadius = config.KothZoneRadius,
                Scores = new float[state.Players.Length],
                RelocateTimer = config.KothRelocateInterval,
                IsContested = false,
                RelocateWarningTimer = 0f
            };
            RelocateZone(state, rng);
        }

        static void UpdateKoth(GameState state, float dt)
        {
            if (state.Config.MatchType != MatchType.KingOfTheHill) return;

            ref KothState koth = ref state.Koth;

            // Zone relocation timer
            koth.RelocateTimer -= dt;
            if (koth.RelocateTimer <= state.Config.KothRelocateWarning && koth.RelocateWarningTimer <= 0f)
                koth.RelocateWarningTimer = state.Config.KothRelocateWarning;

            if (koth.RelocateWarningTimer > 0f)
                koth.RelocateWarningTimer -= dt;

            if (koth.RelocateTimer <= 0f)
            {
                var rng = new Random(state.Seed + (int)(state.Time * 100));
                RelocateZone(state, rng);
                koth.RelocateTimer = state.Config.KothRelocateInterval;
                koth.RelocateWarningTimer = 0f;
            }

            // Count alive players inside the zone
            int playersInZone = 0;
            int scoringPlayer = -1;

            for (int i = 0; i < state.Players.Length; i++)
            {
                if (state.Players[i].IsDead) continue;
                float dist = Vec2.Distance(state.Players[i].Position, koth.ZonePosition);
                if (dist <= koth.ZoneRadius)
                {
                    playersInZone++;
                    scoringPlayer = i;
                }
            }

            koth.IsContested = playersInZone >= 2;

            // Score: exactly 1 player in zone = they score
            if (playersInZone == 1 && scoringPlayer >= 0)
            {
                koth.Scores[scoringPlayer] += state.Config.KothPointsPerSecond * dt;

                // Check for win
                if (koth.Scores[scoringPlayer] >= state.Config.KothPointsToWin)
                {
                    state.Phase = MatchPhase.Ended;
                    state.WinnerIndex = scoringPlayer;
                }
            }
        }

        static void RelocateZone(GameState state, Random rng)
        {
            float halfMap = state.Config.MapWidth / 2f;
            float margin = 30f;
            float range = halfMap - margin;
            if (range < 5f) range = halfMap * 0.8f;

            float x = (float)(rng.NextDouble() * range * 2f - range);
            float y = GamePhysics.FindGroundY(state.Terrain, x, state.Config.SpawnProbeY, 0.5f);

            state.Koth.ZonePosition = new Vec2(x, y);
        }
    }
}
