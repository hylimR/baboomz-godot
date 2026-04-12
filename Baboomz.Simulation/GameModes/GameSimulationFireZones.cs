using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Fire zone (napalm, lava pool) damage-over-time and terrain melt logic.
    /// Partial class extension of GameSimulation.
    /// </summary>
    public static partial class GameSimulation
    {
        static void UpdateFireZones(GameState state, float dt)
        {
            for (int f = state.FireZones.Count - 1; f >= 0; f--)
            {
                var zone = state.FireZones[f];
                if (!zone.Active)
                {
                    state.FireZones.RemoveAt(f);
                    continue;
                }

                zone.RemainingTime -= dt;
                if (zone.RemainingTime <= 0f)
                {
                    state.FireZones.RemoveAt(f);
                    continue;
                }

                // Lava pool terrain melt: every 0.5s, destroy 1-pixel depth under pool
                if (zone.MeltsTerrain && zone.MeltRadius > 0f)
                {
                    zone.MeltTimer += dt;
                    while (zone.MeltTimer >= 0.5f)
                    {
                        zone.MeltTimer -= 0.5f;
                        int cx = state.Terrain.WorldToPixelX(zone.Position.x);
                        int cy = state.Terrain.WorldToPixelY(zone.Position.y);
                        int meltPixelRadius = (int)(zone.MeltRadius * state.Terrain.PixelsPerUnit * state.Config.TerrainDestructionMult);
                        state.Terrain.ClearCircleDestructible(cx, cy, meltPixelRadius);
                    }
                }

                // Throttle DamageEvent emission timer
                zone.DamageEventTimer -= dt;
                bool resetEventTimer = zone.DamageEventTimer <= 0f;

                // Damage players standing in the fire zone
                for (int p = 0; p < state.Players.Length; p++)
                {
                    ref PlayerState player = ref state.Players[p];
                    if (player.IsDead) continue;
                    if (player.IsInvulnerable) continue;

                    if (p == zone.OwnerIndex) continue; // don't damage caster
                    float dist = Vec2.Distance(zone.Position, player.Position);
                    if (dist < zone.Radius)
                    {
                        float damage = zone.DamagePerSecond * dt;
                        if (zone.OwnerIndex >= 0 && zone.OwnerIndex < state.Players.Length)
                            damage *= state.Players[zone.OwnerIndex].DamageMultiplier;
                        damage *= (1f / MathF.Max(player.ArmorMultiplier, 0.01f));
                        player.Health -= damage;
                        player.TotalDamageTaken += damage;

                        // Throttle DamageEvent emission to 0.5s intervals
                        // to prevent hit marker/KillFeed spam. Damage is still
                        // applied every frame for correct HP tracking.
                        if (zone.DamageEventTimer <= 0f)
                        {
                            // Report 0.5s worth of effective damage (with multipliers)
                            float eventDamage = zone.DamagePerSecond * 0.5f;
                            if (zone.OwnerIndex >= 0 && zone.OwnerIndex < state.Players.Length)
                                eventDamage *= state.Players[zone.OwnerIndex].DamageMultiplier;
                            eventDamage *= (1f / MathF.Max(player.ArmorMultiplier, 0.01f));

                            state.DamageEvents.Add(new DamageEvent
                            {
                                TargetIndex = p,
                                Amount = eventDamage,
                                Position = player.Position,
                                SourceIndex = zone.OwnerIndex
                            });
                        }

                        // Track stats
                        if (zone.OwnerIndex >= 0 && zone.OwnerIndex < state.Players.Length && p != zone.OwnerIndex)
                            state.Players[zone.OwnerIndex].TotalDamageDealt += damage;

                        if (player.Health <= 0f)
                        {
                            player.Health = 0f;
                            player.IsDead = true;
                            ScoreSurvivalKill(state, p);
                            DropCtfFlag(state, p);
                            SpawnHeadhunterTokens(state, p);

                            if (zone.OwnerIndex >= 0 && zone.OwnerIndex < state.Players.Length && p != zone.OwnerIndex)
                            {
                                CombatResolver.TrackKill(state, zone.OwnerIndex);
                                CombatResolver.TrackWeaponKill(state, zone.OwnerIndex, zone.SourceWeaponId);
                            }
                        }
                    }
                }

                if (resetEventTimer)
                    zone.DamageEventTimer = 0.5f;

                state.FireZones[f] = zone;
            }
        }
    }
}
