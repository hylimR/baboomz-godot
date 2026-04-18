using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class DemolitionTests
    {
        static GameConfig DemoConfig()
        {
            return new GameConfig
            {
                MatchType = MatchType.Demolition,
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
                DefaultMoveSpeed = 5f,
                DefaultJumpForce = 10f,
                DefaultShootCooldown = 0.5f,
                DemolitionCrystalHP = 300f,
                DemolitionCrystalWidth = 3f,
                DemolitionCrystalHeight = 5f,
                DemolitionCrystalOffset = 10f,
                DemolitionLivesPerPlayer = 3,
                DemolitionRespawnDelay = 3f,
                SuddenDeathTime = 0f
            };
        }

        [Test]
        public void CreateMatch_Demolition_InitializesCrystals()
        {
            var state = GameSimulation.CreateMatch(DemoConfig(), 42);

            Assert.IsNotNull(state.Demolition.Crystals);
            Assert.AreEqual(2, state.Demolition.Crystals.Length);
            Assert.AreEqual(300f, state.Demolition.Crystals[0].HP, 0.01f);
            Assert.AreEqual(300f, state.Demolition.Crystals[1].HP, 0.01f);
            Assert.AreEqual(300f, state.Demolition.Crystals[0].MaxHP, 0.01f);
        }

        [Test]
        public void CreateMatch_Demolition_CrystalsPlacedBehindSpawns()
        {
            var config = DemoConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Crystal 0 should be to the left of P1 spawn (behind = further from center)
            Assert.Less(state.Demolition.Crystals[0].Position.x, config.Player1SpawnX);
            // Crystal 1 should be to the right of P2 spawn
            Assert.Greater(state.Demolition.Crystals[1].Position.x, config.Player2SpawnX);
        }

        [Test]
        public void CreateMatch_Demolition_PlayersHaveLives()
        {
            var state = GameSimulation.CreateMatch(DemoConfig(), 42);

            Assert.IsNotNull(state.Demolition.LivesRemaining);
            Assert.AreEqual(2, state.Demolition.LivesRemaining.Length);
            Assert.AreEqual(3, state.Demolition.LivesRemaining[0]);
            Assert.AreEqual(3, state.Demolition.LivesRemaining[1]);
        }

        [Test]
        public void CreateMatch_Deathmatch_NoDemolitionInit()
        {
            var config = DemoConfig();
            config.MatchType = MatchType.Deathmatch;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.IsNull(state.Demolition.Crystals);
        }

        [Test]
        public void Demolition_ExplosionDamagesCrystal()
        {
            var state = GameSimulation.CreateMatch(DemoConfig(), 42);
            Vec2 crystalPos = state.Demolition.Crystals[1].Position;

            CombatResolver.ApplyExplosion(state, crystalPos, 4f, 60f, 10f, 0, false);

            Assert.Less(state.Demolition.Crystals[1].HP, 300f, "Crystal should take damage from explosion");
            Assert.Greater(state.CrystalDamageEvents.Count, 0, "Should emit crystal damage event");
        }

        [Test]
        public void Demolition_ExplosionOutOfRange_NoCrystalDamage()
        {
            var state = GameSimulation.CreateMatch(DemoConfig(), 42);
            Vec2 crystalPos = state.Demolition.Crystals[0].Position;

            // Explode far away from crystal
            CombatResolver.ApplyExplosion(state, crystalPos + new Vec2(50f, 0f), 4f, 60f, 10f, 1, false);

            Assert.AreEqual(300f, state.Demolition.Crystals[0].HP, 0.01f, "Crystal should not take damage from distant explosion");
        }

        [Test]
        public void Demolition_CrystalDestroyed_OpponentWins()
        {
            var state = GameSimulation.CreateMatch(DemoConfig(), 42);

            // Destroy crystal 0 (belongs to P1) — P2 should win
            state.Demolition.Crystals[0].HP = 0f;

            GameSimulation.Tick(state, 0.1f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(1, state.WinnerIndex, "P2 should win when P1's crystal is destroyed");
        }

        [Test]
        public void Demolition_CrystalP2Destroyed_P1Wins()
        {
            var state = GameSimulation.CreateMatch(DemoConfig(), 42);

            // Destroy crystal 1 (belongs to P2)
            state.Demolition.Crystals[1].HP = 0f;

            GameSimulation.Tick(state, 0.1f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex, "P1 should win when P2's crystal is destroyed");
        }

        [Test]
        public void Demolition_PlayerRespawnsAfterDelay()
        {
            var config = DemoConfig();
            config.DemolitionRespawnDelay = 1f;
            var state = GameSimulation.CreateMatch(config, 42);

            // Kill player 0
            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;

            // Tick less than respawn delay — still dead
            GameSimulation.Tick(state, 0.5f);
            Assert.IsTrue(state.Players[0].IsDead, "Should still be dead before respawn delay");

            // Tick past respawn delay
            GameSimulation.Tick(state, 0.6f);
            Assert.IsFalse(state.Players[0].IsDead, "Should respawn after delay");
            Assert.AreEqual(100f, state.Players[0].Health, 0.01f, "Should respawn with full health");
        }

        [Test]
        public void Demolition_LivesDecrement_OnRespawn()
        {
            var config = DemoConfig();
            config.DemolitionRespawnDelay = 0.5f;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.AreEqual(3, state.Demolition.LivesRemaining[0]);

            // Kill and respawn
            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;
            GameSimulation.Tick(state, 0.6f);

            Assert.AreEqual(2, state.Demolition.LivesRemaining[0], "Should lose a life on respawn");
        }

        [Test]
        public void Demolition_NoRespawn_WhenLivesExhausted()
        {
            var config = DemoConfig();
            config.DemolitionRespawnDelay = 0.5f;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Demolition.LivesRemaining[0] = 0;
            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;

            GameSimulation.Tick(state, 1f);

            Assert.IsTrue(state.Players[0].IsDead, "Should stay dead with no lives remaining");
        }

        [Test]
        public void Demolition_AllDead_NoLives_CrystalHPTiebreaker()
        {
            var config = DemoConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Exhaust all lives
            state.Demolition.LivesRemaining[0] = 0;
            state.Demolition.LivesRemaining[1] = 0;

            // Both dead
            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;
            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;

            // P1 crystal has more HP
            state.Demolition.Crystals[0].HP = 200f;
            state.Demolition.Crystals[1].HP = 100f;

            GameSimulation.Tick(state, 0.1f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex, "Player with more crystal HP should win");
        }

        [Test]
        public void Demolition_AllDead_EqualHP_Draw()
        {
            var config = DemoConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            state.Demolition.LivesRemaining[0] = 0;
            state.Demolition.LivesRemaining[1] = 0;
            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;
            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;

            state.Demolition.Crystals[0].HP = 150f;
            state.Demolition.Crystals[1].HP = 150f;

            GameSimulation.Tick(state, 0.1f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(-1, state.WinnerIndex, "Equal crystal HP should be a draw");
        }

        [Test]
        public void Demolition_RespawnPlayer_UsesTeamIndex_NotPlayerIndex()
        {
            // Regression test for bug #435: RespawnPlayer used playerIndex==0 to select crystal,
            // causing players at index>0 with TeamIndex=0 to respawn near the wrong crystal.
            var config = DemoConfig();
            config.DemolitionRespawnDelay = 0.5f;
            var state = GameSimulation.CreateMatch(config, 42);

            // Simulate a 2v2 scenario: player at index 1 belongs to team 0
            state.Players[1].TeamIndex = 0;

            Vec2 crystal0Pos = state.Demolition.Crystals[0].Position;
            Vec2 crystal1Pos = state.Demolition.Crystals[1].Position;

            // Kill player 1 (index=1, TeamIndex=0) and wait for respawn
            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;
            GameSimulation.Tick(state, 0.6f);

            Assert.IsFalse(state.Players[1].IsDead, "Player 1 should have respawned");

            // Player 1 belongs to team 0 → must respawn near crystal[0], not crystal[1]
            float distToCrystal0 = Vec2.Distance(state.Players[1].Position, crystal0Pos);
            float distToCrystal1 = Vec2.Distance(state.Players[1].Position, crystal1Pos);
            Assert.Less(distToCrystal0, distToCrystal1,
                "Player with TeamIndex=0 at playerIndex=1 must respawn near crystal[0]");
        }

        [Test]
        public void Demolition_FireZone_DoesNotDamageCrystal()
        {
            var state = GameSimulation.CreateMatch(DemoConfig(), 42);
            Vec2 crystalPos = state.Demolition.Crystals[0].Position;

            // Add fire zone at crystal position
            state.FireZones.Add(new FireZoneState
            {
                Position = crystalPos,
                Radius = 5f,
                DamagePerSecond = 15f,
                RemainingTime = 5f,
                OwnerIndex = 1,
                Active = true
            });

            float hpBefore = state.Demolition.Crystals[0].HP;
            GameSimulation.Tick(state, 1f);

            Assert.AreEqual(hpBefore, state.Demolition.Crystals[0].HP, 0.01f,
                "Fire zones should not damage crystals");
        }

        [Test]
        public void Demolition_MatchDoesNotEndByElimination()
        {
            var state = GameSimulation.CreateMatch(DemoConfig(), 42);

            // Kill one player — match should NOT end (Demolition uses crystal destruction)
            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;
            // P2 still has lives, so will respawn
            Assert.Greater(state.Demolition.LivesRemaining[1], 0);

            GameSimulation.Tick(state, 0.1f);

            Assert.AreEqual(MatchPhase.Playing, state.Phase,
                "Demolition should not end by elimination while lives remain");
        }
    }
}
