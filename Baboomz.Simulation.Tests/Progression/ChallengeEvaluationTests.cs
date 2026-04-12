using System.Collections.Generic;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests
{
    [TestFixture]
    public partial class ChallengeEvaluationTests
    {
        // --- Evaluation: Individual Challenges ---

        [Test]
        public void Evaluate_CannonSpecialist_80PercentCannonDamage()
        {
            var stats = MakeStats(won: true, totalDamage: 100f);
            stats.WeaponDamage = new Dictionary<string, float> { { "cannon", 85f } };
            Assert.IsTrue(EvalSingle(1, stats));
        }

        [Test]
        public void Evaluate_CannonSpecialist_FailsBelow80Percent()
        {
            var stats = MakeStats(won: true, totalDamage: 100f);
            stats.WeaponDamage = new Dictionary<string, float> { { "cannon", 79f } };
            Assert.IsFalse(EvalSingle(1, stats));
        }

        [Test]
        public void Evaluate_CannonSpecialist_FailsOnLoss()
        {
            var stats = MakeStats(won: false, totalDamage: 100f);
            stats.WeaponDamage = new Dictionary<string, float> { { "cannon", 100f } };
            Assert.IsFalse(EvalSingle(1, stats));
        }

        [Test]
        public void Evaluate_RocketBarrage_200PlusDamage()
        {
            var stats = MakeStats();
            stats.WeaponDamage = new Dictionary<string, float> { { "rocket", 200f } };
            Assert.IsTrue(EvalSingle(2, stats));
        }

        [Test]
        public void Evaluate_RocketBarrage_FailsBelow200()
        {
            var stats = MakeStats();
            stats.WeaponDamage = new Dictionary<string, float> { { "rocket", 199f } };
            Assert.IsFalse(EvalSingle(2, stats));
        }

        [Test]
        public void Evaluate_DrillSergeant_3Hits()
        {
            var stats = MakeStats();
            stats.WeaponHits = new Dictionary<string, int> { { "drill", 3 } };
            Assert.IsTrue(EvalSingle(3, stats));
        }

        [Test]
        public void Evaluate_DrillSergeant_FailsBelow3()
        {
            var stats = MakeStats();
            stats.WeaponHits = new Dictionary<string, int> { { "drill", 2 } };
            Assert.IsFalse(EvalSingle(3, stats));
        }

        [Test]
        public void Evaluate_BoomBoom_1000Pixels()
        {
            var stats = MakeStats();
            stats.TerrainPixelsDestroyed = 1000;
            Assert.IsTrue(EvalSingle(4, stats));
        }

        [Test]
        public void Evaluate_ChainLightning_2Targets()
        {
            var stats = MakeStats();
            stats.ChainLightningMaxTargets = 2;
            Assert.IsTrue(EvalSingle(5, stats));
        }

        [Test]
        public void Evaluate_SheepHerder_1Kill()
        {
            var stats = MakeStats();
            stats.WeaponKills = new Dictionary<string, int> { { "sheep", 1 } };
            Assert.IsTrue(EvalSingle(6, stats));
        }

        [Test]
        public void Evaluate_SharpshooterElite_Above60Percent()
        {
            var stats = MakeStats();
            stats.ShotsFired = 5;
            stats.DirectHits = 4; // 80% > 60%
            Assert.IsTrue(EvalSingle(7, stats));
        }

        [Test]
        public void Evaluate_SharpshooterElite_FailsBelow60Percent()
        {
            var stats = MakeStats();
            stats.ShotsFired = 5;
            stats.DirectHits = 2; // 40% < 60%
            Assert.IsFalse(EvalSingle(7, stats));
        }

        [Test]
        public void Evaluate_SharpshooterElite_FailsBelow5Shots()
        {
            var stats = MakeStats();
            stats.ShotsFired = 4;
            stats.DirectHits = 4;
            Assert.IsFalse(EvalSingle(7, stats));
        }

        [Test]
        public void Evaluate_UntouchablePro_WinLowDamage()
        {
            Assert.IsTrue(EvalSingle(8, MakeStats(won: true, damageTaken: 20f)));
        }

        [Test]
        public void Evaluate_UntouchablePro_FailsOver20()
        {
            Assert.IsFalse(EvalSingle(8, MakeStats(won: true, damageTaken: 21f)));
        }

        [Test]
        public void Evaluate_SpeedDemon_WinUnder45s()
        {
            Assert.IsTrue(EvalSingle(9, MakeStats(won: true, matchTime: 44f)));
        }

        [Test]
        public void Evaluate_SpeedDemon_FailsAt45s()
        {
            Assert.IsFalse(EvalSingle(9, MakeStats(won: true, matchTime: 45f)));
        }

        [Test]
        public void Evaluate_SkillMaster_5Skills()
        {
            var stats = MakeStats();
            stats.DistinctSkillsUsed = 5;
            Assert.IsTrue(EvalSingle(10, stats));
        }

        [Test]
        public void Evaluate_JetpackAce()
        {
            var stats = MakeStats();
            stats.HitWhileJetpacking = true;
            Assert.IsTrue(EvalSingle(11, stats));
        }

        [Test]
        public void Evaluate_ShieldWall_80Blocked()
        {
            var stats = MakeStats();
            stats.ShieldDamageBlocked = 80f;
            Assert.IsTrue(EvalSingle(12, stats));
        }

        [Test]
        public void Evaluate_NoSkillsAllowed_WinNoSkills()
        {
            var stats = MakeStats(won: true);
            stats.AnySkillActivated = false;
            Assert.IsTrue(EvalSingle(13, stats));
        }

        [Test]
        public void Evaluate_NoSkillsAllowed_FailsIfSkillUsed()
        {
            var stats = MakeStats(won: true);
            stats.AnySkillActivated = true;
            Assert.IsFalse(EvalSingle(13, stats));
        }

        [Test]
        public void Evaluate_Bombardier_3SubHits()
        {
            var stats = MakeStats();
            stats.ClusterBananaSubHits = 3;
            Assert.IsTrue(EvalSingle(15, stats));
        }

        [Test]
        public void Evaluate_FreezeTag()
        {
            var stats = MakeStats();
            stats.FreezeToHitCombo = true;
            Assert.IsTrue(EvalSingle(16, stats));
        }

        [Test]
        public void Evaluate_GravityMaster()
        {
            var stats = MakeStats();
            stats.GravityBombVoidKill = true;
            Assert.IsTrue(EvalSingle(17, stats));
        }

        [Test]
        public void Evaluate_FirstStrike_3Today()
        {
            var stats = MakeStats();
            stats.FirstStrikesToday = 3;
            Assert.IsTrue(EvalSingle(18, stats));
        }

        [Test]
        public void Evaluate_Survivor_WinAfterSuddenDeath()
        {
            Assert.IsTrue(EvalSingle(19, MakeStats(won: true, suddenDeath: true)));
        }

        [Test]
        public void Evaluate_Survivor_FailsWithoutSuddenDeath()
        {
            Assert.IsFalse(EvalSingle(19, MakeStats(won: true, suddenDeath: false)));
        }

        [Test]
        public void Evaluate_WinStreak_3Consecutive()
        {
            var stats = MakeStats();
            stats.ConsecutiveWinsToday = 3;
            Assert.IsTrue(EvalSingle(20, stats));
        }

        [Test]
        public void Evaluate_CloseQuarters_AllKillsWithin5Units()
        {
            var stats = MakeStats(won: true);
            stats.TotalKills = 2;
            stats.CloseRangeKills = 2;
            Assert.IsTrue(EvalSingle(14, stats));
        }

        [Test]
        public void Evaluate_CloseQuarters_FailsIfAnyKillFar()
        {
            var stats = MakeStats(won: true);
            stats.TotalKills = 3;
            stats.CloseRangeKills = 2; // one kill was > 5 units
            Assert.IsFalse(EvalSingle(14, stats));
        }

        [Test]
        public void Evaluate_CloseQuarters_FailsWithNoKills()
        {
            var stats = MakeStats(won: true);
            stats.TotalKills = 0;
            stats.CloseRangeKills = 0;
            Assert.IsFalse(EvalSingle(14, stats));
        }

        [Test]
        public void Evaluate_CloseQuarters_FailsOnLoss()
        {
            var stats = MakeStats(won: false);
            stats.TotalKills = 1;
            stats.CloseRangeKills = 1;
            Assert.IsFalse(EvalSingle(14, stats));
        }

        // --- Helpers ---

        static MatchChallengeStats MakeStats(bool won = false, float totalDamage = 0f,
            float damageTaken = 0f, float matchTime = 0f, bool suddenDeath = false)
        {
            return new MatchChallengeStats
            {
                Won = won,
                TotalDamage = totalDamage,
                DamageTaken = damageTaken,
                MatchTime = matchTime,
                SuddenDeathOccurred = suddenDeath,
            };
        }

        static bool EvalSingle(int challengeId, MatchChallengeStats stats)
        {
            ChallengeDef def = default;
            foreach (var c in ChallengeSystem.Pool)
                if (c.Id == challengeId) { def = c; break; }
            var results = ChallengeSystem.EvaluateChallenges(new[] { def }, stats);
            return results[0].Completed;
        }
    }
}
