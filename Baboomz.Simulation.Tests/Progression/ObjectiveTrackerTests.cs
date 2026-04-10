using NUnit.Framework;
using Baboomz;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class ObjectiveTrackerTests
    {
        static GameState MakeState(int playerCount)
        {
            var config = new GameConfig();
            var state = new GameState
            {
                Config = config,
                Players = new PlayerState[playerCount],
                Terrain = new TerrainState(100, 50, 16f, 0f, 0f),
                Time = 0f,
            };
            state.Projectiles = new System.Collections.Generic.List<ProjectileState>();
            state.Mines = new System.Collections.Generic.List<MineState>();
            state.ExplosionEvents = new System.Collections.Generic.List<ExplosionEvent>();
            state.DamageEvents = new System.Collections.Generic.List<DamageEvent>();
            state.SplashEvents = new System.Collections.Generic.List<SplashEvent>();

            for (int i = 0; i < playerCount; i++)
            {
                state.Players[i] = new PlayerState
                {
                    Health = 100f,
                    MaxHealth = 100f,
                    IsDead = false,
                    IsAI = i > 0,
                    IsMob = i > 0,
                };
            }
            return state;
        }

        // ── eliminate_all ──

        [Test]
        public void EliminateAll_CompleteWhenAllEnemiesDead()
        {
            var tracker = new ObjectiveTracker(new LevelObjectiveData { type = "eliminate_all" });
            var state = MakeState(3);

            tracker.Update(state, 0.016f);
            Assert.IsFalse(tracker.IsComplete);

            state.Players[1].IsDead = true;
            tracker.Update(state, 0.016f);
            Assert.IsFalse(tracker.IsComplete); // player 2 still alive

            state.Players[2].IsDead = true;
            tracker.Update(state, 0.016f);
            Assert.IsTrue(tracker.IsComplete);
        }

        [Test]
        public void EliminateAll_FailsWhenPlayerDies()
        {
            var tracker = new ObjectiveTracker(new LevelObjectiveData { type = "eliminate_all" });
            var state = MakeState(2);

            state.Players[0].IsDead = true;
            tracker.Update(state, 0.016f);
            Assert.IsTrue(tracker.IsFailed);
        }

        // ── defeat_boss ──

        [Test]
        public void DefeatBoss_CompleteWhenBossDies()
        {
            var tracker = new ObjectiveTracker(new LevelObjectiveData
            {
                type = "defeat_boss",
                bossType = "iron_sentinel"
            });
            var state = MakeState(2);
            state.Players[1].BossType = "iron_sentinel";
            tracker.SetBossIndex(1);

            tracker.Update(state, 0.016f);
            Assert.IsFalse(tracker.IsComplete);

            state.Players[1].IsDead = true;
            tracker.Update(state, 0.016f);
            Assert.IsTrue(tracker.IsComplete);
        }

        // ── survive_time ──

        [Test]
        public void SurviveTime_CompleteAfterDuration()
        {
            var tracker = new ObjectiveTracker(new LevelObjectiveData
            {
                type = "survive_time",
                timeLimit = 5f
            });
            var state = MakeState(2);

            // Tick 4 seconds
            for (int i = 0; i < 4; i++)
            {
                tracker.Update(state, 1f);
                Assert.IsFalse(tracker.IsComplete);
            }

            Assert.AreEqual(1f, tracker.TimeRemaining, 0.01f);

            // Tick final second
            tracker.Update(state, 1f);
            Assert.IsTrue(tracker.IsComplete);
            Assert.AreEqual(0f, tracker.TimeRemaining, 0.01f);
        }

        [Test]
        public void SurviveTime_FailsIfPlayerDies()
        {
            var tracker = new ObjectiveTracker(new LevelObjectiveData
            {
                type = "survive_time",
                timeLimit = 10f
            });
            var state = MakeState(2);

            tracker.Update(state, 1f);
            Assert.IsFalse(tracker.IsFailed);

            state.Players[0].IsDead = true;
            tracker.Update(state, 1f);
            Assert.IsTrue(tracker.IsFailed);
        }

        // ── destroy_target ──

        [Test]
        public void DestroyTarget_CompleteWhenAllDestroyed()
        {
            var tracker = new ObjectiveTracker(new LevelObjectiveData
            {
                type = "destroy_target",
                targetCount = 3
            });
            var state = MakeState(2);

            Assert.AreEqual(3, tracker.TargetsRemaining);

            tracker.OnTargetDestroyed();
            tracker.Update(state, 0.016f);
            Assert.IsFalse(tracker.IsComplete);
            Assert.AreEqual(2, tracker.TargetsRemaining);

            tracker.OnTargetDestroyed();
            tracker.OnTargetDestroyed();
            tracker.Update(state, 0.016f);
            Assert.IsTrue(tracker.IsComplete);
            Assert.AreEqual(0, tracker.TargetsRemaining);
        }

        // ── survive_waves ──

        [Test]
        public void SurviveWaves_TracksWaveProgression()
        {
            var tracker = new ObjectiveTracker(new LevelObjectiveData
            {
                type = "survive_waves",
                waveCount = 2,
                waves = new[]
                {
                    new LevelWaveData { delay = 0, enemies = new LevelEnemyData[0] },
                    new LevelWaveData { delay = 3, enemies = new LevelEnemyData[0] },
                }
            });
            var state = MakeState(1); // just the player

            Assert.AreEqual(0, tracker.CurrentWave);
            Assert.AreEqual(2, tracker.TotalWaves);

            // First tick — wave 0 spawn timer fires immediately (delay=0)
            tracker.Update(state, 0.016f);
            Assert.IsTrue(tracker.WaveActive || tracker.WaveSpawnTimer <= 0f);
        }

        [Test]
        public void SurviveWaves_CompleteAfterAllWaves()
        {
            var tracker = new ObjectiveTracker(new LevelObjectiveData
            {
                type = "survive_waves",
                waveCount = 1,
                waves = new[]
                {
                    new LevelWaveData { delay = 0, enemies = new LevelEnemyData[0] },
                }
            });
            var state = MakeState(2);

            // Start wave
            tracker.Update(state, 0.016f);
            tracker.MarkWaveSpawned();

            // Kill wave enemy
            state.Players[1].IsDead = true;
            tracker.Update(state, 0.016f);

            Assert.AreEqual(1, tracker.CurrentWave);
            Assert.IsTrue(tracker.IsComplete);
        }

        // ── null/default handling ──

        [Test]
        public void NullObjective_DefaultsToEliminateAll()
        {
            var tracker = new ObjectiveTracker(null);
            Assert.AreEqual("eliminate_all", tracker.ObjectiveType);
        }

        [Test]
        public void CompletedObjective_DoesNotRevert()
        {
            var tracker = new ObjectiveTracker(new LevelObjectiveData { type = "survive_time", timeLimit = 1f });
            var state = MakeState(1);

            tracker.Update(state, 2f);
            Assert.IsTrue(tracker.IsComplete);

            // Further updates don't change state
            tracker.Update(state, 1f);
            Assert.IsTrue(tracker.IsComplete);
        }
    }
}
