namespace Baboomz.Simulation
{
    public enum MasteryTier
    {
        None,     // 0 XP
        Bronze,   // 100 XP
        Silver,   // 300 XP
        Gold,     // 700 XP
        Diamond,  // 1500 XP
        Master    // 3000 XP
    }

    public struct WeaponMasteryState
    {
        public string WeaponId;
        public int XP;

        public MasteryTier Tier => GetTier(XP);

        public static MasteryTier GetTier(int xp)
        {
            if (xp >= 3000) return MasteryTier.Master;
            if (xp >= 1500) return MasteryTier.Diamond;
            if (xp >= 700) return MasteryTier.Gold;
            if (xp >= 300) return MasteryTier.Silver;
            if (xp >= 100) return MasteryTier.Bronze;
            return MasteryTier.None;
        }

        public static int GetThreshold(MasteryTier tier)
        {
            return tier switch
            {
                MasteryTier.Bronze => 100,
                MasteryTier.Silver => 300,
                MasteryTier.Gold => 700,
                MasteryTier.Diamond => 1500,
                MasteryTier.Master => 3000,
                _ => 0
            };
        }
    }

    /// <summary>
    /// Pure logic for calculating weapon mastery XP earned during a match.
    /// No Unity dependencies — fully testable in EditMode.
    /// </summary>
    public static class WeaponMasteryCalc
    {
        public const int XpPerHit = 10;
        public const int XpPerKill = 50;
        public const int XpPerMatchUsed = 5;

        /// <summary>
        /// Calculate mastery XP earned for a single weapon during a match.
        /// </summary>
        public static int Calculate(int hits, int kills, bool usedInMatch)
        {
            int xp = hits * XpPerHit + kills * XpPerKill;
            if (usedInMatch) xp += XpPerMatchUsed;
            return xp;
        }

        /// <summary>
        /// Returns the damage multiplier bonus for a mastery tier.
        /// Graduated: Bronze +1%, Silver +2%, Gold +3%, Diamond +5%, Master +8%.
        /// </summary>
        public static float GetDamageMultiplier(MasteryTier tier)
        {
            return tier switch
            {
                MasteryTier.Master => 1.08f,
                MasteryTier.Diamond => 1.05f,
                MasteryTier.Gold => 1.03f,
                MasteryTier.Silver => 1.02f,
                MasteryTier.Bronze => 1.01f,
                _ => 1f
            };
        }

        /// <summary>
        /// Returns the projectile speed multiplier for a mastery tier.
        /// Gold +5%, Diamond +10%, Master +15%.
        /// </summary>
        public static float GetSpeedMultiplier(MasteryTier tier)
        {
            return tier switch
            {
                MasteryTier.Master => 1.15f,
                MasteryTier.Diamond => 1.10f,
                MasteryTier.Gold => 1.05f,
                _ => 1f
            };
        }

        /// <summary>
        /// Applies mastery tier bonuses (damage, speed) and weapon-specific mechanical
        /// mods (Silver/Gold) to a weapon slot. Mutates the slot in-place.
        /// </summary>
        public static void ApplyMasteryMods(ref WeaponSlotState slot, MasteryTier tier)
        {
            if (tier == MasteryTier.None || slot.WeaponId == null) return;

            // Graduated damage bonus
            slot.MaxDamage *= GetDamageMultiplier(tier);

            // Silver mod (tier >= Silver)
            if (tier >= MasteryTier.Silver)
                ApplySilverMod(ref slot);

            // Gold mod (tier >= Gold)
            if (tier >= MasteryTier.Gold)
                ApplyGoldMod(ref slot);
        }

        static void ApplySilverMod(ref WeaponSlotState slot)
        {
            switch (slot.WeaponId)
            {
                case "cannon": slot.MinPower *= 1.05f; slot.MaxPower *= 1.05f; break;
                case "rocket": slot.ExplosionRadius += 0.5f; break;
                case "shotgun": slot.ProjectileCount += 1; break;
                case "cluster": slot.ClusterCount += 1; break;
                case "dynamite": slot.FuseTime -= 0.5f; break;
                case "drill": slot.MaxPierceCount += 1; break;
                case "freeze_grenade": break; // +0.5s freeze handled at explosion
                case "lightning_rod": slot.ChainDamage += 10f; break;
                case "boomerang": break; // +5 return speed handled in UpdateBoomerang
                case "harpoon": slot.MaxPierceCount += 1; break;
                // Issue #56: remaining 12 weapons
                case "napalm": slot.FireZoneDuration += 1f; break;
                case "airstrike": slot.AirstrikeCount += 1; break;
                case "blowtorch": slot.DrillRange += 3f; break;
                case "holy_hand_grenade": slot.ExplosionRadius += 1f; break;
                case "sheep": slot.FuseTime += 1f; break;
                case "banana_bomb": slot.ClusterCount += 1; break;
                case "sticky_bomb": slot.FuseTime -= 0.5f; break;
                case "gravity_bomb": slot.PullRadius += 1f; break;
                case "ricochet_disc": slot.Bounces += 1; break;
                case "magma_ball": slot.LavaMeltRadius += 1f; break;
                case "gust_cannon": slot.KnockbackForce += 3f; break;
                case "flak_cannon": slot.ClusterCount += 2; break;
            }
        }

        static void ApplyGoldMod(ref WeaponSlotState slot)
        {
            switch (slot.WeaponId)
            {
                case "cannon": slot.Bounces += 1; break;
                case "rocket": slot.EnergyCost *= 0.9f; break;
                case "shotgun": slot.SpreadAngle *= 0.85f; break;
                case "cluster": slot.MaxDamage += 5f; break;
                case "dynamite": slot.Bounces += 1; break;
                case "drill": slot.MaxDamage += 5f; break;
                case "freeze_grenade": slot.ExplosionRadius += 0.5f; break;
                case "lightning_rod": slot.ChainRange += 3f; break;
                case "boomerang": slot.MaxDamage += 5f; break;
                case "harpoon": slot.MaxDamage += 10f; break;
                // Issue #56: remaining 12 weapons
                case "napalm": slot.FireZoneDPS += 3f; break;
                case "airstrike": slot.EnergyCost -= 5f; break;
                case "blowtorch": slot.MaxDamage += 3f; break;
                case "holy_hand_grenade": slot.KnockbackForce *= 1.2f; break;
                case "sheep": slot.MaxDamage += 10f; break;
                case "banana_bomb": slot.MaxDamage += 4f; break;
                case "sticky_bomb": slot.MaxDamage += 8f; break;
                case "gravity_bomb": slot.PullForce += 5f; break;
                case "ricochet_disc": slot.MaxDamage += 5f; break;
                case "magma_ball": slot.FireZoneDuration += 1f; break;
                case "gust_cannon": slot.ExplosionRadius += 1f; break;
                case "flak_cannon": slot.FlakMaxDist += 5f; break;
            }
        }

        /// <summary>
        /// Returns the freeze duration bonus for freeze_grenade at Silver+ mastery.
        /// Called from ApplyFreezeExplosion.
        /// </summary>
        public static float GetFreezeBonus(string weaponId, MasteryTier tier)
        {
            if (weaponId == "freeze_grenade" && tier >= MasteryTier.Silver)
                return 0.5f;
            return 0f;
        }

        /// <summary>
        /// Returns the boomerang return steer force bonus for Silver+ mastery.
        /// Called from UpdateBoomerang.
        /// </summary>
        public static float GetBoomerangSteerBonus(MasteryTier tier)
        {
            if (tier >= MasteryTier.Silver) return 5f;
            return 0f;
        }
    }
}
