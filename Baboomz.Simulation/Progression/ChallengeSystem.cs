using System.Collections.Generic;

namespace Baboomz.Simulation
{
    public enum ChallengeCategory
    {
        Weapon, Destruction, Accuracy, Defense, Speed, Skill, Restriction, Combo, Endurance, MultiMatch
    }

    public struct ChallengeDef
    {
        public int Id;
        public string Name;
        public int XPReward;
        public ChallengeCategory Category;
    }

    public struct ChallengeResult
    {
        public int ChallengeId;
        public string ChallengeName;
        public int XPReward;
        public bool Completed;
    }

    public struct MatchChallengeStats
    {
        public bool Won;
        public float MatchTime;
        public int ShotsFired;
        public int DirectHits;
        public float DamageTaken;
        public float TotalDamage;
        public bool SuddenDeathOccurred;
        public Dictionary<string, int> WeaponHits;
        public Dictionary<string, int> WeaponKills;
        public Dictionary<string, float> WeaponDamage;
        public int DistinctSkillsUsed;
        public bool AnySkillActivated;
        public int TerrainPixelsDestroyed;
        public bool HitWhileJetpacking;
        public float ShieldDamageBlocked;
        public bool FreezeToHitCombo;
        public bool GravityBombVoidKill;
        public int ChainLightningMaxTargets;
        public int ClusterBananaSubHits;
        public int FirstStrikesToday;
        public int ConsecutiveWinsToday;
        public int TotalKills;
        public int CloseRangeKills;
    }

    public static class ChallengeSystem
    {
        public const int ChallengesPerDay = 3;

        public static readonly ChallengeDef[] Pool =
        {
            new ChallengeDef { Id = 1,  Name = "Cannon Specialist",  XPReward = 40, Category = ChallengeCategory.Weapon },
            new ChallengeDef { Id = 2,  Name = "Rocket Barrage",     XPReward = 35, Category = ChallengeCategory.Weapon },
            new ChallengeDef { Id = 3,  Name = "Drill Sergeant",     XPReward = 45, Category = ChallengeCategory.Weapon },
            new ChallengeDef { Id = 4,  Name = "Boom Boom",          XPReward = 30, Category = ChallengeCategory.Destruction },
            new ChallengeDef { Id = 5,  Name = "Chain Lightning",    XPReward = 50, Category = ChallengeCategory.Weapon },
            new ChallengeDef { Id = 6,  Name = "Sheep Herder",       XPReward = 45, Category = ChallengeCategory.Weapon },
            new ChallengeDef { Id = 7,  Name = "Sharpshooter Elite", XPReward = 40, Category = ChallengeCategory.Accuracy },
            new ChallengeDef { Id = 8,  Name = "Untouchable Pro",    XPReward = 50, Category = ChallengeCategory.Defense },
            new ChallengeDef { Id = 9,  Name = "Speed Demon",        XPReward = 50, Category = ChallengeCategory.Speed },
            new ChallengeDef { Id = 10, Name = "Skill Master",       XPReward = 35, Category = ChallengeCategory.Skill },
            new ChallengeDef { Id = 11, Name = "Jetpack Ace",        XPReward = 45, Category = ChallengeCategory.Skill },
            new ChallengeDef { Id = 12, Name = "Shield Wall",        XPReward = 40, Category = ChallengeCategory.Skill },
            new ChallengeDef { Id = 13, Name = "No Skills Allowed",  XPReward = 40, Category = ChallengeCategory.Restriction },
            new ChallengeDef { Id = 14, Name = "Close Quarters",     XPReward = 50, Category = ChallengeCategory.Weapon },
            new ChallengeDef { Id = 15, Name = "Bombardier",         XPReward = 35, Category = ChallengeCategory.Weapon },
            new ChallengeDef { Id = 16, Name = "Freeze Tag",         XPReward = 45, Category = ChallengeCategory.Combo },
            new ChallengeDef { Id = 17, Name = "Gravity Master",     XPReward = 50, Category = ChallengeCategory.Weapon },
            new ChallengeDef { Id = 18, Name = "First Strike",       XPReward = 30, Category = ChallengeCategory.MultiMatch },
            new ChallengeDef { Id = 19, Name = "Survivor",           XPReward = 40, Category = ChallengeCategory.Endurance },
            new ChallengeDef { Id = 20, Name = "Win Streak",         XPReward = 75, Category = ChallengeCategory.MultiMatch },
        };

        public static int GetDateSeed(int year, int month, int day)
        {
            return year * 10000 + month * 100 + day;
        }

        public static ChallengeDef[] GetDailyChallenges(int year, int month, int day)
        {
            int seed = GetDateSeed(year, month, day);
            var indices = new int[Pool.Length];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;

            var rng = new System.Random(seed);
            for (int i = indices.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = indices[i];
                indices[i] = indices[j];
                indices[j] = tmp;
            }

            return new[] { Pool[indices[0]], Pool[indices[1]], Pool[indices[2]] };
        }

        public static ChallengeResult[] EvaluateChallenges(ChallengeDef[] challenges, MatchChallengeStats stats)
        {
            var results = new ChallengeResult[challenges.Length];
            for (int i = 0; i < challenges.Length; i++)
            {
                results[i] = new ChallengeResult
                {
                    ChallengeId = challenges[i].Id,
                    ChallengeName = challenges[i].Name,
                    XPReward = challenges[i].XPReward,
                    Completed = IsCompleted(challenges[i].Id, stats)
                };
            }
            return results;
        }

