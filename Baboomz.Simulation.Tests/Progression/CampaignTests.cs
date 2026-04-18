using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class CampaignTests
    {
        static GameConfig CampaignConfig()
        {
            var config = new GameConfig { MatchType = MatchType.Campaign };
            return config;
        }

        [Test]
        public void CreateMatch_Campaign_CreatesSinglePlayer()
        {
            var config = CampaignConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.AreEqual(1, state.Players.Length, "Campaign should create exactly 1 player");
            Assert.IsFalse(state.Players[0].IsAI, "Player 0 should be human");
            Assert.AreEqual(MatchPhase.Playing, state.Phase);
        }

        [Test]
        public void Campaign_CheckMatchEnd_DoesNotEndMatch()
        {
            var config = CampaignConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Tick many frames — match should stay Playing (no auto-end)
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Playing, state.Phase,
                "Campaign match should not auto-end via CheckMatchEnd");
        }

        [Test]
        public void Campaign_MobsCanBeAddedToPlayersArray()
        {
            var config = CampaignConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            Assert.AreEqual(1, state.Players.Length);

            // Simulate CampaignBootstrap extending Players array with mobs
            var newPlayers = new PlayerState[3];
            System.Array.Copy(state.Players, newPlayers, 1);
            newPlayers[1] = new PlayerState
            {
                Position = new Vec2(30f, 10f),
                Health = 30f, MaxHealth = 30f,
                IsAI = true, IsMob = true, MobType = "walker",
                WeaponSlots = new[] { new WeaponSlotState { WeaponId = "mob_cannon", Ammo = -1 } }
            };
            newPlayers[2] = new PlayerState
            {
                Position = new Vec2(50f, 10f),
                Health = 50f, MaxHealth = 50f,
                IsAI = true, IsMob = true, MobType = "turret",
                WeaponSlots = new[] { new WeaponSlotState { WeaponId = "mob_cannon", Ammo = -1 } }
            };
            state.Players = newPlayers;
            AILogic.Reset(state.Seed, state.Players.Length);
            BossLogic.Reset(state.Seed, state.Players.Length);

            Assert.AreEqual(3, state.Players.Length);
            Assert.IsFalse(state.Players[0].IsAI);
            Assert.IsTrue(state.Players[1].IsMob);
            Assert.IsTrue(state.Players[2].IsMob);
        }

        [Test]
        public void ObjectiveTracker_EliminateAll_CompletesWhenAllMobsDead()
        {
            var config = CampaignConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Add 2 mobs
            var newPlayers = new PlayerState[3];
            System.Array.Copy(state.Players, newPlayers, 1);
            newPlayers[1] = new PlayerState { Health = 30f, MaxHealth = 30f, IsAI = true, IsMob = true };
            newPlayers[2] = new PlayerState { Health = 50f, MaxHealth = 50f, IsAI = true, IsMob = true };
            state.Players = newPlayers;

            var objective = new LevelObjectiveData { type = "eliminate_all" };
            var tracker = new ObjectiveTracker(objective);
            tracker.SetPlayerIndex(0);

            // Not complete yet
            tracker.Update(state, 0.1f);
            Assert.IsFalse(tracker.IsComplete);

            // Kill mob 1
            state.Players[1].IsDead = true;
            tracker.Update(state, 0.1f);
            Assert.IsFalse(tracker.IsComplete, "Should not complete until ALL mobs dead");

            // Kill mob 2
            state.Players[2].IsDead = true;
            tracker.Update(state, 0.1f);
            Assert.IsTrue(tracker.IsComplete, "Should complete when all mobs are dead");
        }

        [Test]
        public void ObjectiveTracker_DefeatBoss_CompletesWhenBossDies()
        {
            var config = CampaignConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Add a boss at index 1
            var newPlayers = new PlayerState[2];
            System.Array.Copy(state.Players, newPlayers, 1);
            newPlayers[1] = new PlayerState
            {
                Health = 200f, MaxHealth = 200f, IsAI = true, IsMob = true,
                BossType = "iron_sentinel"
            };
            state.Players = newPlayers;

            var objective = new LevelObjectiveData { type = "defeat_boss" };
            var tracker = new ObjectiveTracker(objective);
            tracker.SetPlayerIndex(0);
            tracker.SetBossIndex(1);

            tracker.Update(state, 0.1f);
            Assert.IsFalse(tracker.IsComplete);

            state.Players[1].IsDead = true;
            tracker.Update(state, 0.1f);
            Assert.IsTrue(tracker.IsComplete, "Should complete when boss dies");
        }

        [Test]
        public void ObjectiveTracker_FailsWhenPlayerDies()
        {
            var config = CampaignConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            var objective = new LevelObjectiveData { type = "eliminate_all" };
            var tracker = new ObjectiveTracker(objective);
            tracker.SetPlayerIndex(0);

            state.Players[0].IsDead = true;
            tracker.Update(state, 0.1f);
            Assert.IsTrue(tracker.IsFailed, "Should fail when player dies");
        }

        [Test]
        public void ObjectiveTracker_SurviveWaves_ProgressesThroughWaves()
        {
            var config = CampaignConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            var objective = new LevelObjectiveData
            {
                type = "survive_waves",
                waveCount = 2,
                waves = new[]
                {
                    new LevelWaveData { delay = 1f, enemies = new[] { new LevelEnemyData { type = "walker", x = 30 } } },
                    new LevelWaveData { delay = 1f, enemies = new[] { new LevelEnemyData { type = "turret", x = 50 } } }
                }
            };
            var tracker = new ObjectiveTracker(objective);
            tracker.SetPlayerIndex(0);

            // First tick: starts wave 0 timer
            tracker.Update(state, 0f);
            Assert.IsTrue(tracker.WaveActive);
            Assert.AreEqual(0, tracker.CurrentWave);

            // Tick past wave 0 delay
            tracker.Update(state, 2f);
            Assert.IsTrue(tracker.WaveActive, "Wave still active (waiting for spawn)");

            // Simulate external spawning: add mob at index 1
            var newPlayers = new PlayerState[2];
            System.Array.Copy(state.Players, newPlayers, 1);
            newPlayers[1] = new PlayerState { Health = 30f, MaxHealth = 30f, IsAI = true, IsMob = true };
            state.Players = newPlayers;
            tracker.MarkWaveSpawned();

            // Mob alive — wave not complete yet
            tracker.Update(state, 0.1f);
            Assert.IsFalse(tracker.IsComplete);

            // Kill mob
            state.Players[1].IsDead = true;
            tracker.Update(state, 0.1f);
            Assert.AreEqual(1, tracker.CurrentWave, "Should advance to wave 1");
            Assert.IsTrue(tracker.WaveActive, "Wave 1 should be pending");

            // Tick past wave 1 delay + spawn
            tracker.Update(state, 2f);
            var morePlayers = new PlayerState[3];
            System.Array.Copy(state.Players, morePlayers, 2);
            morePlayers[2] = new PlayerState { Health = 50f, MaxHealth = 50f, IsAI = true, IsMob = true };
            state.Players = morePlayers;
            tracker.MarkWaveSpawned();

            // Kill wave 1 mob
            state.Players[2].IsDead = true;
            tracker.Update(state, 0.1f);
            Assert.IsTrue(tracker.IsComplete, "Should complete after all waves cleared");
        }

        [Test]
        public void Campaign_MatchType_InConfig()
        {
            var config = CampaignConfig();
            Assert.AreEqual(MatchType.Campaign, config.MatchType);
        }

        [Test]
        public void Campaign_SimulationTicks_WithMobs()
        {
            var config = CampaignConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Add mobs like CampaignBootstrap would
            var newPlayers = new PlayerState[3];
            System.Array.Copy(state.Players, newPlayers, 1);
            newPlayers[1] = new PlayerState
            {
                Position = new Vec2(30f, 10f),
                Health = 30f, MaxHealth = 30f,
                IsAI = true, IsMob = true, MobType = "walker",
                Energy = 100f, MaxEnergy = 100f,
                WeaponSlots = new[] { new WeaponSlotState { WeaponId = "mob_cannon", Ammo = -1 } }
            };
            newPlayers[2] = new PlayerState
            {
                Position = new Vec2(50f, 10f),
                Health = 50f, MaxHealth = 50f,
                IsAI = true, IsMob = true, MobType = "turret",
                Energy = 100f, MaxEnergy = 100f,
                WeaponSlots = new[] { new WeaponSlotState { WeaponId = "mob_cannon", Ammo = -1 } }
            };
            state.Players = newPlayers;
            state.InitWeaponTracking(state.Players.Length);
            AILogic.Reset(state.Seed, state.Players.Length);
            BossLogic.Reset(state.Seed, state.Players.Length);

            // Tick simulation — should not crash
            for (int i = 0; i < 120; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Playing, state.Phase,
                "Campaign should remain Playing while mobs alive (no auto-end)");
        }
    }
}
