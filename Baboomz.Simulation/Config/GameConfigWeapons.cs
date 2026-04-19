namespace Baboomz.Simulation
{
    public partial class GameConfig
    {
        public WeaponDef[] Weapons = new[]
        {
            new WeaponDef
            {
                WeaponId = "cannon",
                // Balance #133: EnergyCost 8 -> 11 to tame the 2.50 DPS/E outlier
                // (was 2.81x median). 30 / 11 = 2.73 dmg/E, DPS/E = 1.82 (inside 2x threshold).
                MinPower = 10f, MaxPower = 30f, ChargeTime = 2f, ShootCooldown = 1.5f,
                ExplosionRadius = 2f, MaxDamage = 30f, KnockbackForce = 5f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 11f, Ammo = -1
            },
            new WeaponDef
            {
                WeaponId = "shotgun",
                // Balance #197: DPS/E 2.86 overcorrected from #186 — pull back MaxDamage
                // 18->16, EnergyCost 14->16. Burst 64, DPS 35.6, DPS/E 2.22 (within 2x median).
                MinPower = 12f, MaxPower = 25f, ChargeTime = 1f, ShootCooldown = 1.8f,
                ExplosionRadius = 1.2f, MaxDamage = 16f, KnockbackForce = 3f,
                ProjectileCount = 4, SpreadAngle = 25f,
                DestroysIndestructible = false, EnergyCost = 16f, Ammo = -1
            },
            new WeaponDef
            {
                WeaponId = "rocket",
                MinPower = 8f, MaxPower = 35f, ChargeTime = 3f, ShootCooldown = 3f,
                ExplosionRadius = 4f, MaxDamage = 60f, KnockbackForce = 12f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 25f, Ammo = 4
            },
            new WeaponDef
            {
                WeaponId = "cluster",
                // Balance #34: MaxDamage 20 -> 30 and EnergyCost 35 -> 25 to lift DPS/Energy
                // above the 0.5× median outlier threshold (was 0.19, target ~0.45).
                MinPower = 10f, MaxPower = 28f, ChargeTime = 2f, ShootCooldown = 3f,
                ExplosionRadius = 1.5f, MaxDamage = 30f, KnockbackForce = 4f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 25f, Ammo = 4,
                ClusterCount = 4
            },
            new WeaponDef
            {
                WeaponId = "dynamite",
                // Balance #111: MaxDamage 80 -> 70 to bring DPS (was 26.7, now 23.3)
                // closer to the 2x median (20) and reduce outlier DPS/Energy ratio.
                MinPower = 5f, MaxPower = 20f, ChargeTime = 1.5f, ShootCooldown = 3f,
                ExplosionRadius = 5f, MaxDamage = 70f, KnockbackForce = 15f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 20f, Ammo = 3,
                Bounces = 2, FuseTime = 3f
            },
            new WeaponDef
            {
                WeaponId = "napalm",
                MinPower = 8f, MaxPower = 25f, ChargeTime = 2f, ShootCooldown = 4f,
                ExplosionRadius = 3f, MaxDamage = 20f, KnockbackForce = 3f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 30f, Ammo = 2,
                IsNapalm = true, FireZoneDuration = 5f, FireZoneDPS = 15f
            },
            new WeaponDef
            {
                WeaponId = "airstrike",
                // Balance #34: MaxDamage 35 -> 40 (conservative bump, keeps burst at 160
                // which is just above #22's 140 cap without doubling it).
                MinPower = 5f, MaxPower = 20f, ChargeTime = 1.5f, ShootCooldown = 4f,
                ExplosionRadius = 3f, MaxDamage = 40f, KnockbackForce = 8f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 40f, Ammo = 1,
                // Balance #22: AirstrikeCount 5 -> 4 to drop max-burst from 175 -> 140
                IsAirstrike = true, AirstrikeCount = 4
            },
            new WeaponDef
            {
                WeaponId = "drill",
                MinPower = 8f, MaxPower = 15f, ChargeTime = 1f, ShootCooldown = 3f,
                ExplosionRadius = 1.5f, MaxDamage = 40f, KnockbackForce = 3f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 15f, Ammo = 4,
                IsDrill = true, DrillRange = 30f
            },
            new WeaponDef
            {
                WeaponId = "blowtorch",
                MinPower = 5f, MaxPower = 8f, ChargeTime = 0.5f, ShootCooldown = 1f,
                ExplosionRadius = 0.5f, MaxDamage = 10f, KnockbackForce = 1f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 5f, Ammo = -1,
                IsDrill = true, DrillRange = 12f
            },
            new WeaponDef
            {
                WeaponId = "holy_hand_grenade",
                MinPower = 5f, MaxPower = 18f, ChargeTime = 1.5f, ShootCooldown = 4f,
                ExplosionRadius = 8f, MaxDamage = 150f, KnockbackForce = 25f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = true, EnergyCost = 50f, Ammo = 1,
                Bounces = 1, FuseTime = 3f
            },
            new WeaponDef
            {
                WeaponId = "sheep",
                MinPower = 5f, MaxPower = 12f, ChargeTime = 1f, ShootCooldown = 3f,
                ExplosionRadius = 3f, MaxDamage = 60f, KnockbackForce = 10f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 25f, Ammo = 2,
                FuseTime = 5f, IsSheep = true
            },
            new WeaponDef
            {
                WeaponId = "banana_bomb",
                // Balance #22: EnergyCost 30 -> 40 to reflect higher max-burst ceiling.
                // Balance #34: MaxDamage 22 -> 26 (conservative bump; per-shot burst
                //              26 × 6 = 156, just above #22's 132 without reverting the gate).
                MinPower = 10f, MaxPower = 28f, ChargeTime = 2f, ShootCooldown = 4f,
                ExplosionRadius = 2f, MaxDamage = 26f, KnockbackForce = 5f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 40f, Ammo = 1,
                ClusterCount = 6
            },
            new WeaponDef
            {
                WeaponId = "freeze_grenade",
                // Balance #34: EnergyCost 20 -> 12. Utility CC tool was priced as a damage
                // weapon despite dealing only 5 damage — make it affordable as crowd control.
                MinPower = 8f, MaxPower = 22f, ChargeTime = 1.5f, ShootCooldown = 3f,
                ExplosionRadius = 3f, MaxDamage = 5f, KnockbackForce = 2f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 12f, Ammo = 2,
                IsFreeze = true
            },
            new WeaponDef
            {
                WeaponId = "sticky_bomb",
                MinPower = 8f, MaxPower = 25f, ChargeTime = 1.5f, ShootCooldown = 3f,
                ExplosionRadius = 2.5f, MaxDamage = 50f, KnockbackForce = 8f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 20f, Ammo = 3,
                FuseTime = 2f, IsSticky = true
            },
            new WeaponDef
            {
                WeaponId = "lightning_rod",
                MinPower = 10f, MaxPower = 30f, ChargeTime = 2.5f, ShootCooldown = 3.5f,
                ExplosionRadius = 0f, MaxDamage = 40f, KnockbackForce = 2f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 22f, Ammo = 3,
                IsHitscan = true, ChainRange = 6f, ChainDamage = 20f
            },
            new WeaponDef
            {
                WeaponId = "boomerang",
                MinPower = 8f, MaxPower = 22f, ChargeTime = 1.5f, ShootCooldown = 3.0f,
                ExplosionRadius = 1.5f, MaxDamage = 30f, KnockbackForce = 4f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 15f, Ammo = -1,
                IsBoomerang = true
            },
            new WeaponDef
            {
                WeaponId = "gravity_bomb",
                MinPower = 6f, MaxPower = 22f, ChargeTime = 2f, ShootCooldown = 4f,
                ExplosionRadius = 3f, MaxDamage = 65f, KnockbackForce = 10f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 25f, Ammo = 2,
                FuseTime = 2.5f, IsSticky = true, IsGravityBomb = true,
                PullRadius = 6f, PullForce = 9f
            },
            new WeaponDef
            {
                WeaponId = "ricochet_disc",
                // Balance #187/#207: MaxDamage 28, ShootCooldown 3.0->2.5 for DPS 11.2 vs Boomerang 10.0;
                // spammable bounce weapon niche, Boomerang keeps per-hit crown.
                MinPower = 10f, MaxPower = 28f, ChargeTime = 1.5f, ShootCooldown = 2.5f,
                ExplosionRadius = 1.5f, MaxDamage = 28f, KnockbackForce = 3f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 15f, Ammo = -1,
                Bounces = 3, IsRicochet = true
            },
            new WeaponDef
            {
                WeaponId = "magma_ball",
                MinPower = 6f, MaxPower = 20f, ChargeTime = 2f, ShootCooldown = 4f,
                ExplosionRadius = 2.5f, MaxDamage = 25f, KnockbackForce = 5f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 30f, Ammo = 2,
                IsNapalm = true, FireZoneDuration = 6f, FireZoneDPS = 12f,
                IsLavaPool = true, LavaMeltRadius = 4f
            },
            new WeaponDef
            {
                WeaponId = "gust_cannon",
                MinPower = 12f, MaxPower = 25f, ChargeTime = 1f, ShootCooldown = 3f,
                ExplosionRadius = 4f, MaxDamage = 0f, KnockbackForce = 20f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 15f, Ammo = -1,
                IsWindBlast = true
            },
            new WeaponDef
            {
                WeaponId = "harpoon",
                MinPower = 12f, MaxPower = 30f, ChargeTime = 2.5f, ShootCooldown = 3.5f,
                ExplosionRadius = 1.0f, MaxDamage = 40f, KnockbackForce = 6f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 20f, Ammo = 3,
                IsPiercing = true, MaxPierceCount = 1
            },
            new WeaponDef
            {
                WeaponId = "flak_cannon",
                // Balance #87: MaxDamage 20 -> 15, EnergyCost 25 -> 30.
                // 8×15 = 120 max burst between airstrike and HHG tiers.
                // DPS/E from 2.40 to ~1.33, matching peer burst weapons.
                MinPower = 10f, MaxPower = 25f, ChargeTime = 2f, ShootCooldown = 3f,
                ExplosionRadius = 1f, MaxDamage = 15f, KnockbackForce = 3f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 30f, Ammo = 2,
                ClusterCount = 8,
                IsFlak = true, FlakMinDist = 5f, FlakMaxDist = 25f
            }
        };

        public WeaponDef DefaultWeapon => Weapons[0];
    }
}
