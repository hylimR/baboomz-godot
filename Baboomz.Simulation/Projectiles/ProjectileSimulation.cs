using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Updates all projectile physics: gravity, wind, terrain collision,
    /// player collision, bouncing, and fuse timers.
    /// </summary>
    public static partial class ProjectileSimulation
    {
        public static void Update(GameState state, float dt)
        {
            for (int i = state.Projectiles.Count - 1; i >= 0; i--)
            {
                var proj = state.Projectiles[i];
                if (!proj.Alive)
                {
                    state.Projectiles.RemoveAt(i);
                    continue;
                }

                // Drill projectile: no gravity/wind, tunnels through terrain
                if (proj.IsDrill)
                {
                    UpdateDrill(state, ref proj, dt);
                    state.Projectiles[i] = proj;
                    continue;
                }

                // Sheep projectile: walks along terrain surface
                if (proj.IsSheep)
                {
                    UpdateSheep(state, ref proj, dt);
                    state.Projectiles[i] = proj;
                    continue;
                }

                // Sticky projectile stuck to a player: follow player position
                if (proj.IsSticky && proj.StuckToPlayerId >= 0)
                {
                    UpdateStuckToPlayer(state, ref proj, dt);
                    state.Projectiles[i] = proj;
                    continue;
                }

                // Sticky projectile stuck to terrain: stay in place, tick fuse
                if (proj.IsSticky && proj.StuckToTerrain)
                {
                    UpdateStuckToTerrain(state, ref proj, dt);
                    state.Projectiles[i] = proj;
                    continue;
                }

                // Boomerang projectile: custom arc with return
                if (proj.IsBoomerang)
                {
                    UpdateBoomerang(state, ref proj, dt);
                    state.Projectiles[i] = proj;
                    continue;
                }

                // Physics
                GamePhysics.ApplyGravity(ref proj.Velocity, dt, state.Config.Gravity);
                GamePhysics.ApplyWind(ref proj.Velocity, state.WindForce, dt);

                Vec2 newPos = proj.Position + proj.Velocity * dt;

                // Flak: detonate mid-air when distance from launch exceeds burst distance
                if (proj.IsFlak && proj.FlakBurstDistance > 0f)
                {
                    float dist = Vec2.Distance(newPos, proj.LaunchPosition);
                    if (dist >= proj.FlakBurstDistance)
                    {
                        CombatResolver.ApplyExplosion(state, newPos, proj.ExplosionRadius,
                            proj.MaxDamage, proj.KnockbackForce, proj.OwnerIndex, false, proj.SourceWeaponId);
                        SpawnFlakFragments(state, newPos, proj);
                        proj.Alive = false;
                        state.Projectiles[i] = proj;
                        continue;
                    }
                }

                // Player collision: sticky bombs attach, others check fuse
                if (proj.IsSticky && CheckStickyPlayerCollision(state, ref proj, newPos))
                {
                    state.Projectiles[i] = proj;
                    continue;
                }
                if (!proj.IsSticky && proj.FuseTimer <= 0f && CheckPlayerCollision(state, ref proj, newPos))
                {
                    state.Projectiles[i] = proj;
                    continue;
                }

                // Terrain collision
                if (CheckTerrainCollision(state, ref proj, newPos))
                {
                    state.Projectiles[i] = proj;
                    continue;
                }

                proj.Position = newPos;

                // Fuse timer
                if (proj.FuseTimer > 0f)
                {
                    proj.FuseTimer -= dt;
                    if (proj.FuseTimer <= 0f)
                    {
                        CombatResolver.ApplyExplosion(state, proj.Position,
                            proj.ExplosionRadius, proj.MaxDamage,
                            proj.KnockbackForce, proj.OwnerIndex, proj.DestroysIndestructible, proj.SourceWeaponId);
                        proj.Alive = false;
                        state.Projectiles[i] = proj;
                        continue;
                    }
                }

                // Out of bounds
                // Water splash when projectile hits water level (accounts for rising water during sudden death)
                float waterY = MathF.Max(state.Config.DeathBoundaryY, state.WaterLevel);
                if (proj.Position.y < waterY && proj.Alive)
                {
                    state.SplashEvents.Add(new SplashEvent
                    {
                        Position = new Vec2(proj.Position.x, waterY),
                        Size = 0.5f
                    });
                    proj.Alive = false;
                }
                else if (MathF.Abs(proj.Position.x) > state.Config.MapWidth / 2f)
                {
                    proj.Alive = false;
                }

                state.Projectiles[i] = proj;
            }
        }

        private static bool CheckPlayerCollision(GameState state, ref ProjectileState proj, Vec2 newPos)
        {
            for (int pi = 0; pi < state.Players.Length; pi++)
            {
                if (state.Players[pi].IsDead) continue;
                if (pi == proj.OwnerIndex && Vec2.Distance(newPos, state.Players[pi].Position) < 2f)
                    continue;
                // Skip players already pierced by this projectile
                if (proj.IsPiercing && pi == proj.LastPiercedPlayerId)
                    continue;

                float dist = Vec2.Distance(newPos, state.Players[pi].Position + new Vec2(0f, 0.5f));
                if (dist < 0.6f)
                {
                    // Piercing projectile: pass through player if under max pierce count
                    // Shield blocks the pierce — projectile stops on shielded targets
                    if (proj.IsPiercing && proj.PierceCount < proj.MaxPierceCount
                        && state.Players[pi].ShieldHP <= 0f)
                    {
                        CombatResolver.ApplyPierceDamage(state, pi, proj.MaxDamage,
                            proj.KnockbackForce, newPos, proj.OwnerIndex, proj.SourceWeaponId);
                        proj.PierceCount++;
                        proj.LastPiercedPlayerId = pi;
                        proj.Position = newPos;
                        return false; // still alive — let terrain/bounds checks run
                    }

                    if (proj.IsWindBlast)
                        CombatResolver.ApplyWindBlast(state, newPos, proj.ExplosionRadius,
                            proj.KnockbackForce, proj.OwnerIndex);
                    else
                        CombatResolver.ApplyExplosion(state, newPos, proj.ExplosionRadius,
                            proj.MaxDamage, proj.KnockbackForce, proj.OwnerIndex, proj.DestroysIndestructible, proj.SourceWeaponId);
                    if (proj.IsFlak && proj.ClusterCount > 0)
                        SpawnFlakFragments(state, newPos, proj);
                    proj.Alive = false;
                    return true;
                }
            }
            return false;
        }

        private static bool CheckTerrainCollision(GameState state, ref ProjectileState proj, Vec2 newPos)
        {
            if (!GamePhysics.RaycastTerrain(state.Terrain, proj.Position, newPos, out Vec2 hitPoint))
                return false;

            if (proj.BouncesRemaining > 0)
            {
                proj.BouncesRemaining--;

                if (proj.IsRicochet)
                {
                    // Ricochet: reflect off terrain normal + emit damage at bounce point
                    Vec2 normal = GamePhysics.EstimateTerrainNormal(state.Terrain, hitPoint);
                    proj.Velocity = GamePhysics.Reflect(proj.Velocity, normal) * 0.9f;
                    proj.Position = hitPoint + normal * 0.3f;
                    CombatResolver.ApplyExplosion(state, hitPoint, proj.ExplosionRadius,
                        proj.MaxDamage, proj.KnockbackForce, proj.OwnerIndex, false, proj.SourceWeaponId);
                }
                else
                {
                    // Regular bounce (dynamite etc): dampen velocity, nudge up
                    proj.Position = hitPoint + new Vec2(0f, 0.2f);
                    proj.Velocity = new Vec2(proj.Velocity.x * 0.8f, -proj.Velocity.y * 0.5f);
                }
                return true; // consumed, but still alive
            }

            // Sticky projectile: attach to terrain surface
            if (proj.IsSticky)
            {
                proj.Position = hitPoint + new Vec2(0f, 0.1f);
                proj.Velocity = Vec2.Zero;
                proj.StuckToTerrain = true;
                return true; // alive, fuse ticking via UpdateStuckToTerrain
            }

            // Fused projectiles with no bounces left rest on terrain until fuse expires
            if (proj.FuseTimer > 0f)
            {
                proj.Position = hitPoint + new Vec2(0f, 0.1f);
                proj.Velocity = Vec2.Zero;
                return true; // still alive, waiting for fuse
            }

            // Airstrike: spawn bombs raining from above at impact X position
            if (proj.IsAirstrike && proj.AirstrikeCount > 0)
            {
                SpawnAirstrikeBombs(state, hitPoint, proj);
            }
            // Napalm: create lingering fire zone + small initial explosion
            else if (proj.IsNapalm)
            {
                SpawnFireZone(state, hitPoint, proj);
                CombatResolver.ApplyExplosion(state, hitPoint, proj.ExplosionRadius * 0.5f,
                    proj.MaxDamage * 0.3f, 2f, proj.OwnerIndex, false, proj.SourceWeaponId);
            }
            // Flak: spawn downward fragments instead of upward cluster
            else if (proj.IsFlak && proj.ClusterCount > 0)
            {
                SpawnFlakFragments(state, hitPoint, proj);
                CombatResolver.ApplyExplosion(state, hitPoint, proj.ExplosionRadius * 0.5f,
                    proj.MaxDamage * 0.3f, 2f, proj.OwnerIndex, false, proj.SourceWeaponId);
            }
            // Cluster: spawn sub-projectiles instead of single explosion
            else if (proj.ClusterCount > 0)
            {
                SpawnClusterBombs(state, hitPoint, proj);
                CombatResolver.ApplyExplosion(state, hitPoint, proj.ExplosionRadius * 0.5f,
                    proj.MaxDamage * 0.3f, 2f, proj.OwnerIndex, false, proj.SourceWeaponId);
            }
            else if (proj.IsFreeze)
            {
                CombatResolver.ApplyFreezeExplosion(state, hitPoint, proj.ExplosionRadius,
                    2f, proj.OwnerIndex); // 2s freeze
                CombatResolver.ApplyExplosion(state, hitPoint, proj.ExplosionRadius * 0.5f,
                    proj.MaxDamage, 2f, proj.OwnerIndex, false, proj.SourceWeaponId); // minor splash damage
            }
            else if (proj.IsWindBlast)
            {
                CombatResolver.ApplyWindBlast(state, hitPoint, proj.ExplosionRadius,
                    proj.KnockbackForce, proj.OwnerIndex);
            }
            else
            {
                CombatResolver.ApplyExplosion(state, hitPoint, proj.ExplosionRadius,
                    proj.MaxDamage, proj.KnockbackForce, proj.OwnerIndex, proj.DestroysIndestructible, proj.SourceWeaponId);
            }

            proj.Alive = false;
            return true;
        }

        // UpdateSheep, UpdateDrill, UpdateBoomerang, ApplyVortexPull in ProjectileSimulationSpecial.cs (partial class)
        // CheckStickyPlayerCollision, UpdateStuckToPlayer, UpdateStuckToTerrain in ProjectileSimulationSticky.cs (partial class)
        // SpawnClusterBombs, SpawnAirstrikeBombs, SpawnFireZone in ProjectileSimulationSpawners.cs (partial class)
    }
}
