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

        static void SpawnMines(GameState state, Random rng)
        {
            var config = state.Config;
            float halfMap = config.MapWidth / 2f;
            for (int i = 0; i < config.MineCount; i++)
            {
                float x = (float)(rng.NextDouble() * config.MapWidth - halfMap);
                float y = GamePhysics.FindGroundY(state.Terrain, x, config.SpawnProbeY, 0.1f);
                if (MathF.Abs(y - config.SpawnProbeY) < 0.2f) continue; // no ground at this X, skip
                state.Mines.Add(new MineState
                {
                    Position = new Vec2(x, y),
                    TriggerRadius = config.MineTriggerRadius,
                    ExplosionRadius = config.MineExplosionRadius,
                    Damage = config.MineDamage,
                    Active = true,
                    OwnerIndex = -1 // environment mine
                });
            }
        }

        static void CheckMines(GameState state, float dt)
        {
            for (int m = 0; m < state.Mines.Count; m++)
            {
                var mine = state.Mines[m];
                if (!mine.Active) continue;

                if (mine.Lifetime > 0f)
                {
                    mine.Lifetime -= dt;
                    if (mine.Lifetime <= 0f)
                    {
                        mine.Active = false;
                        state.Mines[m] = mine;
                        continue;
                    }
                    state.Mines[m] = mine;
                }

                if (mine.ActivationDelay > 0f)
                {
                    mine.ActivationDelay -= dt;
                    state.Mines[m] = mine;
                    continue;
                }

                if (mine.IsHoming)
                    UpdateHomingMine(state, ref mine, dt);

                state.Mines[m] = mine;

                for (int p = 0; p < state.Players.Length; p++)
                {
                    if (state.Players[p].IsDead) continue;
                    if (p == mine.OwnerIndex) continue;
                    float dist = Vec2.Distance(mine.Position, state.Players[p].Position);
                    if (dist < mine.TriggerRadius)
                    {
                        CombatResolver.ApplyExplosion(state, mine.Position,
                            mine.ExplosionRadius, mine.Damage, 8f, mine.OwnerIndex, false);
                        mine.Active = false;
                        state.Mines[m] = mine;
                        break;
                    }
                }
            }

            for (int i = state.Mines.Count - 1; i >= 0; i--)
                if (!state.Mines[i].Active)
                    state.Mines.RemoveAt(i);
        }

        static void UpdateHomingMine(GameState state, ref MineState mine, float dt)
        {
            int nearest = -1;
            float nearestDist = mine.DetectionRange;
            for (int p = 0; p < state.Players.Length; p++)
            {
                if (state.Players[p].IsDead) continue;
                if (p == mine.OwnerIndex) continue;
                float dist = Vec2.Distance(mine.Position, state.Players[p].Position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = p;
                }
            }

            if (nearest < 0) return;

            float dirX = state.Players[nearest].Position.x - mine.Position.x;
            if (MathF.Abs(dirX) < 0.01f) return;
            float sign = dirX > 0f ? 1f : -1f;
            float moveX = sign * mine.MoveSpeed * dt;

            float newX = mine.Position.x + moveX;
            float groundY = GamePhysics.FindGroundY(state.Terrain, newX, mine.Position.y + 2f, 0.1f);
            mine.Position = new Vec2(newX, groundY + 0.1f);
        }

        static void SpawnBarrels(GameState state, Random rng)
        {
            var config = state.Config;
            if (config.BarrelCount <= 0) return;

            float halfMap = config.MapWidth / 2f;
            for (int i = 0; i < config.BarrelCount; i++)
            {
                float x = (float)(rng.NextDouble() * config.MapWidth - halfMap);
                float y = GamePhysics.FindGroundY(state.Terrain, x, config.SpawnProbeY, 0.1f);
                if (MathF.Abs(y - config.SpawnProbeY) < 0.2f) continue; // no ground at this X, skip

                state.Barrels.Add(new BarrelState
                {
                    Position = new Vec2(x, y),
                    ExplosionRadius = config.BarrelExplosionRadius,
                    Damage = config.BarrelDamage,
                    Active = true
                });
            }
        }

        static void CheckBarrels(GameState state)
        {
            if (state.Barrels.Count == 0 || state.ExplosionEvents.Count == 0) return;

            int checkedUpTo = 0;
            int safetyLimit = state.Barrels.Count + 1;

            while (checkedUpTo < state.ExplosionEvents.Count && safetyLimit-- > 0)
            {
                int explosionCount = state.ExplosionEvents.Count;

                for (int b = 0; b < state.Barrels.Count; b++)
                {
                    var barrel = state.Barrels[b];
                    if (!barrel.Active) continue;

                    for (int e = checkedUpTo; e < explosionCount; e++)
                    {
                        var explosion = state.ExplosionEvents[e];
                        float dist = Vec2.Distance(barrel.Position, explosion.Position);
                        if (dist < explosion.Radius + 0.5f)
                        {
                            barrel.Active = false;
                            barrel.OwnerIndex = explosion.OwnerIndex;
                            state.Barrels[b] = barrel;
                            state.BarrelDetonationsThisTick++;

                            CombatResolver.ApplyExplosion(state, barrel.Position,
                                barrel.ExplosionRadius, barrel.Damage, 10f, explosion.OwnerIndex, false);
                            break;
                        }
                    }
                }

                checkedUpTo = explosionCount;
            }

            // Remove inactive barrels to prevent unbounded list growth
            for (int i = state.Barrels.Count - 1; i >= 0; i--)
                if (!state.Barrels[i].Active)
                    state.Barrels.RemoveAt(i);
        }

    }
}
