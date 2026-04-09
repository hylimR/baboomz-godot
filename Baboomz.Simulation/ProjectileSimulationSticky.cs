using System;

namespace Baboomz.Simulation
{
    /// <summary>Sticky bomb collision and update logic (partial class of ProjectileSimulation).</summary>
    public static partial class ProjectileSimulation
    {
        private static bool CheckStickyPlayerCollision(GameState state, ref ProjectileState proj, Vec2 newPos)
        {
            for (int pi = 0; pi < state.Players.Length; pi++)
            {
                if (state.Players[pi].IsDead) continue;
                if (pi == proj.OwnerIndex && Vec2.Distance(newPos, state.Players[pi].Position) < 2f)
                    continue;

                float dist = Vec2.Distance(newPos, state.Players[pi].Position + new Vec2(0f, 0.5f));
                if (dist < 0.6f)
                {
                    proj.StuckToPlayerId = pi;
                    proj.Position = state.Players[pi].Position + new Vec2(0f, 0.5f);
                    proj.Velocity = Vec2.Zero;
                    return true;
                }
            }
            return false;
        }

        private static void UpdateStuckToPlayer(GameState state, ref ProjectileState proj, float dt)
        {
            int pi = proj.StuckToPlayerId;
            if (pi < 0 || pi >= state.Players.Length || state.Players[pi].IsDead)
            {
                // Player died or invalid — explode at current position
                CombatResolver.ApplyExplosion(state, proj.Position,
                    proj.ExplosionRadius, proj.MaxDamage,
                    proj.KnockbackForce, proj.OwnerIndex, proj.DestroysIndestructible, proj.SourceWeaponId);
                proj.Alive = false;
                return;
            }

            // Follow the player
            proj.Position = state.Players[pi].Position + new Vec2(0f, 0.5f);

            // Gravity bomb vortex pull while stuck to player
            if (proj.IsGravityBomb)
                ApplyVortexPull(state, ref proj, dt);

            // Tick fuse
            proj.FuseTimer -= dt;
            if (proj.FuseTimer <= 0f)
            {
                CombatResolver.ApplyExplosion(state, proj.Position,
                    proj.ExplosionRadius, proj.MaxDamage,
                    proj.KnockbackForce, proj.OwnerIndex, proj.DestroysIndestructible, proj.SourceWeaponId);
                proj.Alive = false;
            }
        }

        private static void UpdateStuckToTerrain(GameState state, ref ProjectileState proj, float dt)
        {
            // Gravity bomb vortex pull while fuse ticks
            if (proj.IsGravityBomb)
                ApplyVortexPull(state, ref proj, dt);

            proj.FuseTimer -= dt;
            if (proj.FuseTimer <= 0f)
            {
                CombatResolver.ApplyExplosion(state, proj.Position,
                    proj.ExplosionRadius, proj.MaxDamage,
                    proj.KnockbackForce, proj.OwnerIndex, proj.DestroysIndestructible, proj.SourceWeaponId);
                proj.Alive = false;
            }
        }
    }
}
