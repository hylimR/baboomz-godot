using System;

namespace Baboomz.Simulation
{
    /// <summary>Projectile spawn helpers: cluster, airstrike, fire zone (partial class of ProjectileSimulation).</summary>
    public static partial class ProjectileSimulation
    {
        private static void SpawnClusterBombs(GameState state, Vec2 origin, ProjectileState parent)
        {
            var rng = new System.Random(unchecked(state.NextProjectileId * 73856093) ^ state.Seed);
            // Spread sub-projectiles in a full arc (30°-150°) centered upward
            float clusterStep = parent.ClusterCount > 1 ? 120f / (parent.ClusterCount - 1) : 0f;
            for (int i = 0; i < parent.ClusterCount; i++)
            {
                float angle = 30f + clusterStep * i;
                // Add random jitter so spread isn't perfectly symmetric
                angle += (float)(rng.NextDouble() * 10.0 - 5.0);
                float rad = angle * MathF.PI / 180f;
                float speed = 8f + (float)(rng.NextDouble() * 5f);

                // Flip X based on parent's incoming velocity direction
                float dirX = MathF.Cos(rad);
                if (parent.Velocity.x < 0f) dirX = -MathF.Abs(dirX);
                else if (parent.Velocity.x > 0f) dirX = MathF.Abs(dirX);

                state.Projectiles.Add(new ProjectileState
                {
                    Id = state.NextProjectileId++,
                    Position = origin + new Vec2(0f, 0.3f),
                    Velocity = new Vec2(dirX, MathF.Sin(rad)) * speed,
                    OwnerIndex = parent.OwnerIndex,
                    ExplosionRadius = parent.ExplosionRadius,
                    MaxDamage = parent.MaxDamage,
                    KnockbackForce = parent.KnockbackForce,
                    DestroysIndestructible = false,
                    Alive = true,
                    ClusterCount = 0, // sub-projectiles don't cluster again
                    SourceWeaponId = parent.SourceWeaponId
                });
            }
        }

        private static void SpawnAirstrikeBombs(GameState state, Vec2 impactPoint, ProjectileState parent)
        {
            var rng = new System.Random(unchecked(state.NextProjectileId * 48271) ^ state.Seed);
            float dropY = state.Config.SpawnProbeY + 10f; // high above the map
            float spreadX = 4f; // horizontal spread of the bomb pattern

            for (int i = 0; i < parent.AirstrikeCount; i++)
            {
                float offsetX = (float)(rng.NextDouble() * spreadX - spreadX / 2f);

                state.Projectiles.Add(new ProjectileState
                {
                    Id = state.NextProjectileId++,
                    Position = new Vec2(impactPoint.x + (i - (parent.AirstrikeCount - 1) / 2f) * 2f + offsetX, dropY),
                    Velocity = new Vec2(0f, -15f), // straight down, fast
                    OwnerIndex = parent.OwnerIndex,
                    ExplosionRadius = parent.ExplosionRadius,
                    MaxDamage = parent.MaxDamage,
                    KnockbackForce = parent.KnockbackForce,
                    DestroysIndestructible = false,
                    Alive = true,
                    SourceWeaponId = parent.SourceWeaponId
                });
            }
        }

        private static void SpawnFlakFragments(GameState state, Vec2 origin, ProjectileState parent)
        {
            var rng = new System.Random(unchecked(state.NextProjectileId * 29423491) ^ state.Seed);
            // Scatter fragments in a 120° downward cone (±60° from straight down = 210°-330°)
            float flakStep = parent.ClusterCount > 1 ? 120f / (parent.ClusterCount - 1) : 0f;
            for (int i = 0; i < parent.ClusterCount; i++)
            {
                float angle = 210f + flakStep * i;
                angle += (float)(rng.NextDouble() * 10.0 - 5.0); // ±5° jitter
                float rad = angle * MathF.PI / 180f;
                float speed = 6f + (float)(rng.NextDouble() * 4f);

                state.Projectiles.Add(new ProjectileState
                {
                    Id = state.NextProjectileId++,
                    Position = origin,
                    Velocity = new Vec2(MathF.Cos(rad), MathF.Sin(rad)) * speed,
                    OwnerIndex = parent.OwnerIndex,
                    ExplosionRadius = 1f,
                    MaxDamage = parent.MaxDamage,
                    KnockbackForce = 2f,
                    DestroysIndestructible = false,
                    Alive = true,
                    ClusterCount = 0,
                    SourceWeaponId = parent.SourceWeaponId
                });
            }
        }

        private static void SpawnFireZone(GameState state, Vec2 impactPoint, ProjectileState proj)
        {
            state.FireZones.Add(new FireZoneState
            {
                Position = impactPoint,
                Radius = proj.IsLavaPool ? proj.LavaMeltRadius : proj.ExplosionRadius,
                DamagePerSecond = proj.FireZoneDPS > 0f ? proj.FireZoneDPS : 15f,
                RemainingTime = (proj.FireZoneDuration > 0f ? proj.FireZoneDuration : 5f) * state.Config.FireZoneDurationMult,
                OwnerIndex = proj.OwnerIndex,
                SourceWeaponId = proj.SourceWeaponId,
                Active = true,
                MeltsTerrain = proj.IsLavaPool,
                MeltRadius = proj.IsLavaPool ? proj.LavaMeltRadius : 0f
            });
        }
    }
}
