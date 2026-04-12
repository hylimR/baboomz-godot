using System.Collections.Generic;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests
{
    public partial class ChallengeEvaluationTests
    {
        [Test]
        public void Evaluate_BoomBoom_FailsBelow1000()
        {
            var stats = MakeStats();
            stats.TerrainPixelsDestroyed = 999;
            Assert.IsFalse(EvalSingle(4, stats));
        }

        [Test]
        public void Evaluate_JetpackAce_FalseByDefault()
        {
            Assert.IsFalse(EvalSingle(11, MakeStats()));
        }

        [Test]
        public void Evaluate_ShieldWall_FailsBelow80()
        {
            var stats = MakeStats();
            stats.ShieldDamageBlocked = 79f;
            Assert.IsFalse(EvalSingle(12, stats));
        }

        [Test]
        public void Evaluate_FreezeTag_FalseByDefault()
        {
            Assert.IsFalse(EvalSingle(16, MakeStats()));
        }

        [Test]
        public void Evaluate_GravityMaster_FalseByDefault()
        {
            Assert.IsFalse(EvalSingle(17, MakeStats()));
        }

        [Test]
        public void BuildStats_PopulatesChallengeFields()
        {
            var config = new GameConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Players[0].TerrainPixelsDestroyed = 500;
            state.Players[0].ChainLightningTargets = 2;
            state.Players[0].HitWhileJetpacking = true;
            state.Players[0].ShieldDamageBlocked = 100f;
            state.Players[0].FreezeToHitCombo = true;
            state.Players[0].GravityBombVoidKill = true;

            var stats = ChallengeSystem.BuildStats(state, 0);
            Assert.AreEqual(500, stats.TerrainPixelsDestroyed);
            Assert.AreEqual(2, stats.ChainLightningMaxTargets);
            Assert.IsTrue(stats.HitWhileJetpacking);
            Assert.AreEqual(100f, stats.ShieldDamageBlocked, 0.01f);
            Assert.IsTrue(stats.FreezeToHitCombo);
            Assert.IsTrue(stats.GravityBombVoidKill);
        }

        // --- AddChallengeXP ---

        [Test]
        public void AddChallengeXP_AppendsCompletedBonuses()
        {
            var baseResult = new MatchXPResult { BaseXP = 100, BonusXP = 20, TotalXP = 120, Bonuses = new[] { "Sharpshooter" } };
            var challenges = new[]
            {
                new ChallengeResult { ChallengeId = 3, ChallengeName = "Drill Sergeant", XPReward = 45, Completed = true },
                new ChallengeResult { ChallengeId = 7, ChallengeName = "Sharpshooter Elite", XPReward = 40, Completed = false },
            };
            var result = RankSystem.AddChallengeXP(baseResult, challenges);
            Assert.AreEqual(165, result.TotalXP); // 120 + 45
            Assert.AreEqual(65, result.BonusXP);  // 20 + 45
            Assert.AreEqual(100, result.BaseXP);
            Assert.AreEqual(2, result.Bonuses.Length); // original + 1 completed
            Assert.AreEqual("Daily: Drill Sergeant", result.Bonuses[1]);
        }

        [Test]
        public void AddChallengeXP_NullChallenges_ReturnsUnchanged()
        {
            var baseResult = new MatchXPResult { BaseXP = 50, BonusXP = 0, TotalXP = 50, Bonuses = new string[0] };
            var result = RankSystem.AddChallengeXP(baseResult, null);
            Assert.AreEqual(50, result.TotalXP);
            Assert.AreEqual(0, result.Bonuses.Length);
        }

        [Test]
        public void AddChallengeXP_NoneCompleted_NoExtraXP()
        {
            var baseResult = new MatchXPResult { BaseXP = 100, BonusXP = 0, TotalXP = 100, Bonuses = new string[0] };
            var challenges = new[]
            {
                new ChallengeResult { Completed = false, XPReward = 50 },
                new ChallengeResult { Completed = false, XPReward = 40 },
            };
            var result = RankSystem.AddChallengeXP(baseResult, challenges);
            Assert.AreEqual(100, result.TotalXP);
        }

        // --- TotalDamageTaken regression (issue #445) ---

        [Test]
        public void TotalDamageTaken_AccumulatesAcrossMultipleHits()
        {
            var config = new GameConfig
            {
                TerrainWidth = 320, TerrainHeight = 160, TerrainPPU = 8f,
                MapWidth = 40f, TerrainMinHeight = -2f, TerrainMaxHeight = 5f,
                TerrainHillFrequency = 0.1f,
            };
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Players[1].Position = new Vec2(0f, 0f);
            state.Players[1].Health = 200f;
            state.Players[1].MaxHealth = 200f;

            // Hit 1: explosion deals damage
            CombatResolver.ApplyExplosion(state, new Vec2(0f, 0f), 5f, 40f, 0f, 0, false);
            float afterFirst = state.Players[1].TotalDamageTaken;
            Assert.Greater(afterFirst, 0f);

            // Heal back to full
            state.Players[1].Health = 200f;

            // Hit 2: another explosion
            CombatResolver.ApplyExplosion(state, state.Players[1].Position, 5f, 40f, 0f, 0, false);
            float afterSecond = state.Players[1].TotalDamageTaken;

            // TotalDamageTaken must be cumulative (both hits), not just current HP deficit
            Assert.Greater(afterSecond, afterFirst, "TotalDamageTaken must accumulate even after healing");
        }

        [Test]
        public void BuildStats_UsesCumulativeDamageTaken_NotHPDeficit()
        {
            var config = new GameConfig
            {
                TerrainWidth = 320, TerrainHeight = 160, TerrainPPU = 8f,
                MapWidth = 40f, TerrainMinHeight = -2f, TerrainMaxHeight = 5f,
                TerrainHillFrequency = 0.1f,
            };
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.WinnerIndex = 0;

            // Player took 80 cumulative damage but healed back
            state.Players[0].TotalDamageTaken = 80f;
            state.Players[0].Health = state.Players[0].MaxHealth; // full HP after healing

            var stats = ChallengeSystem.BuildStats(state, 0);
            Assert.AreEqual(80f, stats.DamageTaken, "BuildStats must use TotalDamageTaken, not MaxHealth - Health");
        }
    }
}
