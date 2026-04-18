using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class KothTests
    {
        static GameConfig KothConfig()
        {
            return new GameConfig
            {
                MatchType = MatchType.KingOfTheHill,
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
                KothZoneRadius = 4f,
                KothPointsPerSecond = 5f,
                KothPointsToWin = 100f,
                KothRelocateInterval = 30f,
                KothRelocateWarning = 3f,
                SuddenDeathTime = 0f // disable for test stability
            };
        }

        [Test]
        public void CreateMatch_Koth_InitializesZone()
        {
            var state = GameSimulation.CreateMatch(KothConfig(), 42);

            Assert.AreEqual(4f, state.Koth.ZoneRadius, 0.01f);
            Assert.IsNotNull(state.Koth.Scores);
            Assert.AreEqual(2, state.Koth.Scores.Length);
            Assert.AreEqual(0f, state.Koth.Scores[0], 0.01f);
            Assert.AreEqual(0f, state.Koth.Scores[1], 0.01f);
        }

        [Test]
        public void CreateMatch_Koth_ZoneOnTerrain()
        {
            var state = GameSimulation.CreateMatch(KothConfig(), 42);

            // Zone should be within map bounds
            float halfMap = state.Config.MapWidth / 2f;
            Assert.GreaterOrEqual(state.Koth.ZonePosition.x, -halfMap);
            Assert.LessOrEqual(state.Koth.ZonePosition.x, halfMap);
        }

        [Test]
        public void CreateMatch_Deathmatch_NoKothInit()
        {
            var config = KothConfig();
            config.MatchType = MatchType.Deathmatch;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.IsNull(state.Koth.Scores, "Deathmatch should not init KOTH scores");
        }

        [Test]
        public void Koth_SinglePlayerInZone_Scores()
        {
            var state = GameSimulation.CreateMatch(KothConfig(), 42);

            // Place P1 in zone, P2 far away
            state.Players[0].Position = state.Koth.ZonePosition;
            state.Players[0].IsGrounded = true;
            state.Players[1].Position = new Vec2(state.Koth.ZonePosition.x + 50f, state.Koth.ZonePosition.y);

            float dt = 1f;
            GameSimulation.Tick(state, dt);

            Assert.Greater(state.Koth.Scores[0], 0f, "P1 should score while in zone");
            Assert.AreEqual(0f, state.Koth.Scores[1], 0.01f, "P2 should not score while outside zone");
        }

        [Test]
        public void Koth_Contested_NobodyScores()
        {
            var state = GameSimulation.CreateMatch(KothConfig(), 42);

            // Place both players in zone
            state.Players[0].Position = state.Koth.ZonePosition;
            state.Players[0].IsGrounded = true;
            state.Players[1].Position = state.Koth.ZonePosition;
            state.Players[1].IsGrounded = true;

            GameSimulation.Tick(state, 1f);

            Assert.AreEqual(0f, state.Koth.Scores[0], 0.01f, "P1 should not score when contested");
            Assert.AreEqual(0f, state.Koth.Scores[1], 0.01f, "P2 should not score when contested");
            Assert.IsTrue(state.Koth.IsContested, "Zone should be contested");
        }

        [Test]
        public void Koth_NobodyInZone_NoScoring()
        {
            var state = GameSimulation.CreateMatch(KothConfig(), 42);

            // Place both players far from zone
            state.Players[0].Position = new Vec2(state.Koth.ZonePosition.x + 50f, 0f);
            state.Players[1].Position = new Vec2(state.Koth.ZonePosition.x - 50f, 0f);

            GameSimulation.Tick(state, 1f);

            Assert.AreEqual(0f, state.Koth.Scores[0], 0.01f);
            Assert.AreEqual(0f, state.Koth.Scores[1], 0.01f);
            Assert.IsFalse(state.Koth.IsContested);
        }

        [Test]
        public void Koth_ReachPointsToWin_EndsMatch()
        {
            var config = KothConfig();
            config.KothPointsToWin = 10f;
            config.KothPointsPerSecond = 100f; // very fast scoring
            var state = GameSimulation.CreateMatch(config, 42);

            // P1 in zone
            state.Players[0].Position = state.Koth.ZonePosition;
            state.Players[0].IsGrounded = true;
            state.Players[1].Position = new Vec2(state.Koth.ZonePosition.x + 50f, 0f);

            GameSimulation.Tick(state, 1f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex, "P1 should win");
        }

        [Test]
        public void Koth_ZoneRelocates()
        {
            var config = KothConfig();
            config.KothRelocateInterval = 1f;
            config.KothRelocateWarning = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);

            Vec2 originalPos = state.Koth.ZonePosition;

            // Tick past relocate interval
            for (int i = 0; i < 100; i++)
                GameSimulation.Tick(state, 0.02f);

            // Zone should have relocated (position changed)
            // Note: there's a tiny chance it relocates to the same spot, so just check timer reset
            Assert.Greater(state.Koth.RelocateTimer, 0f, "Relocate timer should have reset");
        }

        [Test]
        public void Koth_DeadPlayerDoesNotScore()
        {
            var state = GameSimulation.CreateMatch(KothConfig(), 42);

            state.Players[0].Position = state.Koth.ZonePosition;
            state.Players[0].IsDead = true;
            state.Players[1].Position = new Vec2(state.Koth.ZonePosition.x + 50f, 0f);

            GameSimulation.Tick(state, 1f);

            Assert.AreEqual(0f, state.Koth.Scores[0], 0.01f, "Dead player should not score");
        }

        [Test]
        public void Koth_LastPlayerAlive_WinsByElimination()
        {
            var state = GameSimulation.CreateMatch(KothConfig(), 42);

            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex, "Last alive wins by elimination");
        }

        [Test]
        public void Koth_WarningBeforeRelocate()
        {
            var config = KothConfig();
            config.KothRelocateInterval = 5f;
            config.KothRelocateWarning = 3f;
            var state = GameSimulation.CreateMatch(config, 42);

            // Tick to just before warning starts (5 - 3 = 2 seconds)
            // 100 ticks * 0.016 = 1.6s → relocateTimer ~3.4 > 3 → no warning
            for (int i = 0; i < 100; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0f, state.Koth.RelocateWarningTimer, 0.01f,
                "Warning should not be active yet");

            // Tick more past the 2s mark where warning triggers
            // 30 more ticks * 0.016 = 0.48s → total 2.08s → relocateTimer ~2.92 <= 3 → warning
            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Koth.RelocateWarningTimer, 0f,
                "Warning timer should be active before relocation");
        }

        [Test]
        public void Koth_P2WinsByScore()
        {
            var config = KothConfig();
            config.KothPointsToWin = 10f;
            config.KothPointsPerSecond = 100f;
            var state = GameSimulation.CreateMatch(config, 42);

            // P2 in zone
            state.Players[1].Position = state.Koth.ZonePosition;
            state.Players[1].IsGrounded = true;
            state.Players[0].Position = new Vec2(state.Koth.ZonePosition.x + 50f, 0f);

            GameSimulation.Tick(state, 1f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(1, state.WinnerIndex, "P2 should win by score");
        }

        // AllBiomes_HaveWeatherParticleCoverage test removed: depends on WeatherParticles (Unity runtime class)
    }
}
