using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class ReplayPlayerTests
    {
        static GameConfig SmallConfig() => new GameConfig
        {
            TerrainWidth = 320, TerrainHeight = 160, TerrainPPU = 8f,
            MapWidth = 40f, TerrainMinHeight = -2f, TerrainMaxHeight = 5f,
            TerrainHillFrequency = 0.1f, TerrainFloorDepth = -10f,
            Player1SpawnX = -10f, Player2SpawnX = 10f,
            SpawnProbeY = 20f, DeathBoundaryY = -25f,
            Gravity = 9.81f, DefaultMaxHealth = 100f,
            DefaultMoveSpeed = 5f, DefaultJumpForce = 10f,
            DefaultShootCooldown = 0.5f
        };

        static ReplayData RecordShortMatch(int frames = 30)
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            var data = ReplaySystem.StartRecording(state);
            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < frames; i++)
                GameSimulation.Tick(state, 0.016f);
            ReplaySystem.StopRecording(state);
            return data;
        }

        [Test]
        public void ReplayPlayer_InitialState_FrameIndexIsZero()
        {
            var data = RecordShortMatch(10);
            var player = new ReplayPlayer(data);
            Assert.AreEqual(0, player.FrameIndex);
            Assert.AreEqual(10, player.TotalFrames);
            Assert.IsFalse(player.IsFinished);
        }

        [Test]
        public void ReplayPlayer_Step_AdvancesFrameIndex()
        {
            var data = RecordShortMatch(10);
            var player = new ReplayPlayer(data);
            player.Step();
            Assert.AreEqual(1, player.FrameIndex);
        }

        [Test]
        public void ReplayPlayer_StepAll_IsFinished()
        {
            var data = RecordShortMatch(5);
            var player = new ReplayPlayer(data);
            for (int i = 0; i < 5; i++) player.Step();
            Assert.IsTrue(player.IsFinished);
        }

        [Test]
        public void ReplayPlayer_SeekTo_ResetsAndReachesTarget()
        {
            var data = RecordShortMatch(20);
            var player = new ReplayPlayer(data);
            for (int i = 0; i < 10; i++) player.Step();
            player.SeekTo(5);
            Assert.AreEqual(5, player.FrameIndex);
        }

        [Test]
        public void ReplayPlayer_SeekTo_ClampsBeyondEnd()
        {
            var data = RecordShortMatch(10);
            var player = new ReplayPlayer(data);
            player.SeekTo(999);
            Assert.AreEqual(10, player.FrameIndex);
            Assert.IsTrue(player.IsFinished);
        }

        [Test]
        public void ReplayPlayer_SeekTo_ClampsBeforeStart()
        {
            var data = RecordShortMatch(10);
            var player = new ReplayPlayer(data);
            for (int i = 0; i < 5; i++) player.Step();
            player.SeekTo(-1);
            Assert.AreEqual(0, player.FrameIndex);
        }

        [Test]
        public void ReplayPlayer_PauseResume_PreventsTick()
        {
            var data = RecordShortMatch(30);
            var player = new ReplayPlayer(data);
            player.Pause();
            player.Tick(1.0f);
            Assert.AreEqual(0, player.FrameIndex, "Paused player must not advance on Tick");
            player.Resume();
            player.Tick(0.016f);
            Assert.Greater(player.FrameIndex, 0, "Resumed player should advance on Tick");
        }

        [Test]
        public void ReplayPlayer_TogglePause_FlipsState()
        {
            var data = RecordShortMatch(10);
            var player = new ReplayPlayer(data);
            Assert.IsFalse(player.IsPaused);
            player.TogglePause();
            Assert.IsTrue(player.IsPaused);
            player.TogglePause();
            Assert.IsFalse(player.IsPaused);
        }

        [Test]
        public void ReplayPlayer_SpeedDouble_AdvancesFaster()
        {
            var data = RecordShortMatch(30);
            var playerNormal = new ReplayPlayer(data);
            var playerFast = new ReplayPlayer(data);
            playerNormal.Speed = 1f;
            playerFast.Speed = 2f;
            playerNormal.Tick(0.1f);
            playerFast.Tick(0.1f);
            Assert.Greater(playerFast.FrameIndex, playerNormal.FrameIndex,
                "2x speed should advance more frames than 1x in the same real time");
        }

        [Test]
        public void ReplayPlayer_Deterministic_SameStateAsDirectSimulation()
        {
            var config = SmallConfig();
            const int seed = 77;
            const int frames = 50;

            var direct = GameSimulation.CreateMatch(config, seed);
            direct.Phase = MatchPhase.Playing;
            for (int i = 0; i < frames; i++)
                GameSimulation.Tick(direct, 0.016f);

            var state = GameSimulation.CreateMatch(config, seed);
            var replayData = ReplaySystem.StartRecording(state);
            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < frames; i++)
                GameSimulation.Tick(state, 0.016f);
            ReplaySystem.StopRecording(state);

            var player = new ReplayPlayer(replayData);
            while (!player.IsFinished)
                player.Step();

            for (int i = 0; i < direct.Players.Length; i++)
            {
                Assert.AreEqual(direct.Players[i].Position.x,
                    player.State.Players[i].Position.x, 0.001f,
                    $"Player {i} X position mismatch between direct and replay");
                Assert.AreEqual(direct.Players[i].Position.y,
                    player.State.Players[i].Position.y, 0.001f,
                    $"Player {i} Y position mismatch between direct and replay");
                Assert.AreEqual(direct.Players[i].Health,
                    player.State.Players[i].Health, 0.001f,
                    $"Player {i} health mismatch between direct and replay");
            }
        }

        [Test]
        public void ReplaySystem_StopRecording_DisablesRecording()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            ReplaySystem.StartRecording(state);
            ReplaySystem.StopRecording(state);
            Assert.IsNull(state.ReplayRecording,
                "StopRecording should clear ReplayRecording from GameState");
        }

        [Test]
        public void ReplaySystem_DisabledDuringPlayback()
        {
            var data = RecordShortMatch(10);
            var player = new ReplayPlayer(data);
            Assert.IsNull(player.State.ReplayRecording,
                "ReplayPlayer must disable recording on the playback state");
        }

        [Test]
        public void BossLogic_SandWyrm_StaysSurfacedOnFirstSpawn()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Set up player 1 as a Sand Wyrm boss
            state.Players[1].BossType = "sand_wyrm";
            state.Players[1].IsMob = true;
            state.Players[1].IsAI = true;
            state.Players[1].MaxHealth = 150f;
            state.Players[1].Health = 150f;
            state.Players[1].BossPhase = 0;

            BossLogic.Reset(42);

            // Tick a few frames — wyrm should stay surfaced (subState 0), not immediately submerge
            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, BossLogic.subState[1],
                "Sand Wyrm should remain surfaced after initial spawn, not immediately submerge");
        }

        [Test]
        public void BossLogic_SandWyrm_SnapsToGroundAfterEmerge_Issue90()
        {
            // Issue #90: SandWyrm emerged at arbitrary Y with no ground detection.
            // Tick long enough for a full submerge → underground → emerge cycle.
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].BossType = "sand_wyrm";
            state.Players[1].IsMob = true;
            state.Players[1].IsAI = true;
            state.Players[1].MaxHealth = 150f;
            state.Players[1].Health = 150f;
            state.Players[1].BossPhase = 0;

            BossLogic.Reset(42);

            // Force into submerge (subState 1) by setting subState directly
            BossLogic.subState[1] = 1;

            // Tick for ~15 seconds to complete full submerge→underground→emerge
            for (int i = 0; i < 1000; i++)
                GameSimulation.Tick(state, 0.016f);

            // After enough time, the boss should have completed at least one
            // emerge cycle and be near ground level (not deep underground)
            if (BossLogic.subState[1] == 0) // back to surfaced
            {
                float groundY = GamePhysics.FindGroundY(
                    state.Terrain, state.Players[1].Position.x, config.SpawnProbeY, 0.5f);
                Assert.AreEqual(groundY + 0.5f, state.Players[1].Position.y, 3f,
                    "SandWyrm should be near ground after emerging (issue #90)");
            }
            // If still in a substate, at least verify it's not stuck underground
            Assert.IsFalse(state.Players[1].Position.y < config.DeathBoundaryY,
                "SandWyrm should not be below death boundary");
        }

        [Test]
        public void BossLogic_ForgeColossus_ArmorExpiresEvenIfPhaseAdvancesPast1()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Set up player 1 as Forge Colossus
            state.Players[1].BossType = "forge_colossus";
            state.Players[1].IsMob = true;
            state.Players[1].IsAI = true;
            state.Players[1].MaxHealth = 200f;
            state.Players[1].Health = 200f;
            state.Players[1].BossPhase = 0;

            // Move player 0 far away so explosions don't kill them
            state.Players[0].Position = new Vec2(-40f, 0f);

            BossLogic.Reset(42);

            // Drop HP to 74% to trigger armor (phase 1)
            state.Players[1].Health = 148f;
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(2f, state.Players[1].ArmorMultiplier, "Armor should activate at 75% HP");
            Assert.AreEqual(1, state.Players[1].BossPhase);

            // Immediately drop HP to 49% to trigger phase 2 stomp (skipping armor timer)
            state.Players[1].Health = 98f;
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(2, state.Players[1].BossPhase, "Should advance to phase 2");

            // Advance time past the 10s armor window
            for (int i = 0; i < 700; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(1f, state.Players[1].ArmorMultiplier,
                "Armor should expire after 10s even when BossPhase advanced past 1");
        }

        [Test]
        public void BossLogic_BaronCogsworth_TeleportClampedToMapBounds()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Set up player 1 as Baron Cogsworth
            state.Players[1].BossType = "baron_cogsworth";
            state.Players[1].IsMob = true;
            state.Players[1].IsAI = true;
            state.Players[1].MaxHealth = 300f;
            state.Players[1].Health = 300f;
            state.Players[1].BossPhase = 0;

            BossLogic.Reset(42);

            // Drop HP to trigger phase 2 (teleporting phase, BossPhase = 1)
            state.Players[1].Health = 190f; // ~63%, below 66%
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(1, state.Players[1].BossPhase, "Should enter phase 2");

            // Place boss near the map edge
            float halfMap = config.MapWidth / 2f;
            state.Players[1].Position.x = halfMap - 1f;

            // Tick many times to trigger multiple teleports
            for (int i = 0; i < 1000; i++)
                GameSimulation.Tick(state, 0.016f);

            float bossX = state.Players[1].Position.x;
            Assert.GreaterOrEqual(bossX, -halfMap,
                "Boss X should not go below -halfMap after teleport");
            Assert.LessOrEqual(bossX, halfMap,
                "Boss X should not exceed halfMap after teleport");
        }

        [Test]
        public void BossLogic_BaronCogsworth_Phase2TeleportDelayed_Issue48()
        {
            // Issue #48: Phase 2 entry set specialTimer instead of stateTimer,
            // causing immediate teleport because stateTimer was still 0.
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].BossType = "baron_cogsworth";
            state.Players[1].IsMob = true;
            state.Players[1].IsAI = true;
            state.Players[1].MaxHealth = 300f;
            state.Players[1].Health = 300f;
            state.Players[1].BossPhase = 0;

            BossLogic.Reset(42);

            // Record boss position before phase 2 entry
            Vec2 positionBefore = state.Players[1].Position;

            // Drop HP to trigger phase 2 (66% threshold)
            state.Players[1].Health = 190f; // ~63%, below 66%
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(1, state.Players[1].BossPhase, "Should enter phase 2");

            // Boss should NOT have teleported on the very first phase-2 tick.
            // The 10s delay means position should be unchanged after just 1 tick.
            Assert.AreEqual(positionBefore.x, state.Players[1].Position.x,
                "Boss should not teleport immediately on phase 2 entry (issue #48)");
        }
    }
}
