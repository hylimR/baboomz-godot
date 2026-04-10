using System;

namespace Baboomz.Simulation
{
    /// <summary>Special projectile behaviors: sheep, drill, airstrike, cluster, napalm.</summary>
    public static partial class ProjectileSimulation
    {
        private static void UpdateSheep(GameState state, ref ProjectileState proj, float dt)
        {
            float walkDir = proj.Velocity.x >= 0f ? 1f : -1f;
            float walkSpeed = 4f;
            GamePhysics.ApplyGravity(ref proj.Velocity, dt, state.Config.Gravity);
            proj.Velocity.x = walkDir * walkSpeed;

            Vec2 newPos = proj.Position + proj.Velocity * dt;

            float groundY = GamePhysics.FindGroundY(state.Terrain, newPos.x, newPos.y + 2f, 0.1f);
            if (groundY > -999f && newPos.y <= groundY + 0.2f)
            {
                newPos.y = groundY + 0.1f;
                proj.Velocity.y = 0f;
            }

            for (int pi = 0; pi < state.Players.Length; pi++)
            {
                if (state.Players[pi].IsDead || pi == proj.OwnerIndex) continue;
                if (Vec2.Distance(newPos, state.Players[pi].Position + new Vec2(0f, 0.5f)) < 0.8f)
                {
                    CombatResolver.ApplyExplosion(state, newPos, proj.ExplosionRadius,
                        proj.MaxDamage, proj.KnockbackForce, proj.OwnerIndex, false, proj.SourceWeaponId);
                    proj.Alive = false;
                    return;
                }
            }

            proj.Position = newPos;

            if (proj.FuseTimer > 0f)
            {
                proj.FuseTimer -= dt;
                if (proj.FuseTimer <= 0f)
                {
                    CombatResolver.ApplyExplosion(state, proj.Position, proj.ExplosionRadius,
                        proj.MaxDamage, proj.KnockbackForce, proj.OwnerIndex, false, proj.SourceWeaponId);
                    proj.Alive = false;
                    return;
                }
            }

            float halfMap = state.Config.MapWidth / 2f;
            float sheepWaterY = MathF.Max(state.Config.DeathBoundaryY, state.WaterLevel);
            if (MathF.Abs(proj.Position.x) > halfMap || proj.Position.y < sheepWaterY)
                proj.Alive = false;
        }

        private static void UpdateDrill(GameState state, ref ProjectileState proj, float dt)
        {
            Vec2 newPos = proj.Position + proj.Velocity * dt;

            int steps = Math.Max(1, (int)(Vec2.Distance(proj.Position, newPos) * state.Terrain.PixelsPerUnit));
            for (int s = 0; s <= steps; s++)
            {
                float t = steps > 0 ? (float)s / steps : 0f;
                float wx = proj.Position.x + (newPos.x - proj.Position.x) * t;
                float wy = proj.Position.y + (newPos.y - proj.Position.y) * t;
                int px = state.Terrain.WorldToPixelX(wx);
                int py = state.Terrain.WorldToPixelY(wy);
                int drillRadius = (int)(1f * state.Terrain.PixelsPerUnit);
                state.Terrain.ClearCircleDestructible(px, py, drillRadius);
            }

            for (int pi = 0; pi < state.Players.Length; pi++)
            {
                if (state.Players[pi].IsDead || pi == proj.OwnerIndex) continue;
                float dist = Vec2.Distance(newPos, state.Players[pi].Position + new Vec2(0f, 0.5f));
                if (dist < 0.8f)
                {
                    CombatResolver.ApplyExplosion(state, newPos, proj.ExplosionRadius,
                        proj.MaxDamage, proj.KnockbackForce, proj.OwnerIndex, false, proj.SourceWeaponId);
                    proj.Alive = false;
                    return;
                }
            }

            proj.TravelDistance += Vec2.Distance(proj.Position, newPos);
            proj.Position = newPos;

            float halfMap = state.Config.MapWidth / 2f;
            float drillWaterY = MathF.Max(state.Config.DeathBoundaryY, state.WaterLevel);
            float maxRange = proj.DrillRange > 0f ? proj.DrillRange : 30f;
            if (proj.TravelDistance > maxRange || MathF.Abs(proj.Position.x) > halfMap
                || proj.Position.y < drillWaterY)
            {
                CombatResolver.ApplyExplosion(state, proj.Position, proj.ExplosionRadius,
                    proj.MaxDamage, proj.KnockbackForce, proj.OwnerIndex, false, proj.SourceWeaponId);
                proj.Alive = false;
            }
        }

