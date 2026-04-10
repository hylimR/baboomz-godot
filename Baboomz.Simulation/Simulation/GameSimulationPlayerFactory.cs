namespace Baboomz.Simulation
{
    /// <summary>
    /// Player factory: builds PlayerState from config (weapons, skills, unlocks, mastery).
    /// Partial class extension of GameSimulation.
    /// </summary>
    public static partial class GameSimulation
    {
        static PlayerState CreatePlayer(GameConfig config, float x, float y, string name, bool isAI,
            int skillSlot0 = -1, int skillSlot1 = -1)
        {
            int slotCount = config.Weapons.Length;
            var slots = new WeaponSlotState[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                var w = config.Weapons[i];
                slots[i] = new WeaponSlotState
                {
                    WeaponId = w.WeaponId, Ammo = w.Ammo,
                    MinPower = w.MinPower, MaxPower = w.MaxPower,
                    ChargeTime = w.ChargeTime, ShootCooldown = w.ShootCooldown,
                    ExplosionRadius = w.ExplosionRadius, MaxDamage = w.MaxDamage,
                    KnockbackForce = w.KnockbackForce, ProjectileCount = w.ProjectileCount,
                    SpreadAngle = w.SpreadAngle, DestroysIndestructible = w.DestroysIndestructible,
                    EnergyCost = w.EnergyCost, Bounces = w.Bounces,
                    FuseTime = w.FuseTime, Bounciness = w.Bounciness,
                    ClusterCount = w.ClusterCount,
                    IsAirstrike = w.IsAirstrike, AirstrikeCount = w.AirstrikeCount,
                    IsNapalm = w.IsNapalm, FireZoneDuration = w.FireZoneDuration,
                    FireZoneDPS = w.FireZoneDPS, IsDrill = w.IsDrill, DrillRange = w.DrillRange,
                    IsSheep = w.IsSheep, IsFreeze = w.IsFreeze,
                    IsSticky = w.IsSticky,
                    IsHitscan = w.IsHitscan, ChainRange = w.ChainRange,
                    ChainDamage = w.ChainDamage,
                    IsBoomerang = w.IsBoomerang,
                    IsGravityBomb = w.IsGravityBomb,
                    PullRadius = w.PullRadius,
                    PullForce = w.PullForce,
                    IsRicochet = w.IsRicochet,
                    IsLavaPool = w.IsLavaPool,
                    LavaMeltRadius = w.LavaMeltRadius,
                    IsWindBlast = w.IsWindBlast,
                    IsPiercing = w.IsPiercing,
                    MaxPierceCount = w.MaxPierceCount,
                    IsFlak = w.IsFlak,
                    FlakMinDist = w.FlakMinDist,
                    FlakMaxDist = w.FlakMaxDist
                };
            }

            // Apply weapon mastery bonuses (damage, speed mods, mechanical unlocks)
            if (config.WeaponMasteryTiers != null)
            {
                for (int i = 0; i < slotCount && i < config.WeaponMasteryTiers.Length; i++)
                    WeaponMasteryCalc.ApplyMasteryMods(ref slots[i], config.WeaponMasteryTiers[i]);
            }

            // Lock weapons the player hasn't unlocked yet (AI always has full access)
            if (!isAI)
            {
                for (int i = 0; i < slotCount; i++)
                    if (slots[i].WeaponId != null && !UnlockRegistry.IsWeaponUnlocked(slots[i].WeaponId, config.UnlockedTier))
                        slots[i].WeaponId = null;
            }

            // Apply weapon loadout: null-out slots not in the selected loadout (players only).
            // AI always keeps all weapons — SelectWeapon references all 22 slot indices.
            if (!isAI && config.PlayerWeaponLoadout != null && config.PlayerWeaponLoadout.Length > 0)
            {
                ApplyWeaponLoadout(slots, config.PlayerWeaponLoadout);
            }

            // Use provided skill indices, falling back to config defaults
            if (skillSlot0 < 0) skillSlot0 = config.DefaultSkillSlot0;
            if (skillSlot1 < 0) skillSlot1 = config.DefaultSkillSlot1;

            // Clamp locked skill selections to valid unlocked indices (AI always has full access)
            if (!isAI)
            {
                if (skillSlot0 >= 0 && !UnlockRegistry.IsSkillIndexUnlocked(skillSlot0, config.UnlockedTier))
                    skillSlot0 = config.DefaultSkillSlot0;
                if (skillSlot1 >= 0 && !UnlockRegistry.IsSkillIndexUnlocked(skillSlot1, config.UnlockedTier))
                    skillSlot1 = config.DefaultSkillSlot1;
            }

            var skillSlots = new SkillSlotState[2];
            int[] skills = { skillSlot0, skillSlot1 };
            for (int i = 0; i < 2; i++)
            {
                int idx = skills[i];
                if (idx >= 0 && idx < config.Skills.Length)
                {
                    var s = config.Skills[idx];
                    skillSlots[i] = new SkillSlotState
                    {
                        SkillId = s.SkillId, Type = s.Type, EnergyCost = s.EnergyCost,
                        Cooldown = s.Cooldown, Duration = s.Duration, Range = s.Range,
                        Value = s.Value
                    };
                }
            }

            return new PlayerState
            {
                Position = new Vec2(x, y), Velocity = Vec2.Zero,
                Health = config.DefaultMaxHealth, MaxHealth = config.DefaultMaxHealth,
                Energy = config.DefaultMaxEnergy, MaxEnergy = config.DefaultMaxEnergy,
                EnergyRegen = config.DefaultEnergyRegen,
                MoveSpeed = config.DefaultMoveSpeed * config.MoveSpeedMult, JumpForce = config.DefaultJumpForce,
                AimAngle = 45f, AimPower = 0f, ShootCooldownRemaining = 0f,
                DamageMultiplier = config.DefaultDamageMultiplier,
                ArmorMultiplier = config.DefaultArmorMultiplier,
                CooldownMultiplier = config.DefaultCooldownMultiplier,
                IsGrounded = false, IsDead = false, IsAI = isAI, IsCharging = false,
                ActiveWeaponSlot = 0, FacingDirection = isAI ? -1 : 1,
                Name = name, LastGroundedY = y, TeamIndex = -1,
                WeaponSlots = slots, SkillSlots = skillSlots
            };
        }
    }
}
