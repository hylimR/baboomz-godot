using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- DamageEvent SourceIndex tests ---

        [Test]
        public void DamageEvent_SourceIndex_SetForExplosions()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Fire cannon from player 0 at player 1
            state.Players[0].Position = new Vec2(-5f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(5f, 5f);

            // Directly apply an explosion from player 0 near player 1
            state.DamageEvents.Clear();
            CombatResolver.ApplyExplosion(state, state.Players[1].Position,
                2f, 30f, 5f, 0, false);

            Assert.Greater(state.DamageEvents.Count, 0);
            Assert.AreEqual(0, state.DamageEvents[0].SourceIndex,
                "Explosion DamageEvent should have SourceIndex matching the attacker");
        }

        [Test]
        public void DamageEvent_SourceIndex_NegativeForFallDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Simulate fall damage by placing player high and letting them fall
            state.Players[0].Position = new Vec2(0f, 30f);
            state.Players[0].LastGroundedY = 30f;
            state.Players[0].IsGrounded = false;
            state.Players[0].Velocity = new Vec2(0f, -20f);

            // Tick until player lands and takes fall damage
            state.Phase = MatchPhase.Playing;
            bool foundFallDamage = false;
            for (int i = 0; i < 200; i++)
            {
                state.DamageEvents.Clear();
                GameSimulation.Tick(state, 0.016f);
                for (int d = 0; d < state.DamageEvents.Count; d++)
                {
                    if (state.DamageEvents[d].TargetIndex == 0 && state.DamageEvents[d].SourceIndex == -1)
                    {
                        foundFallDamage = true;
                        break;
                    }
                }
                if (foundFallDamage) break;
            }

            Assert.IsTrue(foundFallDamage,
                "Fall damage DamageEvent should have SourceIndex = -1 (environmental)");
        }

        [Test]
        public void Replay_RecordsFrames_DuringMatch()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            var replay = ReplaySystem.StartRecording(state);

            state.Phase = MatchPhase.Playing;

            // Tick 100 frames
            for (int i = 0; i < 100; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(100, replay.Frames.Count, "Should record 100 frames");
            Assert.AreEqual(42, replay.Seed, "Replay seed should match");
            Assert.AreEqual(0.016f, replay.Frames[0].DeltaTime, 0.001f,
                "Frame dt should be recorded");
        }

        [Test]
        public void Replay_StopRecording_ReturnsData()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            ReplaySystem.StartRecording(state);
            state.Phase = MatchPhase.Playing;

            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            var data = ReplaySystem.StopRecording(state);
            Assert.IsNotNull(data, "StopRecording should return data");
            Assert.AreEqual(10, data.Frames.Count);
            Assert.IsNull(state.ReplayRecording, "Recording should be cleared");

            // Further ticks should not record
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(10, data.Frames.Count,
                "No more frames after stop");
        }

        [Test]
        public void Replay_Playback_ProducesSameState()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            var replay = ReplaySystem.StartRecording(state);

            state.Phase = MatchPhase.Playing;

            // Simulate with some input
            for (int i = 0; i < 100; i++)
            {
                if (i == 10) state.Input.FirePressed = true;
                else state.Input.FirePressed = false;
                if (i >= 20 && i <= 30) state.Input.MoveX = 1f;
                else state.Input.MoveX = 0f;
                GameSimulation.Tick(state, 0.016f);
            }

            float originalP0X = state.Players[0].Position.x;
            float originalP0Health = state.Players[0].Health;
            float originalTime = state.Time;
            int originalProjectileId = state.NextProjectileId;

            // Now replay
            var replayState = ReplaySystem.Replay(replay);

            Assert.AreEqual(originalTime, replayState.Time, 0.001f,
                "Replayed time should match original");
            Assert.AreEqual(originalP0X, replayState.Players[0].Position.x, 0.01f,
                "Replayed player 0 X should match original");
            Assert.AreEqual(originalP0Health, replayState.Players[0].Health, 0.01f,
                "Replayed player 0 health should match original");
            Assert.AreEqual(originalProjectileId, replayState.NextProjectileId,
                "Replayed projectile count should match");
        }

        [Test]
        public void Replay_DoesNotReRecord_DuringPlayback()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            var replay = ReplaySystem.StartRecording(state);

            state.Phase = MatchPhase.Playing;

            for (int i = 0; i < 50; i++)
                GameSimulation.Tick(state, 0.016f);

            int frameCount = replay.Frames.Count;
            Assert.AreEqual(50, frameCount);

            // Replay — should NOT add more frames to the original replay
            var replayState = ReplaySystem.Replay(replay);
            Assert.AreEqual(50, replay.Frames.Count,
                "Playback should not add frames to the replay data");
            Assert.IsNull(replayState.ReplayRecording,
                "Playback state should not have recording active");
        }

        [Test]
        public void Replay_AI_Deterministic_AcrossMultipleReplays()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 99);
            var replay = ReplaySystem.StartRecording(state);

            state.Phase = MatchPhase.Playing;

            // Run enough ticks for AI to make decisions (shoot, move)
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            float originalAIX = state.Players[1].Position.x;
            float originalAIHealth = state.Players[1].Health;
            int originalAISlot = state.Players[1].ActiveWeaponSlot;

            // Replay twice — both should produce identical AI state
            var replay1 = ReplaySystem.Replay(replay);
            var replay2 = ReplaySystem.Replay(replay);

            Assert.AreEqual(originalAIX, replay1.Players[1].Position.x, 0.01f,
                "First replay AI X should match original");
            Assert.AreEqual(originalAIX, replay2.Players[1].Position.x, 0.01f,
                "Second replay AI X should match original");
            Assert.AreEqual(originalAIHealth, replay1.Players[1].Health, 0.01f,
                "Replay AI health should match");
            Assert.AreEqual(replay1.Players[1].Position.x, replay2.Players[1].Position.x, 0.001f,
                "Both replays should produce identical AI position");
        }

    }
}
