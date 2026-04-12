using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests
{
    public partial class RankSystemTests
    {
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
