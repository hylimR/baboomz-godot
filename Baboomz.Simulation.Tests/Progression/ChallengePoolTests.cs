using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests
{
    [TestFixture]
    public class ChallengePoolTests
    {
        // --- Pool ---

        [Test]
        public void Pool_Has20Challenges()
        {
            Assert.AreEqual(20, ChallengeSystem.Pool.Length);
        }

        [Test]
        public void Pool_AllIdsUnique()
        {
            var ids = new System.Collections.Generic.HashSet<int>();
            foreach (var c in ChallengeSystem.Pool)
                Assert.IsTrue(ids.Add(c.Id), $"Duplicate challenge Id: {c.Id}");
        }

        [Test]
        public void Pool_AllNamesNonEmpty()
        {
            foreach (var c in ChallengeSystem.Pool)
                Assert.IsFalse(string.IsNullOrEmpty(c.Name), $"Challenge {c.Id} has empty name");
        }

        [Test]
        public void Pool_AllXPRewardsPositive()
        {
            foreach (var c in ChallengeSystem.Pool)
                Assert.Greater(c.XPReward, 0, $"Challenge {c.Id} has non-positive XP reward");
        }

        // --- Daily Selection ---

        [Test]
        public void GetDailyChallenges_Returns3()
        {
            var challenges = ChallengeSystem.GetDailyChallenges(2026, 3, 31);
            Assert.AreEqual(3, challenges.Length);
        }

        [Test]
        public void GetDailyChallenges_SameDateReturnsSame()
        {
            var a = ChallengeSystem.GetDailyChallenges(2026, 3, 31);
            var b = ChallengeSystem.GetDailyChallenges(2026, 3, 31);
            for (int i = 0; i < 3; i++)
                Assert.AreEqual(a[i].Id, b[i].Id);
        }

        [Test]
        public void GetDailyChallenges_DifferentDatesReturnDifferent()
        {
            var a = ChallengeSystem.GetDailyChallenges(2026, 3, 31);
            var b = ChallengeSystem.GetDailyChallenges(2026, 4, 1);
            bool anyDifferent = false;
            for (int i = 0; i < 3; i++)
                if (a[i].Id != b[i].Id) anyDifferent = true;
            Assert.IsTrue(anyDifferent, "Different dates should produce different challenges");
        }

        [Test]
        public void GetDailyChallenges_All3AreDistinct()
        {
            var challenges = ChallengeSystem.GetDailyChallenges(2026, 3, 31);
            Assert.AreNotEqual(challenges[0].Id, challenges[1].Id);
            Assert.AreNotEqual(challenges[1].Id, challenges[2].Id);
            Assert.AreNotEqual(challenges[0].Id, challenges[2].Id);
        }

        [Test]
        public void GetDateSeed_Deterministic()
        {
            Assert.AreEqual(20260331, ChallengeSystem.GetDateSeed(2026, 3, 31));
            Assert.AreEqual(20260401, ChallengeSystem.GetDateSeed(2026, 4, 1));
        }

        // --- BuildStats ---

        [Test]
        public void BuildStats_ExtractsFromGameState()
        {
            var config = new GameConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Ended;
            state.WinnerIndex = 0;
            state.SuddenDeathActive = true;
            state.Time = 30f;
            state.Players[0].ShotsFired = 10;
            state.Players[0].DirectHits = 6;
            state.Players[0].TotalDamageDealt = 150f;
            state.Players[0].Health = 80f;

            var stats = ChallengeSystem.BuildStats(state, 0);
            Assert.IsTrue(stats.Won);
            Assert.AreEqual(30f, stats.MatchTime, 0.001f);
            Assert.AreEqual(10, stats.ShotsFired);
            Assert.AreEqual(6, stats.DirectHits);
            Assert.AreEqual(150f, stats.TotalDamage, 0.001f);
            Assert.IsTrue(stats.SuddenDeathOccurred);
        }
    }
}
