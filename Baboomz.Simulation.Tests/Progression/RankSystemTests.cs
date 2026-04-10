using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests
{
    [TestFixture]
    public class RankSystemTests
    {
        // --- Rank Thresholds ---

        [Test]
        public void GetRankForXP_ZeroXP_ReturnsRank0()
        {
            Assert.AreEqual(0, RankSystem.GetRankForXP(0));
        }

        [Test]
        public void GetRankForXP_99XP_ReturnsRank0()
        {
            Assert.AreEqual(0, RankSystem.GetRankForXP(99));
        }

        [Test]
        public void GetRankForXP_100XP_ReturnsRank1()
        {
            Assert.AreEqual(1, RankSystem.GetRankForXP(100));
        }

        [Test]
        public void GetRankForXP_36000XP_ReturnsMaxRank()
        {
            Assert.AreEqual(19, RankSystem.GetRankForXP(36000));
        }

        [Test]
        public void GetRankForXP_999999XP_ReturnsMaxRank()
        {
            Assert.AreEqual(19, RankSystem.GetRankForXP(999999));
        }

        [Test]
        public void GetRankForXP_1000XP_ReturnsRank4_Cannoneer()
        {
            Assert.AreEqual(4, RankSystem.GetRankForXP(1000));
        }

        // --- Rank Titles ---

        [Test]
        public void GetRankTitle_ZeroXP_ReturnsRecruit()
        {
            Assert.AreEqual("Recruit", RankSystem.GetRankTitle(0));
        }

        [Test]
        public void GetRankTitle_100XP_ReturnsCadet()
        {
            Assert.AreEqual("Cadet", RankSystem.GetRankTitle(100));
        }

        [Test]
        public void GetRankTitle_36000XP_ReturnsSupremeCommander()
        {
            Assert.AreEqual("Supreme Commander", RankSystem.GetRankTitle(36000));
        }

        [Test]
        public void GetRankTitle_10000XP_ReturnsWarlord()
        {
            Assert.AreEqual("Warlord", RankSystem.GetRankTitle(10000));
        }

        // --- XP For Next Rank ---

        [Test]
        public void GetXPForNextRank_ZeroXP_Returns100()
        {
            Assert.AreEqual(100, RankSystem.GetXPForNextRank(0));
        }

        [Test]
        public void GetXPForNextRank_50XP_Returns50()
        {
            Assert.AreEqual(50, RankSystem.GetXPForNextRank(50));
        }

        [Test]
        public void GetXPForNextRank_MaxRank_ReturnsZero()
        {
            Assert.AreEqual(0, RankSystem.GetXPForNextRank(36000));
        }

        [Test]
        public void GetXPForNextRank_BeyondMax_ReturnsZero()
        {
            Assert.AreEqual(0, RankSystem.GetXPForNextRank(100000));
        }

        // --- Match XP Calculation: Base ---

        [Test]
        public void CalculateMatchXP_Win_Returns100Base()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { Won = true });
            Assert.AreEqual(100, result.BaseXP);
        }

        [Test]
        public void CalculateMatchXP_Loss_Returns30Base()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats());
            Assert.AreEqual(30, result.BaseXP);
        }

        [Test]
        public void CalculateMatchXP_Draw_Returns50Base()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { Draw = true });
            Assert.AreEqual(50, result.BaseXP);
        }

        // --- Sharpshooter Bonus ---

        [Test]
        public void CalculateMatchXP_Sharpshooter_50PercentAccuracy()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats
            {
                ShotsFired = 10, DirectHits = 5, DamageTaken = 100f
            });
            Assert.AreEqual(20, result.BonusXP);
            Assert.Contains("Sharpshooter", result.Bonuses);
        }

        [Test]
        public void CalculateMatchXP_NoSharpshooter_Below50Percent()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats
            {
                ShotsFired = 10, DirectHits = 4
            });
            Assert.IsFalse(System.Array.IndexOf(result.Bonuses, "Sharpshooter") >= 0);
        }

        [Test]
        public void CalculateMatchXP_NoSharpshooter_ZeroShots()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats());
            Assert.IsFalse(System.Array.IndexOf(result.Bonuses, "Sharpshooter") >= 0);
        }

        // --- Demolisher Bonus ---

        [Test]
        public void CalculateMatchXP_Demolisher_150Damage()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { TotalDamage = 150f });
            Assert.IsTrue(System.Array.IndexOf(result.Bonuses, "Demolisher") >= 0);
        }

        [Test]
        public void CalculateMatchXP_NoDemolisher_149Damage()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { TotalDamage = 149f });
            Assert.IsFalse(System.Array.IndexOf(result.Bonuses, "Demolisher") >= 0);
        }

        // --- Untouchable Bonus ---

        [Test]
        public void CalculateMatchXP_Untouchable_ZeroDamageTaken()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { DamageTaken = 0f });
            Assert.IsTrue(System.Array.IndexOf(result.Bonuses, "Untouchable") >= 0);
        }

        [Test]
        public void CalculateMatchXP_Untouchable_30DamageTaken()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { DamageTaken = 30f });
            Assert.IsTrue(System.Array.IndexOf(result.Bonuses, "Untouchable") >= 0);
        }

        [Test]
        public void CalculateMatchXP_NoUntouchable_31DamageTaken()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { DamageTaken = 31f });
            Assert.IsFalse(System.Array.IndexOf(result.Bonuses, "Untouchable") >= 0);
        }

        // --- First Blood Bonus ---

        [Test]
        public void CalculateMatchXP_FirstBlood_Grants10()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { LandedFirstBlood = true, DamageTaken = 100f });
            Assert.IsTrue(System.Array.IndexOf(result.Bonuses, "First Blood") >= 0);
            // Base 30 (loss) + 10 (first blood) = 40
            Assert.AreEqual(10, result.BonusXP - 0); // isolate: only first blood, damage taken > 30 removes untouchable
        }

        // --- Combo King Bonus ---

        [Test]
        public void CalculateMatchXP_ComboKing_60MaxDamage()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { MaxSingleDamage = 60f, DamageTaken = 100f });
            Assert.IsTrue(System.Array.IndexOf(result.Bonuses, "Combo King") >= 0);
        }

        [Test]
        public void CalculateMatchXP_NoComboKing_59MaxDamage()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats { MaxSingleDamage = 59f, DamageTaken = 100f });
            Assert.IsFalse(System.Array.IndexOf(result.Bonuses, "Combo King") >= 0);
        }

        // --- All Bonuses Combined ---

        [Test]
        public void CalculateMatchXP_AllBonuses_MaxPossible()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats
            {
                Won = true,
                TotalDamage = 200f,
                ShotsFired = 10,
                DirectHits = 8,
                DamageTaken = 0f,
                MaxSingleDamage = 80f,
                LandedFirstBlood = true
            });
            // Base: 100 + Sharpshooter: 20 + Demolisher: 15 + Untouchable: 25 + First Blood: 10 + Combo King: 15 = 185
            Assert.AreEqual(100, result.BaseXP);
            Assert.AreEqual(85, result.BonusXP);
            Assert.AreEqual(185, result.TotalXP);
            Assert.AreEqual(5, result.Bonuses.Length);
        }

        [Test]
        public void CalculateMatchXP_NoBonuses_MinPossible()
        {
            var result = RankSystem.CalculateMatchXP(new MatchStats
            {
                Won = false,
                Draw = false,
                TotalDamage = 0f,
                ShotsFired = 0,
                DirectHits = 0,
                DamageTaken = 100f,
                MaxSingleDamage = 0f,
                LandedFirstBlood = false
            });
            Assert.AreEqual(30, result.BaseXP);
            Assert.AreEqual(0, result.BonusXP);
            Assert.AreEqual(30, result.TotalXP);
            Assert.AreEqual(0, result.Bonuses.Length);
        }

        // --- Rank Table Consistency ---

        [Test]
        public void RankThresholds_Has20Entries()
        {
            Assert.AreEqual(20, RankSystem.RankThresholds.Length);
        }

        [Test]
        public void RankTitles_Has20Entries()
        {
            Assert.AreEqual(20, RankSystem.RankTitles.Length);
        }

        [Test]
        public void RankThresholds_AreStrictlyIncreasing()
        {
            for (int i = 1; i < RankSystem.RankThresholds.Length; i++)
                Assert.Greater(RankSystem.RankThresholds[i], RankSystem.RankThresholds[i - 1],
                    $"Threshold {i} must be greater than {i - 1}");
        }

        [Test]
        public void RankThresholds_StartsAtZero()
        {
            Assert.AreEqual(0, RankSystem.RankThresholds[0]);
        }

        // --- Rank Rewards ---

        [Test]
        public void Rewards_Has10Entries()
        {
            Assert.AreEqual(10, RankSystem.Rewards.Length);
        }

        [Test]
        public void Rewards_AllRanksWithinValid()
        {
            for (int i = 0; i < RankSystem.Rewards.Length; i++)
                Assert.IsTrue(RankSystem.Rewards[i].Rank >= 0 && RankSystem.Rewards[i].Rank < RankSystem.MaxRank,
                    $"Reward {i} rank {RankSystem.Rewards[i].Rank} out of range");
        }

        [Test]
        public void GetRewardsForRank_Rank3_ReturnsVikingHelmet()
        {
            var rewards = RankSystem.GetRewardsForRank(3);
            Assert.AreEqual(1, rewards.Length);
            Assert.AreEqual("hat_viking_helmet", rewards[0].UnlockId);
        }

        [Test]
        public void GetRewardsForRank_Rank0_ReturnsEmpty()
        {
            var rewards = RankSystem.GetRewardsForRank(0);
            Assert.AreEqual(0, rewards.Length);
        }

        [Test]
        public void GetRewardsForRank_Rank19_ReturnsGoldenCrown()
        {
            var rewards = RankSystem.GetRewardsForRank(19);
            Assert.AreEqual(1, rewards.Length);
            Assert.AreEqual("hat_golden_crown", rewards[0].UnlockId);
        }

        [Test]
        public void CheckRankUp_NoRankChange_ReturnsEmptyUnlocks()
        {
            var result = RankSystem.CheckRankUp(0, 50);
            Assert.AreEqual(0, result.OldRank);
            Assert.AreEqual(0, result.NewRank);
            Assert.AreEqual(0, result.Unlocks.Length);
        }

        [Test]
        public void CheckRankUp_SingleRankUp_ReturnsNewTitle()
        {
            var result = RankSystem.CheckRankUp(90, 110);
            Assert.AreEqual(0, result.OldRank);
            Assert.AreEqual(1, result.NewRank);
            Assert.AreEqual("Cadet", result.NewTitle);
        }

        [Test]
        public void CheckRankUp_ToRank2_UnlocksClapEmote()
        {
            var result = RankSystem.CheckRankUp(250, 310);
            Assert.AreEqual(1, result.OldRank);
            Assert.AreEqual(2, result.NewRank);
            Assert.AreEqual(1, result.Unlocks.Length);
            Assert.AreEqual("emote_clap", result.Unlocks[0].UnlockId);
        }

        [Test]
        public void CheckRankUp_MultipleRanks_CollectsAllUnlocks()
        {
            // Jump from rank 1 (100 XP) to rank 3 (600 XP) — unlocks rank 2 + rank 3
            var result = RankSystem.CheckRankUp(100, 700);
            Assert.AreEqual(1, result.OldRank);
            Assert.AreEqual(3, result.NewRank);
            Assert.AreEqual(2, result.Unlocks.Length);
            Assert.AreEqual("emote_clap", result.Unlocks[0].UnlockId);
            Assert.AreEqual("hat_viking_helmet", result.Unlocks[1].UnlockId);
        }

        [Test]
        public void CheckRankUp_SameRank_ReturnsNoUnlocks()
        {
            var result = RankSystem.CheckRankUp(100, 200);
            Assert.AreEqual(1, result.OldRank);
            Assert.AreEqual(1, result.NewRank);
            Assert.AreEqual(0, result.Unlocks.Length);
        }

        // --- FirstBloodPlayerIndex tracking ---

        [Test]
        public void FirstBlood_TrackedOnFirstDamage()
        {
            var config = new GameConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            Assert.AreEqual(-1, state.FirstBloodPlayerIndex);

            // Simulate an explosion from player 0 hitting player 1
            state.Players[1].Health = 100f;
            CombatResolver.ApplyExplosion(state, state.Players[1].Position, 3f, 30f, 5f, 0, false);

            Assert.AreEqual(0, state.FirstBloodPlayerIndex);
        }

        [Test]
        public void FirstBlood_NotOverwrittenBySubsequentDamage()
        {
            var config = new GameConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Player 0 hits player 1
            CombatResolver.ApplyExplosion(state, state.Players[1].Position, 3f, 30f, 5f, 0, false);
            Assert.AreEqual(0, state.FirstBloodPlayerIndex);

            // Player 1 hits player 0 — first blood should remain player 0
            CombatResolver.ApplyExplosion(state, state.Players[0].Position, 3f, 30f, 5f, 1, false);
            Assert.AreEqual(0, state.FirstBloodPlayerIndex);
        }
    }
}
