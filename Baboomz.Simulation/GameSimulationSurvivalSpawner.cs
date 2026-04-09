using System;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Survival mode mob/boss entity creation and type selection.
    /// Partial class extension of GameSimulation.
    /// </summary>
    public static partial class GameSimulation
    {
        static readonly string[] BossRotation =
        {
            "iron_sentinel", "sand_wyrm", "glacial_cannon",
            "forge_colossus", "baron_cogsworth"
        };

        static readonly string[] MobTypeRotation =
        {
            "walker", "turret", "bomber", "shielder", "flyer", "healer"
        };

        static string PickMobType(int wave, Random rng)
        {
            // Waves 1-4: walkers only
            if (wave <= 4) return "walker";
            // Waves 6-9: walkers + turrets
            if (wave <= 9)
            {
                int pick = rng.Next(2); // 0=walker, 1=turret
                return MobTypeRotation[pick];
            }
            // Waves 11+: all types (excluding healer until wave 15+)
            int maxType = wave >= 15 ? MobTypeRotation.Length : MobTypeRotation.Length - 1;
            return MobTypeRotation[rng.Next(maxType)];
        }

        static PlayerState CreateSurvivalMob(GameConfig config, float x, float y,
            int wave, float speedMult, float hpMult, Random rng)
        {
            string mobType = PickMobType(wave, rng);

            // Find MobDef or use walker defaults
            float baseHP = 30f;
            float baseSpeed = 3f;
            float baseDamage = 15f;
            foreach (var def in config.MobTypes)
            {
                if (def.MobType == mobType)
                {
                    baseHP = def.Health;
                    baseSpeed = def.Speed;
                    baseDamage = def.Damage;
                    break;
                }
            }

            float hp = baseHP * hpMult;
            float speed = baseSpeed * speedMult;

            // Give mob a single cannon weapon
            var slots = new WeaponSlotState[1];
            slots[0] = new WeaponSlotState
            {
                WeaponId = "cannon",
                Ammo = -1,
                MinPower = 10f,
                MaxPower = 25f,
                ChargeTime = 1f,
                ShootCooldown = 2f,
                ExplosionRadius = 1.5f,
                MaxDamage = baseDamage,
                KnockbackForce = 5f,
                ProjectileCount = 1
            };

            return new PlayerState
            {
                Position = new Vec2(x, y),
                Velocity = Vec2.Zero,
                Health = hp, MaxHealth = hp,
                Energy = 0f, MaxEnergy = 0f, EnergyRegen = 0f,
                MoveSpeed = speed,
                JumpForce = 8f,
                AimAngle = 45f, AimPower = 0f,
                DamageMultiplier = 1f, ArmorMultiplier = 1f, CooldownMultiplier = 1f,
                IsAI = true, IsMob = true, MobType = mobType,
                FacingDirection = x > 0f ? -1 : 1,
                Name = mobType,
                TeamIndex = -1,
                WeaponSlots = slots,
                SkillSlots = new SkillSlotState[0]
            };
        }

        static PlayerState CreateSurvivalBoss(GameConfig config, float x, float y,
            int wave, float hpMult, Random rng)
        {
            // Rotate bosses based on wave: wave 5->0, 10->1, 15->2, 20->3, 25->4, 30->0 ...
            int bossIdx = ((wave / Math.Max(config.SurvivalBossInterval, 1)) - 1) % BossRotation.Length;
            if (bossIdx < 0) bossIdx = 0;
            string bossType = BossRotation[bossIdx];

            float bossHP = 200f * hpMult;

            var slots = new WeaponSlotState[1];
            slots[0] = new WeaponSlotState
            {
                WeaponId = "cannon",
                Ammo = -1,
                MinPower = 10f,
                MaxPower = 30f,
                ChargeTime = 1f,
                ShootCooldown = 2f,
                ExplosionRadius = 2.5f,
                MaxDamage = 40f,
                KnockbackForce = 8f,
                ProjectileCount = 1
            };

            return new PlayerState
            {
                Position = new Vec2(x, y),
                Velocity = Vec2.Zero,
                Health = bossHP, MaxHealth = bossHP,
                Energy = 100f, MaxEnergy = 100f, EnergyRegen = 5f,
                MoveSpeed = 2f,
                JumpForce = 8f,
                AimAngle = 45f, AimPower = 0f,
                DamageMultiplier = 1f, ArmorMultiplier = 1f, CooldownMultiplier = 1f,
                IsAI = true, IsMob = true, MobType = "boss",
                BossType = bossType,
                FacingDirection = x > 0f ? -1 : 1,
                Name = bossType,
                TeamIndex = -1,
                WeaponSlots = slots,
                SkillSlots = new SkillSlotState[0]
            };
        }
    }
}
