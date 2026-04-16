using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Boss first-tick initialization tests (issue #170) ---

        private static float[] GetBossTimerArray(string fieldName)
        {
            var field = typeof(BossLogic).GetField(
                fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(field, $"BossLogic.{fieldName} field must exist");
            return (float[])field.GetValue(null);
        }

        private GameState SetupBossState(string bossType, int bossIndex = 1)
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[bossIndex].BossType = bossType;
            state.Players[bossIndex].IsMob = true;
            state.Players[bossIndex].IsAI = true;
            state.Players[bossIndex].MaxHealth = 200f;
            state.Players[bossIndex].Health = 200f;
            state.Players[bossIndex].BossPhase = 0;

            BossLogic.Reset(42, state.Players.Length);
            return state;
        }

        [Test]
        public void IronSentinel_DoesNotFireOnFirstFrame_Issue170()
        {
            var state = SetupBossState("iron_sentinel");
            int projBefore = state.Projectiles.Count;

            // Single tick — boss should NOT fire because attackTimer is now initialized
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(projBefore, state.Projectiles.Count,
                "Iron Sentinel must not fire on first frame (issue #170)");

            float[] timers = GetBossTimerArray("attackTimer");
            Assert.Greater(timers[1], 0f,
                "attackTimer must be initialized to a future time after first tick");
        }

        [Test]
        public void GlacialCannon_DoesNotFireOnFirstFrame_Issue170()
        {
            var state = SetupBossState("glacial_cannon");
            int projBefore = state.Projectiles.Count;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(projBefore, state.Projectiles.Count,
                "Glacial Cannon must not fire on first frame (issue #170)");

            float[] timers = GetBossTimerArray("attackTimer");
            Assert.Greater(timers[1], 0f,
                "attackTimer must be initialized to a future time after first tick");
        }

        [Test]
        public void ForgeColossus_DoesNotFireOnFirstFrame_Issue170()
        {
            var state = SetupBossState("forge_colossus");
            int projBefore = state.Projectiles.Count;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(projBefore, state.Projectiles.Count,
                "Forge Colossus must not fire on first frame (issue #170)");

            float[] timers = GetBossTimerArray("attackTimer");
            Assert.Greater(timers[1], 0f,
                "attackTimer must be initialized to a future time after first tick");
        }

        [Test]
        public void BaronCogsworth_DoesNotFireOnFirstFrame_Issue170()
        {
            var state = SetupBossState("baron_cogsworth");
            int projBefore = state.Projectiles.Count;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(projBefore, state.Projectiles.Count,
                "Baron Cogsworth must not fire on first frame (issue #170)");

            float[] timers = GetBossTimerArray("attackTimer");
            Assert.Greater(timers[1], 0f,
                "attackTimer must be initialized to a future time after first tick");

            float[] specialTimers = GetBossTimerArray("specialTimer");
            Assert.Greater(specialTimers[1], 0f,
                "specialTimer (gear bomb) must be initialized to a future time after first tick");
        }
    }
}
