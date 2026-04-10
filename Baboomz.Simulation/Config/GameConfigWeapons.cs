namespace Baboomz.Simulation
{
    public partial class GameConfig
    {
        public WeaponDef[] Weapons = new[]
        {
            new WeaponDef
            {
                WeaponId = "cannon",
                MinPower = 10f, MaxPower = 30f, ChargeTime = 2f, ShootCooldown = 1.5f,
                ExplosionRadius = 2f, MaxDamage = 30f, KnockbackForce = 5f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 8f, Ammo = -1
            },
            new WeaponDef
            {
                WeaponId = "shotgun",
                MinPower = 12f, MaxPower = 25f, ChargeTime = 1f, ShootCooldown = 2f,
                ExplosionRadius = 1.2f, MaxDamage = 15f, KnockbackForce = 3f,
                ProjectileCount = 4, SpreadAngle = 25f,
                DestroysIndestructible = false, EnergyCost = 18f, Ammo = -1
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
                MinPower = 10f, MaxPower = 28f, ChargeTime = 2f, ShootCooldown = 3f,
                ExplosionRadius = 1.5f, MaxDamage = 20f, KnockbackForce = 4f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 35f, Ammo = 4,
                ClusterCount = 4
            },
            new WeaponDef
            {
                WeaponId = "dynamite",
                MinPower = 5f, MaxPower = 20f, ChargeTime = 1.5f, ShootCooldown = 3f,
                ExplosionRadius = 5f, MaxDamage = 80f, KnockbackForce = 15f,
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
                MinPower = 5f, MaxPower = 20f, ChargeTime = 1.5f, ShootCooldown = 4f,
                ExplosionRadius = 3f, MaxDamage = 35f, KnockbackForce = 8f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 40f, Ammo = 1,
                IsAirstrike = true, AirstrikeCount = 5
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
                MinPower = 10f, MaxPower = 28f, ChargeTime = 2f, ShootCooldown = 3f,
                ExplosionRadius = 2f, MaxDamage = 22f, KnockbackForce = 5f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 30f, Ammo = 1,
                ClusterCount = 6
            },
            new WeaponDef
            {
                WeaponId = "freeze_grenade",
                MinPower = 8f, MaxPower = 22f, ChargeTime = 1.5f, ShootCooldown = 3f,
                ExplosionRadius = 3f, MaxDamage = 5f, KnockbackForce = 2f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 20f, Ammo = 2,
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
                MinPower = 10f, MaxPower = 30f, ChargeTime = 2.5f, ShootCooldown = 4f,
                ExplosionRadius = 0f, MaxDamage = 40f, KnockbackForce = 2f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 30f, Ammo = 3,
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
                MinPower = 10f, MaxPower = 28f, ChargeTime = 1.5f, ShootCooldown = 3f,
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
                MinPower = 10f, MaxPower = 25f, ChargeTime = 2f, ShootCooldown = 4f,
                ExplosionRadius = 1f, MaxDamage = 10f, KnockbackForce = 3f,
                ProjectileCount = 1, SpreadAngle = 0f,
                DestroysIndestructible = false, EnergyCost = 25f, Ammo = 2,
                ClusterCount = 8,
                IsFlak = true, FlakMinDist = 5f, FlakMaxDist = 25f
            }
        };

        public WeaponDef DefaultWeapon => Weapons[0];
    }
}