        static float GetWeaponDamage(MatchChallengeStats s, string id)
        {
            return s.WeaponDamage != null && s.WeaponDamage.ContainsKey(id) ? s.WeaponDamage[id] : 0f;
        }

        static int GetWeaponHits(MatchChallengeStats s, string id)
        {
            return s.WeaponHits != null && s.WeaponHits.ContainsKey(id) ? s.WeaponHits[id] : 0;
        }

        static int GetWeaponKills(MatchChallengeStats s, string id)
        {
            return s.WeaponKills != null && s.WeaponKills.ContainsKey(id) ? s.WeaponKills[id] : 0;
        }

        static bool IsCompleted(int id, MatchChallengeStats s)
        {
            switch (id)
            {
                case 1: // Cannon Specialist: win with 80%+ damage from cannon
                    return s.Won && s.TotalDamage > 0f && GetWeaponDamage(s, "cannon") / s.TotalDamage >= 0.8f;
                case 2: // Rocket Barrage: 200+ damage with rocket
                    return GetWeaponDamage(s, "rocket") >= 200f;
                case 3: // Drill Sergeant: 3+ hits with drill
                    return GetWeaponHits(s, "drill") >= 3;
                case 4: // Boom Boom: 1000+ terrain pixels destroyed
                    return s.TerrainPixelsDestroyed >= 1000;
                case 5: // Chain Lightning: 2+ chain targets
                    return s.ChainLightningMaxTargets >= 2;
                case 6: // Sheep Herder: kill with sheep
                    return GetWeaponKills(s, "sheep") >= 1;
                case 7: // Sharpshooter Elite: 60%+ accuracy, min 5 shots
                    return s.ShotsFired >= 5 && (float)s.DirectHits / s.ShotsFired >= 0.6f;
                case 8: // Untouchable Pro: win with ≤20 damage taken
                    return s.Won && s.DamageTaken <= 20f;
                case 9: // Speed Demon: win under 45 seconds
                    return s.Won && s.MatchTime < 45f;
                case 10: // Skill Master: 5+ different skills used
                    return s.DistinctSkillsUsed >= 5;
                case 11: // Jetpack Ace: hit enemy while airborne from jetpack
                    return s.HitWhileJetpacking;
                case 12: // Shield Wall: block 80+ damage with shield
                    return s.ShieldDamageBlocked >= 80f;
                case 13: // No Skills Allowed: win without activating any skill
                    return s.Won && !s.AnySkillActivated;
                case 14: // Close Quarters: win where all kills ≤5 unit range
                    return s.Won && s.TotalKills > 0 && s.CloseRangeKills >= s.TotalKills;
                case 15: // Bombardier: 3+ cluster/banana sub-projectile hits
                    return s.ClusterBananaSubHits >= 3;
                case 16: // Freeze Tag: freeze then hit within 2s
                    return s.FreezeToHitCombo;
                case 17: // Gravity Master: kill with gravity bomb pull into void
                    return s.GravityBombVoidKill;
                case 18: // First Strike: land first hit 3 times today
                    return s.FirstStrikesToday >= 3;
                case 19: // Survivor: win match that enters sudden death
                    return s.Won && s.SuddenDeathOccurred;
                case 20: // Win Streak: 3 consecutive wins today
                    return s.ConsecutiveWinsToday >= 3;
                default:
                    return false;
            }
        }

        public static MatchChallengeStats BuildStats(GameState state, int playerIndex)
        {
            var p = state.Players[playerIndex];
            var stats = new MatchChallengeStats
            {
                Won = state.WinnerIndex == playerIndex,
                MatchTime = state.Time,
                ShotsFired = p.ShotsFired,
                DirectHits = p.DirectHits,
                DamageTaken = p.TotalDamageTaken,
                TotalDamage = p.TotalDamageDealt,
                SuddenDeathOccurred = state.SuddenDeathActive,
                TotalKills = p.TotalKills,
                CloseRangeKills = p.CloseRangeKills,
            };

            if (state.WeaponHits != null && playerIndex < state.WeaponHits.Length)
                stats.WeaponHits = state.WeaponHits[playerIndex];
            if (state.WeaponKills != null && playerIndex < state.WeaponKills.Length)
                stats.WeaponKills = state.WeaponKills[playerIndex];
            if (state.WeaponDamage != null && playerIndex < state.WeaponDamage.Length)
                stats.WeaponDamage = state.WeaponDamage[playerIndex];
            if (state.SkillsActivated != null && playerIndex < state.SkillsActivated.Length)
            {
                stats.DistinctSkillsUsed = state.SkillsActivated[playerIndex].Count;
                stats.AnySkillActivated = state.SkillsActivated[playerIndex].Count > 0;
            }

            // Challenge-specific stats from PlayerState
            stats.TerrainPixelsDestroyed = p.TerrainPixelsDestroyed;
            stats.ChainLightningMaxTargets = p.ChainLightningTargets;
            stats.HitWhileJetpacking = p.HitWhileJetpacking;
            stats.ShieldDamageBlocked = p.ShieldDamageBlocked;
            stats.FreezeToHitCombo = p.FreezeToHitCombo;
            stats.GravityBombVoidKill = p.GravityBombVoidKill;

            // ClusterBananaSubHits: sum hits from cluster and banana_bomb weapons
            stats.ClusterBananaSubHits = GetWeaponHits(stats, "cluster") + GetWeaponHits(stats, "banana_bomb");

            return stats;
        }
    }
}
