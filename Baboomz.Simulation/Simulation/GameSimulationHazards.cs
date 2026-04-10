using System;

namespace Baboomz.Simulation
{
    /// <summary>Biome hazard spawning and per-tick effects (partial class of GameSimulation).</summary>
    public static partial class GameSimulation
    {
        static void SpawnBiomeHazards(GameState state, Random rng)
        {
            var biome = state.Biome;
            if (biome.HazardCount <= 0) return;

            float halfMap = state.Config.MapWidth / 2f;
            float spawnSafe = 5f; // min distance from spawn points
            float hazardSpacing = 8f; // min distance between hazards

            float hazardRadius = biome.HazardType == BiomeHazardType.Ice ? 4f
                : biome.HazardType == BiomeHazardType.Lava ? 2f
                : biome.HazardType == BiomeHazardType.Bounce ? 2f
                : biome.HazardType == BiomeHazardType.Quicksand ? 2.5f
                : biome.HazardType == BiomeHazardType.Firecracker ? 2.5f
                : biome.HazardType == BiomeHazardType.Gear ? 3f
                : biome.HazardType == BiomeHazardType.Whirlpool ? 4f
                : biome.HazardType == BiomeHazardType.Waterspout ? 3f
                : 3f; // Mud

            for (int i = 0; i < biome.HazardCount; i++)
            {
                // Try up to 20 positions to find valid placement
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    float x = (float)(rng.NextDouble() * (state.Config.MapWidth - 10f) - halfMap + 5f);
                    float y = GamePhysics.FindGroundY(state.Terrain, x, state.Config.SpawnProbeY, 0.1f);

                    // Skip if too close to spawn points
                    if (MathF.Abs(x - state.Config.Player1SpawnX) < spawnSafe) continue;
                    if (MathF.Abs(x - state.Config.Player2SpawnX) < spawnSafe) continue;

                    // Skip if too close to another hazard
                    bool tooClose = false;
                    for (int h = 0; h < state.BiomeHazards.Count; h++)
                    {
                        if (Vec2.Distance(new Vec2(x, y), state.BiomeHazards[h].Position) < hazardSpacing)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) continue;

                    state.BiomeHazards.Add(new BiomeHazardState
                    {
                        Position = new Vec2(x, y),
                        Radius = hazardRadius,
                        Type = biome.HazardType,
                        Active = true
                    });
                    break;
                }
            }
        }

        static void CheckBiomeHazards(GameState state, float dt)
        {
            for (int h = 0; h < state.BiomeHazards.Count; h++)
            {
                var hazard = state.BiomeHazards[h];
                if (!hazard.Active) continue;

                // Check if terrain under hazard was destroyed (waterspouts are atmospheric, not terrain-bound)
                if (hazard.Type != BiomeHazardType.Waterspout)
                {
                    int px = state.Terrain.WorldToPixelX(hazard.Position.x);
                    int py = state.Terrain.WorldToPixelY(hazard.Position.y - 0.5f);
                    if (!state.Terrain.IsSolid(px, py))
                    {
                        hazard.Active = false;
                        state.BiomeHazards[h] = hazard;
                        continue;
                    }
                }

                for (int p = 0; p < state.Players.Length; p++)
                {
                    ref PlayerState player = ref state.Players[p];
                    if (player.IsDead || player.IsInvulnerable) continue;

                    float dist = Vec2.Distance(player.Position, hazard.Position);
                    if (dist >= hazard.Radius) continue;

                    switch (hazard.Type)
                    {
                        case BiomeHazardType.Mud:
                            // Halve movement speed while inside (applied each frame)
                            player.Velocity.x *= 0.5f;
                            break;

                        case BiomeHazardType.Quicksand:
                            // Pull player downward
                            player.Velocity.y -= 1f * dt;
                            break;

                        case BiomeHazardType.Ice:
                            // Sliding effect: add momentum in facing direction, making it hard to stop
                            player.Velocity.x += player.FacingDirection * 3f * dt;
                            break;

                        case BiomeHazardType.Lava:
                            // Deal 8 DPS (reduced by armor)
                            float lavaDmg = 8f * dt / MathF.Max(player.ArmorMultiplier, 0.01f);
                            player.Health -= lavaDmg;
                            player.TotalDamageTaken += lavaDmg;
                            state.DamageEvents.Add(new DamageEvent
                            {
                                TargetIndex = p, Amount = lavaDmg,
                                Position = player.Position, SourceIndex = -1
                            });
                            if (player.Health <= 0f) { player.Health = 0f; player.IsDead = true; ScoreSurvivalKill(state, p); DropCtfFlag(state, p); SpawnHeadhunterTokens(state, p); }
                            break;

                        case BiomeHazardType.Bounce:
                            // Launch player upward on contact (only if grounded/falling)
                            if (player.IsGrounded || player.Velocity.y <= 0f)
                            {
                                player.Velocity.y = player.JumpForce * 2f;
                                player.IsGrounded = false;
                            }
                            break;

                        case BiomeHazardType.Firecracker:
                            // Strong upward launch + random horizontal scatter (no damage)
                            // Per-player cooldown prevents repeated knockback spam
                            if (player.FirecrackerCooldown <= 0f &&
                                (player.IsGrounded || player.Velocity.y <= 0f))
                            {
                                player.Velocity.y = 12f;
                                // Deterministic horizontal scatter using player index + time
                                float scatter = ((p * 7 + (int)(state.Time * 100f)) % 100) / 10f - 5f;
                                player.Velocity.x += scatter;
                                player.IsGrounded = false;
                                player.FirecrackerCooldown = 1.5f;
                            }
                            break;

                        case BiomeHazardType.Gear:
                            // Push player horizontally, alternating direction every 3s
                            float gearCycle = state.Time % 6f;
                            float pushDir = gearCycle < 3f ? 1f : -1f;
                            player.Velocity.x += pushDir * 4f * dt;
                            break;

                        case BiomeHazardType.Whirlpool:
                            // Pull player toward center (only if not in freefall)
                            if (player.Velocity.y > -2f)
                            {
                                Vec2 toCenter = new Vec2(
                                    hazard.Position.x - player.Position.x,
                                    hazard.Position.y - player.Position.y);
                                float d = toCenter.Magnitude;
                                if (d > 0.1f)
                                {
                                    float invD = 1f / d;
                                    player.Velocity.x += toCenter.x * invD * 3f * dt;
                                    player.Velocity.y += toCenter.y * invD * 3f * dt;
                                }
                            }
                            break;

                        case BiomeHazardType.Waterspout:
                            // Launch player upward with a strong Y impulse (per-player cooldown prevents spam)
                            if (player.FirecrackerCooldown <= 0f)
                            {
                                player.Velocity.y = 15f;
                                player.IsGrounded = false;
                                player.FirecrackerCooldown = 2f; // reuse existing cooldown field
                            }
                            break;
                    }
                }
            }
        }
    }
}
