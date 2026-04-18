using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public partial class TargetPracticeTests
    {
        static GameConfig TPConfig()
        {
            return new GameConfig
            {
                MatchType = MatchType.TargetPractice,
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
                MineCount = 0,
                BarrelCount = 0,
                SuddenDeathTime = 0f,
                CrateSpawnInterval = 0f,
                TargetPracticeRoundDuration = 60f,
                TargetRadius = 1.5f,
                TargetRespawnTime = 3f,
                TargetStaticNearCount = 2,
                TargetStaticMidCount = 2,
                TargetStaticFarCount = 1,
                TargetMovingHorizontalCount = 1,
                TargetMovingVerticalCount = 1,
                TargetNearPoints = 50,
                TargetMidPoints = 100,
                TargetFarPoints = 200,
                TargetMovingPoints = 150,
                TargetStreakBonus = 50,
                TargetStreakThreshold = 3,
                TargetLongRangeBonus = 100,
                TargetLongRangeDistance = 25f,
                TargetSpeedBonus = 25,
                TargetSpeedBonusWindow = 1f
            };
        }

        [Test]
        public void CreateMatch_TP_SpawnsCorrectTargetCount()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            // 2 near + 2 mid + 1 far + 1 moving-h + 1 moving-v = 7
            Assert.AreEqual(7, state.Targets.Count, "Should spawn 7 targets");
            foreach (var t in state.Targets)
                Assert.IsTrue(t.Active, "All targets should start active");
        }

        [Test]
        public void CreateMatch_TP_AIPlayerIsDead()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            Assert.IsTrue(state.Players[1].IsDead, "AI opponent should be dead in target practice");
            Assert.AreEqual(0f, state.Players[1].Health, 0.01f);
        }

        [Test]
        public void CreateMatch_TP_InfiniteAmmoZeroEnergyCost()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            for (int i = 0; i < state.Players[0].WeaponSlots.Length; i++)
            {
                var w = state.Players[0].WeaponSlots[i];
                if (w.WeaponId == null) continue;
                Assert.AreEqual(-1, w.Ammo, $"Weapon {w.WeaponId} should have infinite ammo");
                Assert.AreEqual(0f, w.EnergyCost, 0.01f, $"Weapon {w.WeaponId} should have zero energy cost");
            }
        }

        [Test]
        public void CreateMatch_TP_TimerStartsAtRoundDuration()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            Assert.AreEqual(60f, state.TargetTimeRemaining, 0.01f);
            Assert.AreEqual(0, state.TargetScore);
        }

        [Test]
        public void TP_MatchDoesNotEndFromDeathCount()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            // AI is dead, but match should still be Playing
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(MatchPhase.Playing, state.Phase,
                "Target practice should not end from death count");
        }

        [Test]
        public void TP_MatchEndsWhenTimerExpires()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            // Tick with a delta equal to full duration
            GameSimulation.Tick(state, 61f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(0, state.WinnerIndex, "Player should be the winner");
        }

        /// <summary>Deactivate all targets except the one at the given index.</summary>
        static void IsolateTarget(GameState state, int keepIdx)
        {
            for (int i = 0; i < state.Targets.Count; i++)
            {
                if (i == keepIdx) continue;
                var t = state.Targets[i];
                t.Active = false;
                t.RespawnTimer = 9999f; // prevent respawn during test
                state.Targets[i] = t;
            }
        }

        [Test]
        public void TP_ExplosionHitsTarget_ScoresPoints()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            // Find a near target and isolate it
            int nearIdx = -1;
            for (int i = 0; i < state.Targets.Count; i++)
            {
                if (state.Targets[i].Type == TargetType.StaticNear)
                {
                    nearIdx = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(nearIdx, 0, "Should have at least one near target");
            IsolateTarget(state, nearIdx);

            var target = state.Targets[nearIdx];

            // Place player far enough to not trigger long-range bonus
            state.Players[0].Position = target.Position + new Vec2(-3f, 0f);

            // Create an explosion right on the target
            state.ExplosionEvents.Add(new ExplosionEvent
            {
                Position = target.Position,
                Radius = 2f
            });

            TargetPractice.Update(state, 0.016f);

            Assert.Greater(state.TargetScore, 0, "Score should increase on hit");
            Assert.IsFalse(state.Targets[nearIdx].Active, "Hit target should be deactivated");
            Assert.AreEqual(1, state.TargetHitEvents.Count, "Should emit a hit event");
        }

        [Test]
        public void TP_HitTarget_RespawnsAfterDelay()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            var target = state.Targets[0];

            // Hit the target
            state.ExplosionEvents.Add(new ExplosionEvent
            {
                Position = target.Position,
                Radius = 2f
            });
            TargetPractice.Update(state, 0.016f);
            Assert.IsFalse(state.Targets[0].Active);

            // Tick partially — not enough to respawn
            state.ExplosionEvents.Clear();
            state.Time += 1f;
            TargetPractice.Update(state, 1f);
            Assert.IsFalse(state.Targets[0].Active, "Should not respawn before timer");

            // Tick enough to trigger respawn
            state.Time += 3f;
            TargetPractice.Update(state, 3f);
            Assert.IsTrue(state.Targets[0].Active, "Should respawn after 3s");
        }

        [Test]
        public void TP_StreakBonus_AfterThreeHits()
        {
            var state = GameSimulation.CreateMatch(TPConfig(), 42);

            // Place 3 targets far apart to avoid multi-hit, deactivate the rest
            for (int i = 0; i < state.Targets.Count; i++)
            {
                var t = state.Targets[i];
                t.Active = false;
                t.RespawnTimer = 9999f;
                state.Targets[i] = t;
            }
            // Activate first 3 targets at known isolated positions
            for (int i = 0; i < 3 && i < state.Targets.Count; i++)
            {
                var t = state.Targets[i];
                t.Active = true;
                t.Position = new Vec2(-10f + i * 20f, 5f); // 20 units apart
                state.Targets[i] = t;
            }

            // Hit 3 consecutive targets — 3rd should get streak bonus
            for (int hit = 0; hit < 3; hit++)
            {
                int idx = -1;
                for (int i = 0; i < state.Targets.Count; i++)
                {
                    if (state.Targets[i].Active) { idx = i; break; }
                }
                Assert.GreaterOrEqual(idx, 0, $"Should have active target for hit {hit}");

                var t = state.Targets[idx];
                state.Players[0].Position = t.Position + new Vec2(-2f, 0f);

                state.ExplosionEvents.Clear();
                state.ExplosionEvents.Add(new ExplosionEvent
                {
                    Position = t.Position,
                    Radius = 0.5f // small radius to only hit this target
                });

                int scoreBefore = state.TargetScore;
                state.Time += 2f; // more than speed bonus window
                TargetPractice.Update(state, 0.016f);
                int gained = state.TargetScore - scoreBefore;

                if (hit >= 2) // 3rd hit (index 2) = streak threshold met
                {
                    Assert.AreEqual(t.Points + 50, gained,
                        $"Hit #{hit + 1} should include +50 streak bonus");
                }
            }
        }

    }
}