        /// <summary>
        /// Applies vortex pull toward gravity bomb position for all players within PullRadius.
        /// Pull does not apply through solid terrain (LOS check).
        /// </summary>
        private static void ApplyVortexPull(GameState state, ref ProjectileState proj, float dt)
        {
            float pullRadiusSq = proj.PullRadius * proj.PullRadius;

            for (int pi = 0; pi < state.Players.Length; pi++)
            {
                if (state.Players[pi].IsDead) continue;

                Vec2 playerCenter = state.Players[pi].Position + new Vec2(0f, 0.5f);
                Vec2 diff = proj.Position - playerCenter;
                float distSq = diff.x * diff.x + diff.y * diff.y;

                if (distSq > pullRadiusSq || distSq < 0.01f) continue;

                // LOS check: skip if terrain blocks the pull
                if (GamePhysics.RaycastTerrain(state.Terrain, proj.Position, playerCenter, out _))
                    continue;

                float dist = MathF.Sqrt(distSq);
                Vec2 dir = new Vec2(diff.x / dist, diff.y / dist);
                // Apply pull as position displacement (velocity is overwritten by input/AI each frame)
                float prevY = state.Players[pi].Position.y;
                state.Players[pi].Position.x += dir.x * proj.PullForce * dt;
                state.Players[pi].Position.y += dir.y * proj.PullForce * dt;

                // Gravity Master challenge: pulled below water = void kill credit
                if (prevY >= state.WaterLevel && state.Players[pi].Position.y < state.WaterLevel
                    && proj.OwnerIndex >= 0 && proj.OwnerIndex < state.Players.Length && proj.OwnerIndex != pi)
                    state.Players[proj.OwnerIndex].GravityBombVoidKill = true;
            }
        }

        private static void UpdateBoomerang(GameState state, ref ProjectileState proj, float dt)
        {
            GamePhysics.ApplyGravity(ref proj.Velocity, dt, state.Config.Gravity);
            GamePhysics.ApplyWind(ref proj.Velocity, state.WindForce, dt);

            // Track whether the boomerang ever ascended (Velocity.y > 0)
            if (!proj.HasAscended && proj.Velocity.y > 0f)
                proj.HasAscended = true;

            // Detect apex: only trigger return after ascending then crossing zero
            // For horizontal/downward shots that never ascend, use a minimum travel
            // distance before allowing return (prevents immediate snap-back)
            if (!proj.IsReturning && proj.Velocity.y <= 0f && proj.HasAscended)
                proj.IsReturning = true;

            // Fallback: if the boomerang never ascended, trigger return after 8 units of travel
            if (!proj.IsReturning && !proj.HasAscended)
            {
                float stepDist = MathF.Sqrt(proj.Velocity.x * proj.Velocity.x + proj.Velocity.y * proj.Velocity.y) * dt;
                proj.TravelDistance += stepDist;
                if (proj.TravelDistance >= 8f)
                    proj.IsReturning = true;
            }

            // Return: steer toward thrower's current position (only if owner alive)
            if (proj.IsReturning && proj.OwnerIndex >= 0 && proj.OwnerIndex < state.Players.Length
                && !state.Players[proj.OwnerIndex].IsDead)
            {
                Vec2 throwerPos = state.Players[proj.OwnerIndex].Position + new Vec2(0f, 0.5f);
                Vec2 toThrower = throwerPos - proj.Position;
                float dist = MathF.Sqrt(toThrower.x * toThrower.x + toThrower.y * toThrower.y);

                if (dist > 0.1f)
                {
                    // Steer velocity toward thrower (gradual homing)
                    float steerForce = 15f;
                    proj.Velocity.x += (toThrower.x / dist) * steerForce * dt;
                    proj.Velocity.y += (toThrower.y / dist) * steerForce * dt;
                }

                // Caught by thrower — despawn without explosion
                if (dist < 1.5f)
                {
                    proj.Alive = false;
                    return;
                }
            }

            Vec2 newPos = proj.Position + proj.Velocity * dt;

            // Hit cooldown: FuseTimer repurposed as hit throttle for boomerang
            if (proj.FuseTimer > 0f)
                proj.FuseTimer -= dt;

            // Check player hits — only if cooldown expired (max once per 0.5s)
            if (proj.FuseTimer <= 0f)
            {
                for (int pi = 0; pi < state.Players.Length; pi++)
                {
                    if (state.Players[pi].IsDead || pi == proj.OwnerIndex) continue;
                    float dist = Vec2.Distance(newPos, state.Players[pi].Position + new Vec2(0f, 0.5f));
                    if (dist < 0.8f)
                    {
                        CombatResolver.ApplyExplosion(state, newPos, proj.ExplosionRadius,
                            proj.MaxDamage, proj.KnockbackForce, proj.OwnerIndex, false, proj.SourceWeaponId);
                        proj.FuseTimer = 0.5f; // cooldown before next hit
                        break;
                    }
                }
            }

            // Terrain collision — bounce once and reverse if not returning, else die
            if (GamePhysics.RaycastTerrain(state.Terrain, proj.Position, newPos, out Vec2 hitPoint))
            {
                if (!proj.IsReturning)
                {
                    proj.IsReturning = true;
                    proj.Position = hitPoint + new Vec2(0f, 0.2f);
                    proj.Velocity = new Vec2(-proj.Velocity.x * 0.6f, MathF.Abs(proj.Velocity.y) * 0.4f);
                    return;
                }
                proj.Alive = false;
                return;
            }

            proj.Position = newPos;

            // Out of bounds
            float waterY = MathF.Max(state.Config.DeathBoundaryY, state.WaterLevel);
            if (proj.Position.y < waterY || MathF.Abs(proj.Position.x) > state.Config.MapWidth / 2f)
                proj.Alive = false;
        }
    }
}
