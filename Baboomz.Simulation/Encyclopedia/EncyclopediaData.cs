using System.Collections.Generic;

namespace Baboomz.Simulation
{
    public struct EncyclopediaEntry
    {
        public string Id;
        public string Name;
        public string Description;
        public Dictionary<string, string> Stats;
    }

    /// <summary>
    /// Builds encyclopedia entries from GameConfig data (weapons, skills).
    /// Static content (mobs, bosses, biomes, descriptions) lives in EncyclopediaContent.
    /// </summary>
    public static class EncyclopediaData
    {
        public static EncyclopediaEntry[] GetWeaponEntries(GameConfig config)
        {
            var entries = new EncyclopediaEntry[config.Weapons.Length];
            for (int i = 0; i < config.Weapons.Length; i++)
            {
                ref WeaponDef w = ref config.Weapons[i];
                entries[i] = new EncyclopediaEntry
                {
                    Id = w.WeaponId,
                    Name = FormatName(w.WeaponId),
                    Description = EncyclopediaContent.GetWeaponDescription(w.WeaponId),
                    Stats = BuildWeaponStats(ref w)
                };
            }
            return entries;
        }

        public static EncyclopediaEntry[] GetSkillEntries(GameConfig config)
        {
            var entries = new EncyclopediaEntry[config.Skills.Length];
            for (int i = 0; i < config.Skills.Length; i++)
            {
                ref SkillDef s = ref config.Skills[i];
                entries[i] = new EncyclopediaEntry
                {
                    Id = s.SkillId,
                    Name = FormatName(s.SkillId),
                    Description = EncyclopediaContent.GetSkillDescription(s.SkillId),
                    Stats = BuildSkillStats(ref s)
                };
            }
            return entries;
        }

        public static EncyclopediaEntry[] GetMobEntries() => EncyclopediaContent.GetMobEntries();
        public static EncyclopediaEntry[] GetBossEntries() => EncyclopediaContent.GetBossEntries();
        public static EncyclopediaEntry[] GetBiomeEntries() => EncyclopediaContent.GetBiomeEntries();
        public static EncyclopediaEntry[] GetFactionEntries() => EncyclopediaContent.GetFactionEntries();
        public static EncyclopediaEntry[] GetHistoryEntries() => EncyclopediaContent.GetHistoryEntries();

        static Dictionary<string, string> BuildWeaponStats(ref WeaponDef w)
        {
            var stats = new Dictionary<string, string>
            {
                { "Damage", w.MaxDamage.ToString("0") },
                { "Radius", w.ExplosionRadius.ToString("0.#") },
                { "Ammo", w.Ammo < 0 ? "Infinite" : w.Ammo.ToString() },
                { "Energy", w.EnergyCost.ToString("0") }
            };

            if (w.ClusterCount > 0)
                stats["Special"] = $"{w.ClusterCount} sub-projectiles";
            else if (w.IsAirstrike)
                stats["Special"] = $"{w.AirstrikeCount}-bomb airstrike";
            else if (w.IsNapalm)
                stats["Special"] = $"Fire zone ({w.FireZoneDuration}s, {w.FireZoneDPS} DPS)";
            else if (w.IsDrill)
                stats["Special"] = "Tunnels through terrain";
            else if (w.IsSheep)
                stats["Special"] = "Walking projectile";
            else if (w.IsFreeze)
                stats["Special"] = "Freezes targets";
            else if (w.IsSticky)
                stats["Special"] = $"Sticks to surfaces ({w.FuseTime}s fuse)";
            else if (w.IsHitscan)
                stats["Special"] = $"Instant hit, chains {w.ChainRange} range";
            else if (w.IsBoomerang)
                stats["Special"] = "Returns to thrower";
            else if (w.DestroysIndestructible)
                stats["Special"] = "Destroys indestructible terrain";
            else if (w.Bounces > 0)
                stats["Special"] = $"Bounces {w.Bounces}x";
            else if (w.IsPiercing)
                stats["Special"] = "Piercing (passes through 1 target)";
            else if (w.IsFlak)
                stats["Special"] = "Mid-air burst into 8 fragments";

            return stats;
        }

        static Dictionary<string, string> BuildSkillStats(ref SkillDef s)
        {
            var stats = new Dictionary<string, string>
            {
                { "Energy", s.EnergyCost.ToString("0") },
                { "Cooldown", s.Cooldown.ToString("0") + "s" }
            };

            if (s.Duration > 0f)
                stats["Duration"] = s.Duration.ToString("0.#") + "s";

            stats["Effect"] = EncyclopediaContent.GetSkillEffectDescription(s.SkillId, s.Value);
            return stats;
        }

        public static string FormatName(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;

            var chars = new System.Text.StringBuilder(id.Length);
            bool capitalize = true;
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if (c == '_')
                {
                    chars.Append(' ');
                    capitalize = true;
                }
                else if (capitalize)
                {
                    chars.Append(char.ToUpper(c));
                    capitalize = false;
                }
                else
                {
                    chars.Append(c);
                }
            }
            return chars.ToString();
        }
    }
}
