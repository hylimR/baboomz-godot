using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    /// <summary>
    /// Regression tests for WarCry achievement (sm_7) kill counting (#327).
    /// </summary>
    [TestFixture]
    public class AchievementTrackerWarCryTests
    {
        static GameConfig SmallConfig()
        {
            return new GameConfig
            {
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
                BarrelCount = 0,
                MineCount = 0
            };
        }

        [SetUp]
        public void SetUp()
        {
            AchievementTracker.LoadUnlocked(System.Array.Empty<string>());
        }

        [Test]
        public void WarCry_MultiHitWeapon_CountsAsOneKill()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Phase = MatchPhase.Playing;
            AchievementTracker.OnMatchStart(state);

            // Activate WarCry on player 0
            state.Players[0].WarCryTimer = 5f;

            // Kill player 1
            state.Players[1].IsDead = true;
            state.Players[1].Health = 0f;

            // Simulate 4 damage events from a multi-hit weapon (e.g., shotgun)
            // hitting the same dead target — all in the same tick
            for (int i = 0; i < 4; i++)
            {
                state.DamageEvents.Add(new DamageEvent
                {
                    SourceIndex = 0, TargetIndex = 1,
                    Amount = 15f, Position = state.Players[1].Position
                });
            }

            AchievementTracker.Update(state);

            // Should NOT unlock sm_7 (requires 3 kills, this is only 1 unique kill)
            Assert.IsFalse(AchievementTracker.IsUnlocked("sm_7"),
                "Multi-hit weapon killing one target during WarCry should count as 1 kill, not 4");
        }

        [Test]
        public void WarCry_ThreeUniqueKills_AcrossTicks_UnlocksSm7()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AchievementTracker.OnMatchStart(state);

            // Activate WarCry on player 0
            state.Players[0].WarCryTimer = 5f;

            // Kill player 1 in tick 1
            state.Players[1].IsDead = true;
            state.Players[1].Health = 0f;
            state.DamageEvents.Add(new DamageEvent
            {
                SourceIndex = 0, TargetIndex = 1,
                Amount = 100f, Position = state.Players[1].Position
            });
            AchievementTracker.Update(state);
            Assert.IsFalse(AchievementTracker.IsUnlocked("sm_7"),
                "1 kill is not enough for sm_7");

            // Reset events, add 2 more kills via same target index re-killed
            // (simulating respawn in demolition mode or different targets via mob)
            state.DamageEvents.Clear();
            state.Players[1].IsDead = true;
            state.DamageEvents.Add(new DamageEvent
            {
                SourceIndex = 0, TargetIndex = 1,
                Amount = 100f, Position = state.Players[1].Position
            });
            AchievementTracker.Update(state);
            // Even though same target killed again in a new tick, it's still a unique
            // target per tick, so _warCryKills increments
            Assert.IsFalse(AchievementTracker.IsUnlocked("sm_7"),
                "2 kills is not enough for sm_7");

            state.DamageEvents.Clear();
            state.DamageEvents.Add(new DamageEvent
            {
                SourceIndex = 0, TargetIndex = 1,
                Amount = 100f, Position = state.Players[1].Position
            });
            AchievementTracker.Update(state);
            Assert.IsTrue(AchievementTracker.IsUnlocked("sm_7"),
                "3 kills across ticks during WarCry should unlock sm_7");
        }
    }
}
