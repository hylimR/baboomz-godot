using System;

namespace Baboomz.Simulation
{
    public static partial class GameSimulation
    {
        public static void InitTerritories(GameState state, Random rng)
        {
            var config = state.Config;
            int zoneCount = config.TerritoryZoneCount;
            float mapWidth = config.MapWidth;

            var positions = new Vec2[zoneCount];
            float spacing = mapWidth / (zoneCount + 1);
            float halfMap = mapWidth / 2f;

            for (int i = 0; i < zoneCount; i++)
            {
                float x = -halfMap + spacing * (i + 1);
                float y = GamePhysics.FindGroundY(state.Terrain, x, config.SpawnProbeY) + 2f;
                positions[i] = new Vec2(x, y);
            }

            // Determine team count (2 teams in standard mode)
            int teamCount = 2;
            if (config.TeamMode)
            {
                int maxTeam = 0;
                for (int i = 0; i < state.Players.Length; i++)
                    if (state.Players[i].TeamIndex > maxTeam) maxTeam = state.Players[i].TeamIndex;
                teamCount = maxTeam + 1;
            }
            else
            {
                // Non-team mode: assign teams (player 0 = team 0, player 1 = team 1)
                for (int i = 0; i < state.Players.Length; i++)
                    state.Players[i].TeamIndex = i < state.Players.Length / 2 ? 0 : 1;
                teamCount = 2;
            }

            state.Territory = new TerritoryState
            {
                ZonePositions = positions,
                ZoneRadius = config.TerritoryZoneRadius,
                TeamScores = new float[teamCount],
                ZoneOwner = new int[zoneCount],
                ZoneContested = new bool[zoneCount]
            };

            for (int i = 0; i < zoneCount; i++)
                state.Territory.ZoneOwner[i] = -1;
        }

        static void UpdateTerritories(GameState state, float dt)
        {
            if (state.Config.MatchType != MatchType.Territories) return;

            ref TerritoryState territory = ref state.Territory;
            if (territory.ZonePositions == null) return;

            int zoneCount = territory.ZonePositions.Length;
            int teamCount = territory.TeamScores.Length;

            for (int z = 0; z < zoneCount; z++)
            {
                // Determine which teams have alive players in this zone
                var teamsPresent = new bool[teamCount];
                int teamsInZone = 0;
                int lastTeam = -1;

                for (int i = 0; i < state.Players.Length; i++)
                {
                    if (state.Players[i].IsDead || state.Players[i].IsMob) continue;
                    int team = state.Players[i].TeamIndex;
                    if (team < 0 || team >= teamCount) continue;

                    float dist = Vec2.Distance(state.Players[i].Position, territory.ZonePositions[z]);
                    if (dist <= territory.ZoneRadius)
                    {
                        if (!teamsPresent[team])
                        {
                            teamsPresent[team] = true;
                            teamsInZone++;
                            lastTeam = team;
                        }
                    }
                }

                territory.ZoneContested[z] = teamsInZone > 1;

                if (teamsInZone == 1)
                {
                    // Capture or maintain ownership when only one team is present
                    territory.ZoneOwner[z] = lastTeam;
                    territory.TeamScores[lastTeam] += state.Config.TerritoryPointsPerSecond * dt;
                }
                else if (teamsInZone == 0 && territory.ZoneOwner[z] >= 0)
                {
                    // Zone stays owned when unoccupied — owner keeps scoring
                    territory.TeamScores[territory.ZoneOwner[z]] +=
                        state.Config.TerritoryPointsPerSecond * dt;
                }
                // else: contested — no scoring, owner stays
            }

            // Check for winner
            for (int t = 0; t < teamCount; t++)
            {
                if (territory.TeamScores[t] >= state.Config.TerritoryPointsToWin)
                {
                    state.Phase = MatchPhase.Ended;
                    state.WinnerTeamIndex = t;
                    // Find first alive player on winning team, fall back to first on team
                    int fallback = -1;
                    for (int i = 0; i < state.Players.Length; i++)
                    {
                        if (state.Players[i].TeamIndex != t) continue;
                        if (fallback < 0) fallback = i;
                        if (!state.Players[i].IsDead)
                        {
                            state.WinnerIndex = i;
                            fallback = -1;
                            break;
                        }
                    }
                    if (fallback >= 0) state.WinnerIndex = fallback;
                    return;
                }
            }
        }
    }
}
