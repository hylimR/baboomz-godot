using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class SurvivalTests
    {
        [Test]
        public void Survival_MobCountScaling()
        {
            var config = SurvivalConfig();
            config.SurvivalWaveMobBase = 2;

            Assert.AreEqual(2, GameSimulation.GetSurvivalMobCount(1, config));
            Assert.AreEqual(3, GameSimulation.GetSurvivalMobCount(4, config));
            Assert.AreEqual(4, GameSimulation.GetSurvivalMobCount(8, config));
            Assert.LessOrEqual(GameSimulation.GetSurvivalMobCount(100, config), 7,
                "Mob count should cap at base + 5");
        }

        [Test]
        public void Survival_SpeedMultiplier_Scaling()
        {
            Assert.AreEqual(1.0f, GameSimulation.GetSurvivalSpeedMult(1), 0.01f);
            Assert.AreEqual(1.0f, GameSimulation.GetSurvivalSpeedMult(4), 0.01f);
            Assert.AreEqual(1.1f, GameSimulation.GetSurvivalSpeedMult(6), 0.01f);
            Assert.AreEqual(1.2f, GameSimulation.GetSurvivalSpeedMult(11), 0.01f);
            Assert.AreEqual(1.5f, GameSimulation.GetSurvivalSpeedMult(21), 0.01f);
        }

        [Test]
        public void Survival_HPMultiplier_Scaling()
        {
            Assert.AreEqual(1.0f, GameSimulation.GetSurvivalHPMult(1), 0.01f);
            Assert.AreEqual(1.2f, GameSimulation.GetSurvivalHPMult(6), 0.01f);
            Assert.AreEqual(2.0f, GameSimulation.GetSurvivalHPMult(21), 0.01f);
            Assert.AreEqual(2.5f, GameSimulation.GetSurvivalHPMult(30), 0.01f);
        }

        [Test]
        public void Survival_Deathmatch_SkipsSurvivalLogic()
        {
            var config = SurvivalConfig();
            config.MatchType = MatchType.Deathmatch;
            var state = GameSimulation.CreateMatch(config, 42);

            // Tick past break duration — no survival waves should spawn
            GameSimulation.Tick(state, 10f);

            Assert.AreEqual(0, state.Survival.WaveNumber, "Deathmatch should not run survival logic");
        }

        [Test]
        public void Survival_PlayerPreservedAcrossWaves()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            // Set player stats
            state.Players[0].TotalDamageDealt = 999f;
            state.Players[0].ShotsFired = 42;

            // Clear and spawn new wave
            ClearWave(state);
            TickPastBreak(state);

            Assert.AreEqual(999f, state.Players[0].TotalDamageDealt, 0.01f,
                "Player stats should persist across waves");
            Assert.AreEqual(42, state.Players[0].ShotsFired);
        }

        [Test]
        public void Survival_ScoreKill_MobAtIndexZero_ScoresCorrectly()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            // Manually mark player 0 as a mob to simulate a PVE scenario
            // where a mob occupies index 0
            state.Players[0].IsMob = true;

            int scoreBefore = state.Survival.Score;
            GameSimulation.ScoreSurvivalKill(state, 0);

            Assert.AreEqual(scoreBefore + state.Config.SurvivalScorePerKill,
                state.Survival.Score,
                "ScoreSurvivalKill should credit score for mob at index 0");
        }

        [Test]
        public void Survival_MobsSpawnedTotal_AccumulatesAcrossWaves()
        {
            // Regression test for bug #433: MobsSpawnedTotal was assigned (=) instead of accumulated (+=)
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);

            // Wave 1
            TickPastBreak(state);
            int wave1Mobs = state.Players.Length - 1; // all players except the human
            Assert.AreEqual(wave1Mobs, state.Survival.MobsSpawnedTotal,
                "After wave 1, MobsSpawnedTotal should equal wave 1 mob count");

            // Clear wave 1 and spawn wave 2
            ClearWave(state);
            TickPastBreak(state);
            int wave2Mobs = state.Players.Length - 1;

            Assert.AreEqual(wave1Mobs + wave2Mobs, state.Survival.MobsSpawnedTotal,
                "After wave 2, MobsSpawnedTotal should be cumulative across both waves");
        }

        [Test]
        public void Survival_DirectHitBonus_AppliedOnDirectKill()
        {
            // Regression test for bug #434: SurvivalScoreDirectHitBonus was defined but never applied.
            var config = SurvivalConfig();
            config.SurvivalScoreDirectHitBonus = 25;
            var state = GameSimulation.CreateMatch(config, 42);
            TickPastBreak(state);

            // Place mob directly on the explosion center (guaranteed direct hit: dist=0, dmgRatio=1)
            state.Players[1].Position = new Vec2(0f, 5f);
            state.Players[1].Health = 1f; // low HP so one explosion kills it

            int scoreBefore = state.Survival.Score;
            CombatResolver.ApplyExplosion(state, new Vec2(0f, 5f), 5f, 999f, 0f, 0, false);

            bool mobKilled = state.Players[1].IsDead;
            Assert.IsTrue(mobKilled, "Mob should have been killed by the explosion");
            Assert.AreEqual(scoreBefore + config.SurvivalScorePerKill + config.SurvivalScoreDirectHitBonus,
                state.Survival.Score,
                "Direct hit kill should award SurvivalScorePerKill + SurvivalScoreDirectHitBonus");
        }

        [Test]
        public void Survival_DirectHitBonus_NotAppliedOnSplashKill()
        {
            // Splash kill (explosion far from mob center) should not award direct hit bonus
            var config = SurvivalConfig();
            config.SurvivalScoreDirectHitBonus = 25;
            var state = GameSimulation.CreateMatch(config, 42);
            TickPastBreak(state);

            // Place mob at the edge of the blast radius (splash, not direct)
            float radius = 5f;
            state.Players[1].Position = new Vec2(radius * 0.9f, 5f); // 90% of radius away
            state.Players[1].Health = 1f;

            int scoreBefore = state.Survival.Score;
            CombatResolver.ApplyExplosion(state, new Vec2(0f, 5f), radius, 999f, 0f, 0, false);

            bool mobKilled = state.Players[1].IsDead;
            Assert.IsTrue(mobKilled, "Mob should have been killed by the splash");
            Assert.AreEqual(scoreBefore + config.SurvivalScorePerKill,
                state.Survival.Score,
                "Splash kill should award only SurvivalScorePerKill, not the direct hit bonus");
        }

        [Test]
        public void SurvivalWaveSpawn_ResizesWeaponTracking_Issue94()
        {
            // Issue #94: SpawnSurvivalWave replaces Players array but weapon
            // tracking arrays stayed at original size, causing silent data loss.
            var config = SurvivalConfig();
            config.SurvivalWaveMobBase = 3;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42, state.Players.Length);
            BossLogic.Reset(42, state.Players.Length);

            // Survival starts with just 1 player (no AI opponent)
            int initialCount = state.Players.Length;
            Assert.AreEqual(initialCount, state.WeaponHits.Length);

            // Tick until wave 1 starts (mobs spawn, expanding Players array)
            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < 6000; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players.Length > initialCount) break;
            }

            // After wave spawn, tracking arrays should match new player count
            Assert.AreEqual(state.Players.Length, state.WeaponHits.Length,
                "WeaponHits should be resized to match new Players array (issue #94)");
            Assert.AreEqual(state.Players.Length, state.WeaponKills.Length,
                "WeaponKills should be resized to match new Players array");
            Assert.AreEqual(state.Players.Length, state.WeaponsUsed.Length,
                "WeaponsUsed should be resized to match new Players array");
        }
    }
}
