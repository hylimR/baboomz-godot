using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Mine and barrel logic: spawning, proximity checks, homing mines, chain detonation.
    /// Partial class extension of GameSimulation — extracted from GameSimulationEnvironment.cs.
    /// </summary>
    public static partial class GameSimulation
    {
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
