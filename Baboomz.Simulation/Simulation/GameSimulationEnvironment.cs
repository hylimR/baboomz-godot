using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Environment hazards (mines, barrels), sudden death, wind updates,
    /// death boundary, and match end checks.
    /// Partial class extension of GameSimulation.
    /// </summary>
    public static partial class GameSimulation
    {
        static void UpdateSuddenDeath(GameState state, float dt)
        {
            // Arms Race suppresses sudden death without mutating the shared config
            if (state.Config.MatchType == MatchType.ArmsRace && state.ArmsRace.DisableSuddenDeath) return;

            float sdTime = state.Config.SuddenDeathTime;
            if (sdTime <= 0f) return;
            if (state.Time >= sdTime)
            {
                state.SuddenDeathActive = true;
                state.WaterLevel += state.Config.WaterRiseSpeed * dt;
            }
        }

        static void CheckDeathBoundary(GameState state, float dt)
        {
            float waterY = state.WaterLevel;
            for (int i = 0; i < state.Players.Length; i++)
            {
                ref PlayerState p = ref state.Players[i];
                if (p.IsDead) continue;

                if (p.Position.y < waterY)
                {
                    if (!p.IsSwimming)
                    {
                        p.IsSwimming = true;
                        p.SwimTimer = 0f;
                        state.SplashEvents.Add(new SplashEvent
                        {
                            Position = new Vec2(p.Position.x, waterY),
                            Size = 1f
                        });
                    }

                    p.SwimTimer += dt;
                    if (p.SwimTimer >= state.Config.SwimDuration)
                    {
                        p.Health = 0f;
                        p.IsDead = true;
                        ScoreSurvivalKill(state, i);
                        DropCtfFlag(state, i);
                        SpawnHeadhunterTokens(state, i);

                        int attacker = p.LastDamagedByIndex;
                        if (attacker >= 0 && attacker < state.Players.Length
                            && attacker != i && p.LastDamagedByTimer > 0f)
                        {
                            CombatResolver.TrackKill(state, attacker);
                            state.Players[attacker].TotalKills++;
                            state.DamageEvents.Add(new DamageEvent
                            {
                                TargetIndex = i,
                                Amount = 0f,
                                Position = p.Position,
                                SourceIndex = attacker
                            });
                        }
                    }
                }
                else if (p.IsSwimming)
                {
                    p.IsSwimming = false;
                    p.SwimTimer = 0f;
                }
            }
        }

        static void CheckMatchEnd(GameState state)
        {
            // Survival mode handles its own end condition in UpdateSurvival
            if (state.Config.MatchType == MatchType.Survival) return;

            // Campaign handles its own end condition via CampaignBootstrap/ObjectiveTracker
            if (state.Config.MatchType == MatchType.Campaign) return;

            // Target Practice ends via timer, not death count
            if (state.Config.MatchType == MatchType.TargetPractice) return;

            // Demolition handles its own end condition in UpdateDemolition
            if (state.Config.MatchType == MatchType.Demolition) return;

            // Payload handles its own end condition in UpdatePayload
            if (state.Config.MatchType == MatchType.Payload) return;

            // CTF handles its own end condition in UpdateCtf
            if (state.Config.MatchType == MatchType.CaptureTheFlag) return;

            // Headhunter handles its own end condition (respawns, so death ≠ elimination)
            if (state.Config.MatchType == MatchType.Headhunter) return;

            // KOTH: win by score is handled in UpdateKoth; still end if all but one die
            if (state.Config.TeamMode)
            {
                CheckTeamMatchEnd(state);
                return;
            }

            int aliveCount = 0, lastAlive = -1;
            for (int i = 0; i < state.Players.Length; i++)
            {
                if (!state.Players[i].IsDead) { aliveCount++; lastAlive = i; }
            }
            if (aliveCount <= 1)
            {
                state.Phase = MatchPhase.Ended;
                state.WinnerIndex = aliveCount == 1 ? lastAlive : -1;
            }
        }

        static void CheckTeamMatchEnd(GameState state)
        {
            int maxTeam = 0;
            for (int i = 0; i < state.Players.Length; i++)
                if (state.Players[i].TeamIndex > maxTeam) maxTeam = state.Players[i].TeamIndex;

            int aliveTeams = 0;
            int lastAliveTeam = -1;
            int lastAlivePlayer = -1;

            for (int t = 0; t <= maxTeam; t++)
            {
                bool teamHasAlive = false;
                for (int i = 0; i < state.Players.Length; i++)
                {
                    if (state.Players[i].TeamIndex == t && !state.Players[i].IsDead)
                    {
                        teamHasAlive = true;
                        lastAlivePlayer = i;
                        break;
                    }
                }
                if (teamHasAlive)
                {
                    aliveTeams++;
                    lastAliveTeam = t;
                }
            }

            if (aliveTeams <= 1)
            {
                state.Phase = MatchPhase.Ended;
                state.WinnerTeamIndex = aliveTeams == 1 ? lastAliveTeam : -1;
                state.WinnerIndex = aliveTeams == 1 ? lastAlivePlayer : -1;
            }
        }

        static void UpdateWind(GameState state, Random rng)
        {
            float strength = (float)(rng.NextDouble() * state.Config.MaxWindStrength * 2 - state.Config.MaxWindStrength);
            state.WindForce = strength;
            state.WindAngle = strength >= 0f ? 0f : 180f;
        }

    }
}
