using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class TargetPracticeTests
    {
        [Test]
        public void TP_StreakResets_OnMiss()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            // Isolate 3 targets far apart
            for (int i = 0; i < state.Targets.Count; i++)
            {
                var t = state.Targets[i];
                t.Active = false;
                t.RespawnTimer = 9999f;
                state.Targets[i] = t;
            }
            for (int i = 0; i < 3 && i < state.Targets.Count; i++)
            {
                var t = state.Targets[i];
                t.Active = true;
                t.Position = new Vec2(-10f + i * 20f, 5f);
                state.Targets[i] = t;
            }

            // Build up 3 consecutive hits
            for (int hit = 0; hit < 3; hit++)
            {
                int idx = -1;
                for (int i = 0; i < state.Targets.Count; i++)
                {
                    if (state.Targets[i].Active) { idx = i; break; }
                }
                var t = state.Targets[idx];
                state.Players[0].Position = t.Position + new Vec2(-2f, 0f);
                state.ExplosionEvents.Clear();
                state.ExplosionEvents.Add(new ExplosionEvent { Position = t.Position, Radius = 0.5f });
                state.Time += 2f;
                TargetPractice.Update(state, 0.016f);
            }

            Assert.AreEqual(3, state.TargetConsecutiveHits);

            // Miss: explosion that hits no target
            state.ExplosionEvents.Clear();
            state.ExplosionEvents.Add(new ExplosionEvent
            {
                Position = new Vec2(999f, 999f),
                Radius = 1f
            });
            TargetPractice.Update(state, 0.016f);
            TargetPractice.ResetStreakOnMiss(state);

            Assert.AreEqual(0, state.TargetConsecutiveHits, "Streak should reset on miss");
        }

        [Test]
        public void TP_LongRangeBonus()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            // Find the far target and isolate it
            int farIdx = -1;
            for (int i = 0; i < state.Targets.Count; i++)
            {
                if (state.Targets[i].Type == TargetType.StaticFar)
                {
                    farIdx = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(farIdx, 0, "Should have a far target");
            IsolateTarget(state, farIdx);

            var target = state.Targets[farIdx];

            // Place player 30 units away (> 25 threshold)
            state.Players[0].Position = target.Position + new Vec2(-30f, 0f);

            state.ExplosionEvents.Add(new ExplosionEvent
            {
                Position = target.Position,
                Radius = 0.5f
            });
            state.Time = 10f; // avoid speed bonus
            state.TargetLastHitTime = -10f;
            TargetPractice.Update(state, 0.016f);

            // 200 base + 100 long-range = 300 (first hit, no streak)
            Assert.AreEqual(300, state.TargetScore, "Should get long-range bonus");
        }

        [Test]
        public void TP_SpeedBonus_QuickConsecutiveHits()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            // Isolate first two targets far apart
            for (int i = 0; i < state.Targets.Count; i++)
            {
                var t = state.Targets[i];
                t.Active = false;
                t.RespawnTimer = 9999f;
                state.Targets[i] = t;
            }
            // Activate 2 targets at known positions, close to player (no long-range bonus)
            var ta = state.Targets[0];
            ta.Active = true;
            ta.Position = new Vec2(-5f, 5f);
            state.Targets[0] = ta;

            var tb = state.Targets[1];
            tb.Active = true;
            tb.Position = new Vec2(5f, 5f); // 10 units from first, no overlap
            state.Targets[1] = tb;

            // First hit
            state.Players[0].Position = state.Targets[0].Position + new Vec2(-2f, 0f);
            state.ExplosionEvents.Add(new ExplosionEvent { Position = state.Targets[0].Position, Radius = 0.5f });
            state.Time = 5f;
            TargetPractice.Update(state, 0.016f);
            int scoreAfterFirst = state.TargetScore;

            // Second hit within 1s
            state.ExplosionEvents.Clear();
            state.Players[0].Position = state.Targets[1].Position + new Vec2(-2f, 0f);
            state.ExplosionEvents.Add(new ExplosionEvent { Position = state.Targets[1].Position, Radius = 0.5f });
            state.Time = 5.5f; // 0.5s later, within 1s window
            TargetPractice.Update(state, 0.016f);

            int secondHitPoints = state.TargetScore - scoreAfterFirst;
            Assert.AreEqual(state.Targets[1].Points + 25, secondHitPoints,
                "Second quick hit should include +25 speed bonus");
        }

        [Test]
        public void TP_Deathmatch_DoesNotInitTargets()
        {
            var config = TPConfig();
            config.MatchType = MatchType.Deathmatch;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.AreEqual(0, state.Targets.Count, "Deathmatch should not have targets");
        }

        [Test]
        public void TP_MovingTarget_PositionChanges()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            int movIdx = -1;
            for (int i = 0; i < state.Targets.Count; i++)
            {
                if (state.Targets[i].Type == TargetType.MovingHorizontal)
                {
                    movIdx = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(movIdx, 0, "Should have a moving horizontal target");

            Vec2 posBefore = state.Targets[movIdx].Position;
            GameSimulation.Tick(state, 1f);
            Vec2 posAfter = state.Targets[movIdx].Position;

            Assert.AreNotEqual(posBefore.x, posAfter.x, "Moving target X should change over time");
        }

        [Test]
        public void TP_VerticalTarget_BobsAroundSpawnY_NotCurrentGround()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            // Find a vertical moving target
            int vIdx = -1;
            for (int i = 0; i < state.Targets.Count; i++)
            {
                if (state.Targets[i].Type == TargetType.MovingVertical)
                {
                    vIdx = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(vIdx, 0, "Should have a moving vertical target");

            float spawnY = state.Targets[vIdx].SpawnY;

            // Destroy terrain under the target by clearing a wide column
            var t = state.Targets[vIdx];
            int px = state.Terrain.WorldToPixelX(t.Position.x);
            px = Math.Clamp(px, 0, state.Terrain.Width - 1);
            for (int row = 0; row < state.Terrain.Height; row++)
                state.Terrain.SetSolid(px, row, false);

            // Tick the simulation a few times — target should keep bobbing around SpawnY
            for (int tick = 0; tick < 30; tick++)
                TargetPractice.Update(state, 0.05f);

            var afterTarget = state.Targets[vIdx];
            float deviation = MathF.Abs(afterTarget.Position.y - spawnY);
            float maxBob = state.Config.TargetMoveAmplitude + 0.1f;
            Assert.LessOrEqual(deviation, maxBob,
                $"Vertical target should stay within {maxBob} of SpawnY ({spawnY}), " +
                $"but was at {afterTarget.Position.y} (deviation {deviation})");
        }
    }
}
