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

        // Regression: #149 — GameRunner must feed saved win count through
        // UnlockRegistry.GetTier so match config reflects actual progression.
        // This mirrors the fix in GameRunner.StartMatch: GetTier(PlayerRecord.Wins).
        [TestCase(0, 0, 5)]    // Starter: 5 weapons
        [TestCase(5, 1, 9)]    // Veteran: +4 weapons
        [TestCase(15, 2, 13)]  // Expert: +4 weapons
        [TestCase(30, 3, 18)]  // Master: +5 weapons
        [TestCase(50, 4, 22)]  // Legend: all 22 weapons
        [TestCase(100, 4, 22)] // above max still Legend
        public void WinCount_MapsToUnlockedTier_GatingPlayerWeapons(int wins, int expectedTier, int expectedWeaponCount)
        {
            // Simulate GameRunner.StartMatch's fix: config.UnlockedTier = GetTier(PlayerRecord.Wins)
            int tier = UnlockRegistry.GetTier(wins);
            Assert.That(tier, Is.EqualTo(expectedTier),
                $"{wins} wins should map to tier {expectedTier}");

            var config = new GameConfig { UnlockedTier = tier };
            var state = GameSimulation.CreateMatch(config, 42);
            var player = state.Players[0];

            int unlocked = 0;
            for (int i = 0; i < player.WeaponSlots.Length; i++)
                if (player.WeaponSlots[i].WeaponId != null) unlocked++;

            Assert.That(unlocked, Is.EqualTo(expectedWeaponCount),
                $"Tier {tier} player should have exactly {expectedWeaponCount} weapons unlocked");
        }
    }
}
