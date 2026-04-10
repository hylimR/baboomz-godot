using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Crate drop spawning, physics, and collection logic.
    /// Partial class extension of GameSimulation.
    /// </summary>
    public static partial class GameSimulation
    {
        static void UpdateCrates(GameState state, float dt)
        {
            var config = state.Config;

            // Spawn new crate when timer elapses (only if crate spawning enabled and not suppressed by mode)
            bool cratesSuppressed = state.Config.MatchType == MatchType.ArmsRace && state.ArmsRace.DisableCrates;
            if (config.CrateSpawnInterval > 0f && !cratesSuppressed)
            {
                if (state.Time >= state.NextCrateSpawnTime && state.NextCrateSpawnTime > 0f)
                {
                    SpawnCrate(state);
                    state.NextCrateSpawnTime = state.Time + config.CrateSpawnInterval;
                }
                else if (state.NextCrateSpawnTime <= 0f)
                {
                    state.NextCrateSpawnTime = config.CrateSpawnInterval;
                }
            }

            // Update existing crates (physics + collection)
            if (state.Crates.Count == 0) return;
            for (int i = 0; i < state.Crates.Count; i++)
            {
                var crate = state.Crates[i];
                if (!crate.Active) continue;

                // Gravity for falling crates
                if (!crate.Grounded)
                {
                    GamePhysics.ApplyGravity(ref crate.Velocity, dt, config.Gravity * 0.5f);
                    crate.Position = crate.Position + crate.Velocity * dt;

                    // Check ground collision
                    if (GamePhysics.IsGrounded(state.Terrain, crate.Position))
                    {
                        crate.Grounded = true;
                        crate.Velocity = Vec2.Zero;
                        GamePhysics.ResolveTerrainPenetration(state.Terrain, ref crate.Position);
                    }

                    // Fell below death boundary
                    float deathY = MathF.Max(config.DeathBoundaryY, state.WaterLevel);
                    if (crate.Position.y < deathY)
                    {
                        crate.Active = false;
                        state.Crates[i] = crate;
                        continue;
                    }
                }

                // Check if rising water submerged a grounded crate
                if (crate.Grounded)
                {
                    float deathY = MathF.Max(config.DeathBoundaryY, state.WaterLevel);
                    if (crate.Position.y < deathY)
                    {
                        crate.Active = false;
                        state.Crates[i] = crate;
                        continue;
                    }
                }

                // Collection check — any alive player within radius
                for (int p = 0; p < state.Players.Length; p++)
                {
                    if (state.Players[p].IsDead) continue;
                    float dist = Vec2.Distance(crate.Position, state.Players[p].Position);
                    if (dist < config.CrateCollectRadius)
                    {
                        CollectCrate(state, p, crate.Type);
                        crate.Active = false;
                        break;
                    }
                }

                state.Crates[i] = crate;
            }

            // Remove inactive crates to prevent unbounded list growth
            for (int i = state.Crates.Count - 1; i >= 0; i--)
            {
                if (!state.Crates[i].Active)
                    state.Crates.RemoveAt(i);
            }
        }

        static void SpawnCrate(GameState state)
        {
            var rng = new Random(state.Seed + (int)(state.Time * 37));
            float halfMap = state.Config.MapWidth / 2f;

            // Random X position within map
            float x = (float)(rng.NextDouble() * state.Config.MapWidth - halfMap);
            // Drop from above
            float y = state.Config.SpawnProbeY + 5f;

            // Random type
            int crateTypeCount = Enum.GetValues(typeof(CrateType)).Length;
            CrateType type = (CrateType)(rng.Next(0, crateTypeCount));

            state.Crates.Add(new CrateState
            {
                Position = new Vec2(x, y),
                Velocity = Vec2.Zero,
                Type = type,
                Active = true,
                Grounded = false
            });
        }

        static void CollectCrate(GameState state, int playerIndex, CrateType type)
        {
            ref PlayerState p = ref state.Players[playerIndex];
            var config = state.Config;

            switch (type)
            {
                case CrateType.Health:
                    p.Health = MathF.Min(p.MaxHealth, p.Health + config.CrateHealthAmount);
                    break;

                case CrateType.Energy:
                    p.Energy = MathF.Min(p.MaxEnergy, p.Energy + config.CrateEnergyAmount);
                    break;

                case CrateType.AmmoRefill:
                    for (int s = 0; s < p.WeaponSlots.Length; s++)
                    {
                        if (p.WeaponSlots[s].WeaponId == null) continue;
                        // Find original ammo count from config
                        for (int w = 0; w < config.Weapons.Length; w++)
                        {
                            if (config.Weapons[w].WeaponId == p.WeaponSlots[s].WeaponId)
                            {
                                if (config.Weapons[w].Ammo > 0)
                                    p.WeaponSlots[s].Ammo = config.Weapons[w].Ammo;
                                break;
                            }
                        }
                    }
                    break;

                case CrateType.DoubleDamage:
                    // Only apply if not already buffed (prevents infinite stacking)
                    if (p.DoubleDamageTimer <= 0f)
                    {
                        p.DamageMultiplier = 2f;
                        p.DoubleDamageTimer = config.CrateDoubleDamageDuration;
                    }
                    break;
            }
        }
    }
}
