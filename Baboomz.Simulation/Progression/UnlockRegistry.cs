using System.Collections.Generic;

namespace Baboomz.Simulation
{
    /// <summary>
    /// Pure C# unlock progression system. Determines which weapons and skills
    /// are available based on the player's win count (persisted externally).
    /// 5 tiers: Starter (0 wins) through Legend (50 wins).
    /// </summary>
    public static class UnlockRegistry
    {
        public static readonly string[] TierNames = { "Starter", "Veteran", "Expert", "Master", "Legend" };
        public static readonly int[] TierWinThresholds = { 0, 5, 15, 30, 50 };

        // Weapons unlocked at each tier (cumulative)
        static readonly string[][] WeaponsByTier = new[]
        {
            // Tier 0 — Starter
            new[] { "cannon", "rocket", "dynamite", "shotgun", "drill" },
            // Tier 1 — Veteran
            new[] { "cluster", "napalm", "lightning_rod", "boomerang" },
            // Tier 2 — Expert
            new[] { "banana_bomb", "airstrike", "holy_hand_grenade", "gravity_bomb" },
            // Tier 3 — Master
            new[] { "flak_cannon", "harpoon", "ricochet_disc", "magma_ball", "gust_cannon" },
            // Tier 4 — Legend
            new[] { "freeze_grenade", "sticky_bomb", "sheep", "blowtorch" }
        };

        // Skill indices unlocked at each tier (cumulative, into GameConfig.Skills array)
        static readonly int[][] SkillIndicesByTier = new[]
        {
            // Tier 0 — teleport(0), grapple(1), shield(2), dash(3), heal(4), jetpack(5)
            new[] { 0, 1, 2, 3, 4, 5 },
            // Tier 1 — girder(6), earthquake(7), smoke(8), landslide(20), sprint(21)
            new[] { 6, 7, 8, 20, 21 },
            // Tier 2 — warcry(9), mine_layer(10), energy_drain(11)
            new[] { 9, 10, 11 },
            // Tier 3 — deflect(12), decoy(13), hookshot(14), magnetic_mine(18), petrify(19)
            new[] { 12, 13, 14, 18, 19 },
            // Tier 4 — shadow_step(15), overcharge(16), mend(17)
            new[] { 15, 16, 17 }
        };

        public static int GetTier(int wins)
        {
            int tier = 0;
            for (int i = 1; i < TierWinThresholds.Length; i++)
            {
                if (wins >= TierWinThresholds[i])
                    tier = i;
            }
            return tier;
        }

        public static string GetTierName(int tier)
        {
            if (tier < 0 || tier >= TierNames.Length) return TierNames[0];
            return TierNames[tier];
        }

        public static int GetWinsForNextTier(int wins)
        {
            int currentTier = GetTier(wins);
            if (currentTier >= TierWinThresholds.Length - 1) return 0; // max tier
            return TierWinThresholds[currentTier + 1] - wins;
        }

        public static bool IsWeaponUnlocked(string weaponId, int tier)
        {
            for (int t = 0; t <= tier && t < WeaponsByTier.Length; t++)
            {
                var weapons = WeaponsByTier[t];
                for (int i = 0; i < weapons.Length; i++)
                    if (weapons[i] == weaponId) return true;
            }
            return false;
        }

        public static bool IsSkillIndexUnlocked(int skillIndex, int tier)
        {
            for (int t = 0; t <= tier && t < SkillIndicesByTier.Length; t++)
            {
                var indices = SkillIndicesByTier[t];
                for (int i = 0; i < indices.Length; i++)
                    if (indices[i] == skillIndex) return true;
            }
            return false;
        }

        public static List<string> GetUnlockedWeaponIds(int tier)
        {
            var result = new List<string>();
            for (int t = 0; t <= tier && t < WeaponsByTier.Length; t++)
                result.AddRange(WeaponsByTier[t]);
            return result;
        }

        public static List<int> GetUnlockedSkillIndices(int tier)
        {
            var result = new List<int>();
            for (int t = 0; t <= tier && t < SkillIndicesByTier.Length; t++)
                result.AddRange(SkillIndicesByTier[t]);
            return result;
        }
    }
}
