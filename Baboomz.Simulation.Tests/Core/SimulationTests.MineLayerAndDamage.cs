using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Mine Layer skill tests ---

        [Test]
        public void MineLay_ExistsInConfig()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Skills.Length >= 11, "Should have at least 11 skills");
            Assert.AreEqual("mine_layer", config.Skills[10].SkillId);
            Assert.AreEqual(SkillType.MineLay, config.Skills[10].Type);
            Assert.AreEqual(25f, config.Skills[10].EnergyCost);
            Assert.AreEqual(30f, config.Skills[10].Value); // mine damage
        }

        [Test]
        public void MineLay_PlacesMineAtAimTarget()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].AimAngle = -45f; // aim downward

            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "mine_layer", Type = SkillType.MineLay,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 0f,
                    Range = 10f, Value = 30f
                },
                new SkillSlotState()
            };
            state.Players[0].Energy = 100f;

            int minesBefore = state.Mines.Count;
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(minesBefore + 1, state.Mines.Count,
                "Mine Layer should place a mine");
            var mine = state.Mines[state.Mines.Count - 1];
            Assert.IsTrue(mine.Active);
            Assert.AreEqual(0, mine.OwnerIndex);
            Assert.AreEqual(30f, mine.Damage);
            Assert.AreEqual(15f, mine.Lifetime);
        }

        [Test]
        public void MineLay_MaxTwoMinesPerPlayer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;

            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "mine_layer", Type = SkillType.MineLay,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 0f,
                    Range = 10f, Value = 30f
                },
                new SkillSlotState()
            };
            state.Players[0].Energy = 1000f;

            // Place 3 mines — third should deactivate the first
            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.ActivateSkill(state, 0, 0);

            int activeOwned = 0;
            for (int i = 0; i < state.Mines.Count; i++)
                if (state.Mines[i].Active && state.Mines[i].OwnerIndex == 0)
                    activeOwned++;

            Assert.LessOrEqual(activeOwned, 2,
                "Player should have at most 2 active mines");
        }

        [Test]
        public void MineLay_OverflowRemovesActuallyOldestByPlacedTime()
        {
            // Regression test for #33: overflow eviction previously picked the
            // first-found-owned-index, which is unstable when a deactivated slot
            // gets reused. Ensure we remove the mine with the smallest PlacedTime.
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "mine_layer", Type = SkillType.MineLay,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 0f,
                    Range = 10f, Value = 30f
                },
                new SkillSlotState()
            };
            state.Players[0].Energy = 1000f;

            // Seed a deactivated-slot scenario: place mine A, kill it, then fill slots
            // with mines B and C at increasing times. When we place D it should evict
            // the earliest of {B, C} — which is B — not whichever the loop finds first.
            state.Time = 1f;
            SkillSystem.ActivateSkill(state, 0, 0); // Mine A @ t=1
            // Simulate mine A exploding/deactivating: flip its slot dead.
            int aIdx = -1;
            for (int i = 0; i < state.Mines.Count; i++)
                if (state.Mines[i].Active && state.Mines[i].OwnerIndex == 0) { aIdx = i; break; }
            var a = state.Mines[aIdx];
            a.Active = false;
            state.Mines[aIdx] = a;

            state.Time = 5f;
            SkillSystem.ActivateSkill(state, 0, 0); // Mine B @ t=5
            state.Time = 10f;
            SkillSystem.ActivateSkill(state, 0, 0); // Mine C @ t=10

            // Track B's and C's PlacedTime before overflow
            float bTime = float.MaxValue;
            float cTime = float.MinValue;
            for (int i = 0; i < state.Mines.Count; i++)
            {
                if (!state.Mines[i].Active || state.Mines[i].OwnerIndex != 0) continue;
                if (state.Mines[i].PlacedTime < bTime) bTime = state.Mines[i].PlacedTime;
                if (state.Mines[i].PlacedTime > cTime) cTime = state.Mines[i].PlacedTime;
            }
            Assert.AreEqual(5f, bTime, 0.01f);
            Assert.AreEqual(10f, cTime, 0.01f);

            // Place mine D — B should be evicted (oldest), C should survive alongside D
            state.Time = 15f;
            SkillSystem.ActivateSkill(state, 0, 0); // Mine D @ t=15

            bool bStillActive = false;
            bool cStillActive = false;
            bool dActive = false;
            for (int i = 0; i < state.Mines.Count; i++)
            {
                if (!state.Mines[i].Active || state.Mines[i].OwnerIndex != 0) continue;
                if (MathF.Abs(state.Mines[i].PlacedTime - 5f) < 0.01f) bStillActive = true;
                if (MathF.Abs(state.Mines[i].PlacedTime - 10f) < 0.01f) cStillActive = true;
                if (MathF.Abs(state.Mines[i].PlacedTime - 15f) < 0.01f) dActive = true;
            }
            Assert.IsFalse(bStillActive, "Oldest mine (B, t=5) should have been evicted");
            Assert.IsTrue(cStillActive, "Newer mine (C, t=10) should still be active");
            Assert.IsTrue(dActive, "Newly placed mine (D, t=15) should be active");
        }

        [Test]
        public void MineLay_DoesNotTriggerOnOwner()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place player 0 at a known position
            state.Players[0].Position = new Vec2(0f, 5f);

            // Add a mine owned by player 0 at player 0's position
            state.Mines.Add(new MineState
            {
                Position = state.Players[0].Position,
                TriggerRadius = 2f,
                ExplosionRadius = 3f,
                Damage = 30f,
                Active = true,
                Lifetime = 15f,
                OwnerIndex = 0
            });

            float healthBefore = state.Players[0].Health;

            // Tick — mine should NOT trigger on its owner
            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(healthBefore, state.Players[0].Health, 0.01f,
                "Player-laid mine should not trigger on its owner");
            Assert.IsTrue(state.Mines[state.Mines.Count - 1].Active,
                "Mine should still be active (not triggered by owner)");
        }

        [Test]
        public void MineLay_ExplosionCreditsOwner()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Player 1 is at a known position
            state.Players[1].Position = new Vec2(5f, 5f);

            // Add a mine owned by player 0 at player 1's position
            state.Mines.Add(new MineState
            {
                Position = state.Players[1].Position,
                TriggerRadius = 2f,
                ExplosionRadius = 3f,
                Damage = 30f,
                Active = true,
                Lifetime = 15f,
                OwnerIndex = 0
            });

            state.DamageEvents.Clear();
            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.016f);

            // Check that explosion credited player 0
            bool foundOwnerCredit = false;
            for (int d = 0; d < state.DamageEvents.Count; d++)
            {
                if (state.DamageEvents[d].SourceIndex == 0 && state.DamageEvents[d].TargetIndex == 1)
                {
                    foundOwnerCredit = true;
                    break;
                }
            }
            Assert.IsTrue(foundOwnerCredit,
                "Mine explosion should credit the mine owner (SourceIndex = OwnerIndex)");
        }

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
