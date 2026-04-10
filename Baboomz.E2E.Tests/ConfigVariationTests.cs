using System;

namespace Baboomz.E2E.Tests
{
    /// <summary>
    /// End-to-end tests for configuration variations.
    /// Verifies different difficulty settings, player counts, and config tweaks
    /// all produce valid matches.
    /// </summary>
    [TestFixture]
    public class ConfigVariationTests
    {
        private const float Dt = 1f / 60f;
        private const int MaxTicks = 30000;

        [TestCase(80f)]   // Hard
        [TestCase(100f)]  // Normal
        [TestCase(150f)]  // Easy
        public void DifferentMaxHealth_MatchCompletes(float maxHealth)
        {
            var config = new GameConfig
            {
                DefaultMaxHealth = maxHealth,
                UnlockedTier = UnlockRegistry.GetTier(0)
            };
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42, state.Players.Length);
            BossLogic.Reset(42, state.Players.Length);
            int ticks = 0;
            while (state.Phase != MatchPhase.Ended && ticks < MaxTicks)
            {
                GameSimulation.Tick(state, Dt);
                ticks++;
            }

            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Ended));
        }

        [TestCase(2f)]   // Hard AI
        [TestCase(5f)]   // Normal AI
        [TestCase(12f)]  // Easy AI
        public void DifferentAIShootInterval_MatchCompletes(float shootInterval)
        {
            var config = new GameConfig
            {
                AIShootInterval = shootInterval,
                UnlockedTier = UnlockRegistry.GetTier(0)
            };
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42, state.Players.Length);
            BossLogic.Reset(42, state.Players.Length);
            int ticks = 0;
            while (state.Phase != MatchPhase.Ended && ticks < MaxTicks)
            {
                GameSimulation.Tick(state, Dt);
                ticks++;
            }

            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Ended));
        }

        [Test]
        public void EasyDifficulty_PlayerHasMoreHealth()
        {
            var easyConfig = new GameConfig { DefaultMaxHealth = 150f };
            easyConfig.UnlockedTier = UnlockRegistry.GetTier(0);
            var easyState = GameSimulation.CreateMatch(easyConfig, 42);

            var hardConfig = new GameConfig { DefaultMaxHealth = 80f };
            hardConfig.UnlockedTier = UnlockRegistry.GetTier(0);
            var hardState = GameSimulation.CreateMatch(hardConfig, 42);

            Assert.That(easyState.Players[0].MaxHealth,
                Is.GreaterThan(hardState.Players[0].MaxHealth));
        }

        [Test]
        public void CustomPlayerName_PropagatedToState()
        {
            var config = new GameConfig
            {
                Player1Name = "TestHero",
                UnlockedTier = UnlockRegistry.GetTier(0)
            };
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.That(state.Players[0].Name, Is.EqualTo("TestHero"));
        }

        [Test]
        public void WindForce_IsSet()
        {
            var config = new GameConfig();
            config.UnlockedTier = UnlockRegistry.GetTier(0);
            var state = GameSimulation.CreateMatch(config, 42);

            // Wind is randomized — just verify it's a valid number
            Assert.That(float.IsNaN(state.WindForce), Is.False);
            Assert.That(float.IsInfinity(state.WindForce), Is.False);
        }

        [Test]
        public void Gravity_AffectsProjectiles()
        {
            var config = new GameConfig();
            config.UnlockedTier = UnlockRegistry.GetTier(0);
            var state = GameSimulation.CreateMatch(config, 42);
            Assert.That(state.Config.Gravity, Is.GreaterThan(0f),
                "Default gravity should be positive");
        }
    }
}
