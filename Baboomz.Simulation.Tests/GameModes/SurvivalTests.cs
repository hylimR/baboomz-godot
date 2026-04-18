using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public partial class SurvivalTests
    {
        static GameConfig SurvivalConfig()
        {
            return new GameConfig
            {
                MatchType = MatchType.Survival,
                TerrainWidth = 320,
                TerrainHeight = 160,
                TerrainPPU = 8f,
                MapWidth = 40f,
                TerrainMinHeight = -2f,
                TerrainMaxHeight = 5f,
                TerrainHillFrequency = 0.1f,
                TerrainFloorDepth = -10f,
                Player1SpawnX = -10f,
                Player2SpawnX = 10f,
                SpawnProbeY = 20f,
                DeathBoundaryY = -25f,
                Gravity = 9.81f,
                DefaultMaxHealth = 100f,
                DefaultMaxEnergy = 100f,
                DefaultEnergyRegen = 10f,
                DefaultMoveSpeed = 5f,
                DefaultJumpForce = 10f,
                DefaultShootCooldown = 0.5f,
                SuddenDeathTime = 0f,
                CrateSpawnInterval = 0f,        // disable crates for test stability
                SurvivalBreakDuration = 0.1f,   // short break for test speed
                SurvivalHealthRegen = 20f,
                SurvivalWaveMobBase = 2,
                SurvivalBossInterval = 5,
                SurvivalScorePerWave = 100,
                SurvivalScorePerKill = 50,
                SurvivalScorePerBossKill = 500,
                SurvivalScoreDirectHitBonus = 25,
                SurvivalScoreNoDamageBonus = 200
            };
        }

        /// <summary>Tick past the break to spawn a wave, using small steps.</summary>
        static void TickPastBreak(GameState state)
        {
            float needed = state.Config.SurvivalBreakDuration + 0.05f;
            int steps = (int)(needed / 0.02f) + 1;
            for (int i = 0; i < steps; i++)
                GameSimulation.Tick(state, 0.02f);
        }

        /// <summary>Kill all mobs and tick once to detect wave clear.</summary>
        static void ClearWave(GameState state)
        {
            for (int i = 1; i < state.Players.Length; i++)
            {
                state.Players[i].Health = 0f;
                state.Players[i].IsDead = true;
            }
            GameSimulation.Tick(state, 0.02f);
        }

        [Test]
        public void CreateMatch_Survival_InitializesState()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);

            Assert.AreEqual(MatchType.Survival, state.Config.MatchType);
            Assert.AreEqual(0, state.Survival.WaveNumber);
            Assert.AreEqual(0, state.Survival.Score);
            Assert.IsFalse(state.Survival.WaveActive);
            Assert.Greater(state.Survival.BreakTimer, 0f, "Should start with break timer");
        }

        [Test]
        public void Survival_BreakTimerCountsDown()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);

            float initialBreak = state.Survival.BreakTimer;
            GameSimulation.Tick(state, 0.02f);

            Assert.Less(state.Survival.BreakTimer, initialBreak, "Break timer should decrease");
        }

        [Test]
        public void Survival_Wave1_SpawnsAfterBreak()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            Assert.AreEqual(1, state.Survival.WaveNumber, "Should be wave 1");
            Assert.IsTrue(state.Survival.WaveActive, "Wave should be active");
            Assert.Greater(state.Players.Length, 1, "Mobs should have spawned");
        }

        [Test]
        public void Survival_MobsAreMarkedAsMobs()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            for (int i = 1; i < state.Players.Length; i++)
            {
                Assert.IsTrue(state.Players[i].IsMob, $"Player {i} should be a mob");
                Assert.IsTrue(state.Players[i].IsAI, $"Player {i} should be AI");
            }
        }

        [Test]
        public void Survival_KillingAllMobs_ClearsWave()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            Assert.IsTrue(state.Survival.WaveActive);
            ClearWave(state);

            Assert.IsFalse(state.Survival.WaveActive, "Wave should end when all mobs dead");
            Assert.Greater(state.Survival.Score, 0, "Should have scored for wave clear");
        }

        [Test]
        public void Survival_PlayerDeath_EndsMatch()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            // Kill the player
            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;

            GameSimulation.Tick(state, 0.02f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(-1, state.WinnerIndex, "No winner in survival when player dies");
        }

        [Test]
        public void Survival_DoesNotEndMatch_WhenMobsDie()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            ClearWave(state);

            Assert.AreEqual(MatchPhase.Playing, state.Phase, "Match should continue after wave clear");
        }

        [Test]
        public void Survival_Wave2_SpawnsAfterWave1Clear()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);
            Assert.AreEqual(1, state.Survival.WaveNumber);

            ClearWave(state);
            TickPastBreak(state);

            Assert.AreEqual(2, state.Survival.WaveNumber, "Should be wave 2");
            Assert.IsTrue(state.Survival.WaveActive);
        }

        [Test]
        public void Survival_BossWave_SpawnsBoss()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);

            // Fast-forward to wave 5 (boss wave): spawn and clear waves 1-4, then spawn wave 5
            for (int w = 0; w < 4; w++)
            {
                TickPastBreak(state);
                ClearWave(state);
            }

            // Spawn wave 5
            TickPastBreak(state);

            Assert.AreEqual(5, state.Survival.WaveNumber);
            Assert.IsTrue(state.Survival.WaveActive);

            // Boss should be spawned
            bool foundBoss = false;
            for (int i = 1; i < state.Players.Length; i++)
            {
                if (!string.IsNullOrEmpty(state.Players[i].BossType))
                    foundBoss = true;
            }
            Assert.IsTrue(foundBoss, "Wave 5 should spawn a boss");
        }

        [Test]
        public void Survival_HealthRegen_BetweenWaves()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            // Damage the player
            state.Players[0].Health = 50f;

            ClearWave(state);

            // Player should have received health regen
            Assert.AreEqual(70f, state.Players[0].Health, 0.01f,
                "Player should get +20 HP between waves");
        }

        [Test]
        public void Survival_EnergyRefill_BetweenWaves()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            // Drain player energy
            state.Players[0].Energy = 10f;

            ClearWave(state);

            Assert.AreEqual(state.Players[0].MaxEnergy, state.Players[0].Energy, 0.01f,
                "Energy should be fully refilled between waves");
        }

        [Test]
        public void Survival_KillScoring_MobKill()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            int scoreBefore = state.Survival.Score;

            // Kill one mob via explosion
            CombatResolver.ApplyExplosion(state, state.Players[1].Position,
                5f, 999f, 0f, 0, false);

            Assert.AreEqual(scoreBefore + state.Config.SurvivalScorePerKill + state.Config.SurvivalScoreDirectHitBonus,
                state.Survival.Score, "Should score for mob kill (includes direct hit bonus)");
        }

        [Test]
        public void Survival_NoDamageBonusAwarded()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            TickPastBreak(state);

            // Ensure player at full HP
            state.Players[0].Health = state.Players[0].MaxHealth;

            ClearWave(state);

            int waveScore = state.Config.SurvivalScorePerWave * 1 + state.Config.SurvivalScoreNoDamageBonus;
            // Score includes any kill scoring from ClearWave, plus wave clear + no-damage bonus
            Assert.GreaterOrEqual(state.Survival.Score, waveScore,
                "Should include wave clear + no-damage bonus");
        }

        [Test]
        public void Survival_WaveSpawn_PreservesPlayerWeaponTracking()
        {
            var state = GameSimulation.CreateMatch(SurvivalConfig(), 42);
            state.Phase = MatchPhase.Playing;
            TickPastBreak(state);

            // Populate player 0's tracking data
            state.WeaponHits[0]["cannon"] = 5;
            state.WeaponKills[0]["cannon"] = 2;
            state.WeaponDamage[0]["cannon"] = 120f;
            state.WeaponsUsed[0].Add("cannon");
            state.SkillsActivated[0].Add(SkillType.Shield);

            // Clear wave 1 and tick past break to spawn wave 2
            ClearWave(state);
            TickPastBreak(state);

            Assert.AreEqual(2, state.Survival.WaveNumber);
            Assert.AreEqual(5, state.WeaponHits[0]["cannon"], "WeaponHits should survive wave spawn");
            Assert.AreEqual(2, state.WeaponKills[0]["cannon"], "WeaponKills should survive wave spawn");
            Assert.AreEqual(120f, state.WeaponDamage[0]["cannon"], 0.01f, "WeaponDamage should survive wave spawn");
            Assert.IsTrue(state.WeaponsUsed[0].Contains("cannon"), "WeaponsUsed should survive wave spawn");
            Assert.IsTrue(state.SkillsActivated[0].Contains(SkillType.Shield), "SkillsActivated should survive wave spawn");
        }

    }
}
