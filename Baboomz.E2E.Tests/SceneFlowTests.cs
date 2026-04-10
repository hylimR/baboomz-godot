using System;

namespace Baboomz.E2E.Tests
{
    /// <summary>
    /// End-to-end tests for the menu → game → result data flow.
    /// Exercises the same config→simulation→result contract that Godot scenes use,
    /// without depending on Godot types (GameModeContext, Difficulty).
    /// </summary>
    [TestFixture]
    public class SceneFlowTests
    {
        private const float Dt = 1f / 60f;
        private const int MaxTicks = 30000;

        [Test]
        public void HardDifficulty_ConfigApplied_MatchCompletes()
        {
            // Simulates: MainMenu sets Hard → GameRunner.StartMatch reads config
            var config = new GameConfig
            {
                AIAimErrorMargin = 2f,
                AIShootInterval = 2f,
                DefaultMaxHealth = 80f,
                AIDifficultyLevel = 2,
                Player1Name = "E2EPlayer",
                UnlockedTier = UnlockRegistry.GetTier(0)
            };

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42, state.Players.Length);
            BossLogic.Reset(42, state.Players.Length);

            Assert.That(state.Players[0].Name, Is.EqualTo("E2EPlayer"));
            Assert.That(state.Players[0].MaxHealth, Is.EqualTo(80f));

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
            var config = new GameConfig
            {
                AIAimErrorMargin = 12f,
                AIShootInterval = 5f,
                DefaultMaxHealth = 150f,
                AIDifficultyLevel = 0,
                UnlockedTier = UnlockRegistry.GetTier(0)
            };

            var state = GameSimulation.CreateMatch(config, 42);
            Assert.That(state.Players[0].MaxHealth, Is.EqualTo(150f));
        }

        [Test]
        public void FullFlow_MenuToMatchToResult()
        {
            // Step 1: MainMenu picks name + Normal difficulty
            var config = new GameConfig
            {
                Player1Name = "Tester",
                AIDifficultyLevel = 1,
                UnlockedTier = UnlockRegistry.GetTier(0)
            };

            // Step 2: GameRunner.StartMatch() creates match
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42, state.Players.Length);
            BossLogic.Reset(42, state.Players.Length);
            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Playing));

            // Step 3: Match ticks until ended
            int ticks = 0;
            while (state.Phase != MatchPhase.Ended && ticks < MaxTicks)
            {
                GameSimulation.Tick(state, Dt);
                ticks++;
            }

            // Step 5: MatchResultPanel reads result state
            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Ended));

            bool anyDamageDealt = false;
            for (int i = 0; i < state.Players.Length; i++)
            {
                if (state.Players[i].TotalDamageDealt > 0)
                    anyDamageDealt = true;
            }
            Assert.That(anyDamageDealt, Is.True, "At least one player should have dealt damage");
        }

        [Test]
        public void SkillSelection_PropagatedToMatch()
        {
            var config = new GameConfig();
            config.UnlockedTier = UnlockRegistry.GetTier(0);

            // Pass custom skill indices (same as GameModeContext.SelectedSkillSlot0/1)
            var state = GameSimulation.CreateMatch(config, 42,
                playerSkill0: 2, playerSkill1: 5);

            Assert.That(state.Players[0].SkillSlots, Is.Not.Null);
            Assert.That(state.Players[0].SkillSlots.Length, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void PlayAgain_CanCreateNewMatchAfterEnd()
        {
            var config = new GameConfig();
            config.UnlockedTier = UnlockRegistry.GetTier(0);

            // First match
            var state1 = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42, state1.Players.Length);
            BossLogic.Reset(42, state1.Players.Length);
            while (state1.Phase != MatchPhase.Ended)
                GameSimulation.Tick(state1, Dt);

            Assert.That(state1.Phase, Is.EqualTo(MatchPhase.Ended));

            // Play Again — new match with different seed
            var state2 = GameSimulation.CreateMatch(config, 99);
            AILogic.Reset(99, state2.Players.Length);
            BossLogic.Reset(99, state2.Players.Length);

            Assert.That(state2.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(state2.WinnerIndex, Is.EqualTo(-1));
            Assert.That(state2.Players[0].Health, Is.GreaterThan(0f));
        }

        [Test]
        public void BackToBackMatches_NoStateLeak()
        {
            var config = new GameConfig();
            config.UnlockedTier = UnlockRegistry.GetTier(0);

            // Run 3 matches in a row
            for (int round = 0; round < 3; round++)
            {
                int seed = 100 + round;
                var state = GameSimulation.CreateMatch(config, seed);
                AILogic.Reset(seed, state.Players.Length);
                BossLogic.Reset(seed, state.Players.Length);
                int ticks = 0;
                while (state.Phase != MatchPhase.Ended && ticks < MaxTicks)
                {
                    GameSimulation.Tick(state, Dt);
                    ticks++;
                }

                Assert.That(state.Phase, Is.EqualTo(MatchPhase.Ended),
                    $"Round {round + 1}: match should end");

                // Fresh match should have clean state
                var fresh = GameSimulation.CreateMatch(config, seed + 50);
                Assert.That(fresh.WinnerIndex, Is.EqualTo(-1),
                    $"Round {round + 1}: new match should have no winner");
                Assert.That(fresh.Time, Is.LessThan(0.01f),
                    $"Round {round + 1}: new match should start at time 0");
            }
        }
    }
}
