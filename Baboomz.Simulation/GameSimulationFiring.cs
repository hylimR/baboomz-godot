using System;

namespace Baboomz.Simulation
{
    /// <summary>Projectile firing logic (partial class of GameSimulation).</summary>
    public static partial class GameSimulation
    {
        public static void Fire(GameState state, int playerIndex)
        {
            ref PlayerState p = ref state.Players[playerIndex];
            if (p.IsDead) return;
            if (p.FreezeTimer > 0f) return; // frozen
            if (p.RetreatTimer > 0f) return; // retreat: movement only

            var weapon = p.WeaponSlots[p.ActiveWeaponSlot];
            if (weapon.WeaponId == null) return;
            if (p.ShootCooldownRemaining > 0f) return;

            if (weapon.Ammo == 0) return;

            if (weapon.EnergyCost > 0f && p.Energy < weapon.EnergyCost) return;
            if (weapon.EnergyCost > 0f) p.Energy -= weapon.EnergyCost;
            if (weapon.Ammo > 0)
            {
                p.WeaponSlots[p.ActiveWeaponSlot].Ammo--;
                if (p.WeaponSlots[p.ActiveWeaponSlot].Ammo == 0)
                {
                    for (int s = 0; s < p.WeaponSlots.Length; s++)
                    {
                        if (p.WeaponSlots[s].WeaponId != null && p.WeaponSlots[s].Ammo != 0)
                        {
                            p.ActiveWeaponSlot = s;
                            break;
                        }
                    }
                }
            }

            float power = Math.Max(p.AimPower, weapon.MinPower);

            // Track weapon usage for mastery
            if (state.WeaponsUsed != null && playerIndex < state.WeaponsUsed.Length)
                state.WeaponsUsed[playerIndex].Add(weapon.WeaponId);

            // Hitscan weapons: instant ray, no projectile
            if (weapon.IsHitscan)
            {
                FireHitscan(state, playerIndex, ref p, weapon, power);
                p.ShootCooldownRemaining = weapon.ShootCooldown * p.CooldownMultiplier;
                p.ShotsFired++;
                ConsumeOvercharge(state, ref p);
                OnRouletteShot(state, playerIndex);
                if (state.Config.RetreatDuration > 0f)
                    p.RetreatTimer = state.Config.RetreatDuration;
                return;
            }

            int count = Math.Max(1, weapon.ProjectileCount);

            for (int i = 0; i < count; i++)
            {
                float angleOffset = 0f;
                if (count > 1)
                    angleOffset = Lerp(-weapon.SpreadAngle / 2f, weapon.SpreadAngle / 2f, (float)i / (count - 1));

                float angle = p.AimAngle + angleOffset;
                float rad = angle * MathF.PI / 180f;
                float speedMult = state.Config.WeaponMasteryTiers != null
                    && p.ActiveWeaponSlot < state.Config.WeaponMasteryTiers.Length
                    ? WeaponMasteryCalc.GetSpeedMultiplier(state.Config.WeaponMasteryTiers[p.ActiveWeaponSlot])
                    : 1f;
                Vec2 velocity = new Vec2(MathF.Cos(rad) * p.FacingDirection, MathF.Sin(rad)) * power * speedMult;

                state.Projectiles.Add(new ProjectileState
                {
                    Id = state.NextProjectileId++,
                    Position = p.Position + new Vec2(0f, 0.5f),
                    Velocity = velocity,
                    OwnerIndex = playerIndex,
                    ExplosionRadius = weapon.ExplosionRadius,
                    MaxDamage = weapon.MaxDamage * p.DamageMultiplier,
                    KnockbackForce = weapon.KnockbackForce,
                    DestroysIndestructible = weapon.DestroysIndestructible,
                    Alive = true,
                    BouncesRemaining = weapon.Bounces,
                    FuseTimer = weapon.FuseTime,
                    ClusterCount = weapon.ClusterCount,
                    IsAirstrike = weapon.IsAirstrike,
                    AirstrikeCount = weapon.AirstrikeCount,
                    IsNapalm = weapon.IsNapalm,
                    FireZoneDuration = weapon.FireZoneDuration,
                    FireZoneDPS = weapon.FireZoneDPS,
                    IsDrill = weapon.IsDrill,
                    DrillRange = weapon.DrillRange,
                    IsSheep = weapon.IsSheep,
                    IsFreeze = weapon.IsFreeze,
                    IsSticky = weapon.IsSticky,
                    StuckToPlayerId = -1,
                    IsBoomerang = weapon.IsBoomerang,
                    IsGravityBomb = weapon.IsGravityBomb,
                    PullRadius = weapon.PullRadius,
                    PullForce = weapon.PullForce,
                    IsRicochet = weapon.IsRicochet,
                    IsLavaPool = weapon.IsLavaPool,
                    LavaMeltRadius = weapon.LavaMeltRadius,
                    IsWindBlast = weapon.IsWindBlast,
                    IsPiercing = weapon.IsPiercing,
                    MaxPierceCount = weapon.MaxPierceCount,
                    LastPiercedPlayerId = -1,
                    IsFlak = weapon.IsFlak,
                    FlakBurstDistance = weapon.IsFlak
                        ? Lerp(weapon.FlakMinDist, weapon.FlakMaxDist,
                              (weapon.MaxPower - weapon.MinPower) > 0f
                                  ? Math.Clamp((power - weapon.MinPower) / (weapon.MaxPower - weapon.MinPower), 0f, 1f)
                                  : 0f)
                        : 0f,
                    LaunchPosition = p.Position + new Vec2(0f, 0.5f),
                    SourceWeaponId = weapon.WeaponId
                });
            }

            p.ShootCooldownRemaining = weapon.ShootCooldown * p.CooldownMultiplier;
            p.ShotsFired++;
            ConsumeOvercharge(state, ref p);
            OnRouletteShot(state, playerIndex);

            // Start retreat timer (movement-only window after firing)
            if (state.Config.RetreatDuration > 0f)
                p.RetreatTimer = state.Config.RetreatDuration;
        }

        /// <summary>Consume an armed Overcharge buff after a shot is fired (multiplier reverts).</summary>
        static void ConsumeOvercharge(GameState state, ref PlayerState p)
        {
            if (p.OverchargeTimer <= 0f) return;
            p.OverchargeTimer = 0f;
            RevertOverchargeMultiplier(state, ref p);
        }

        internal static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
