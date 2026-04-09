using System.Collections.Generic;

namespace Baboomz.Simulation
{
    public struct MatchStats
    {
        public bool Won;
        public bool Draw;
        public float TotalDamage;
        public int ShotsFired;
        public int DirectHits;
        public float DamageTaken;
        public float MaxSingleDamage;
        public bool LandedFirstBlood;
    }

    public struct MatchXPResult
    {
        public int BaseXP;
        public int BonusXP;
        public int TotalXP;
        public string[] Bonuses;
    }

    public struct RankReward
    {
        public int Rank;
        public string UnlockId;
        public string DisplayName;
    }

    public struct RankUpResult
    {
        public int OldRank;
        public int NewRank;
        public string NewTitle;
        public RankReward[] Unlocks;
    }

    public static class RankSystem
    {
        public static readonly int[] RankThresholds =
        {
            0, 100, 300, 600, 1000, 1500, 2200, 3000, 4000, 5200,
            6600, 8200, 10000, 12000, 14500, 17500, 21000, 25000, 30000, 36000
        };

        public static readonly string[] RankTitles =
        {
            "Recruit", "Cadet", "Gunner", "Bombardier", "Cannoneer",
            "Sergeant", "Lieutenant", "Captain", "Major", "Colonel",
            "Brigadier", "General", "Warlord", "Champion", "Legend",
            "Steampunk Ace", "Sky Marshal", "Grand Artificer", "Baron of Boom", "Supreme Commander"
        };

        public const int MaxRank = 20;

        public static readonly RankReward[] Rewards =
        {
            new RankReward { Rank = 2, UnlockId = "emote_clap", DisplayName = "Clap Emote" },
            new RankReward { Rank = 3, UnlockId = "hat_viking_helmet", DisplayName = "Viking Helmet" },
            new RankReward { Rank = 5, UnlockId = "hat_wizard_hat", DisplayName = "Wizard Hat" },
            new RankReward { Rank = 6, UnlockId = "emote_salute", DisplayName = "Salute Emote" },
            new RankReward { Rank = 8, UnlockId = "hat_samurai_helmet", DisplayName = "Samurai Helmet" },
            new RankReward { Rank = 10, UnlockId = "emote_dance", DisplayName = "Dance Emote" },
            new RankReward { Rank = 12, UnlockId = "hat_dragon_crown", DisplayName = "Dragon Crown" },
            new RankReward { Rank = 15, UnlockId = "emote_flex", DisplayName = "Flex Emote" },
            new RankReward { Rank = 16, UnlockId = "hat_halo", DisplayName = "Halo" },
            new RankReward { Rank = 19, UnlockId = "hat_golden_crown", DisplayName = "Golden Crown" }
        };

        public static int GetRankForXP(int xp)
        {
            for (int i = RankThresholds.Length - 1; i >= 0; i--)
            {
                if (xp >= RankThresholds[i])
                    return i;
            }
            return 0;
        }

        public static string GetRankTitle(int xp)
        {
            return RankTitles[GetRankForXP(xp)];
        }

        public static int GetXPForNextRank(int xp)
        {
            int rank = GetRankForXP(xp);
            if (rank >= RankThresholds.Length - 1)
                return 0; // already max rank
            return RankThresholds[rank + 1] - xp;
        }

        public static RankReward[] GetRewardsForRank(int rank)
        {
            var result = new List<RankReward>();
            for (int i = 0; i < Rewards.Length; i++)
            {
                if (Rewards[i].Rank == rank)
                    result.Add(Rewards[i]);
            }
            return result.ToArray();
        }

        public static RankUpResult CheckRankUp(int oldXP, int newXP)
        {
            int oldRank = GetRankForXP(oldXP);
            int newRank = GetRankForXP(newXP);
            var unlocks = new List<RankReward>();
            for (int r = oldRank + 1; r <= newRank; r++)
            {
                var rewards = GetRewardsForRank(r);
                for (int i = 0; i < rewards.Length; i++)
                    unlocks.Add(rewards[i]);
            }
            return new RankUpResult
            {
                OldRank = oldRank,
                NewRank = newRank,
                NewTitle = RankTitles[newRank],
                Unlocks = unlocks.ToArray()
            };
        }

        public static MatchXPResult AddChallengeXP(MatchXPResult baseResult, ChallengeResult[] challenges)
        {
            var bonuses = new List<string>(baseResult.Bonuses);
            int challengeXP = 0;
            if (challenges != null)
            {
                for (int i = 0; i < challenges.Length; i++)
                {
                    if (challenges[i].Completed)
                    {
                        challengeXP += challenges[i].XPReward;
                        bonuses.Add("Daily: " + challenges[i].ChallengeName);
                    }
                }
            }
            return new MatchXPResult
            {
                BaseXP = baseResult.BaseXP,
                BonusXP = baseResult.BonusXP + challengeXP,
                TotalXP = baseResult.TotalXP + challengeXP,
                Bonuses = bonuses.ToArray()
            };
        }

        public static MatchXPResult CalculateMatchXP(MatchStats stats)
        {
            int baseXP = stats.Won ? 100 : stats.Draw ? 50 : 30;
            int bonusXP = 0;
            var bonuses = new List<string>();

            // Sharpshooter: +20 if accuracy >= 50%
            if (stats.ShotsFired > 0)
            {
                float accuracy = (float)stats.DirectHits / stats.ShotsFired;
                if (accuracy >= 0.5f)
                {
                    bonusXP += 20;
                    bonuses.Add("Sharpshooter");
                }
            }

            // Demolisher: +15 if total damage >= 150
            if (stats.TotalDamage >= 150f)
            {
                bonusXP += 15;
                bonuses.Add("Demolisher");
            }

            // Untouchable: +25 if took <= 30 damage
            if (stats.DamageTaken <= 30f)
            {
                bonusXP += 25;
                bonuses.Add("Untouchable");
            }

            // First Blood: +10 if landed the first hit
            if (stats.LandedFirstBlood)
            {
                bonusXP += 10;
                bonuses.Add("First Blood");
            }

            // Combo King: +15 if best single hit >= 60
            if (stats.MaxSingleDamage >= 60f)
            {
                bonusXP += 15;
                bonuses.Add("Combo King");
            }

            return new MatchXPResult
            {
                BaseXP = baseXP,
                BonusXP = bonusXP,
                TotalXP = baseXP + bonusXP,
                Bonuses = bonuses.ToArray()
            };
        }
    }
}
