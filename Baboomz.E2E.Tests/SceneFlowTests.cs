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

        [Test]
        public void PostMatchProgression_XPAndChallengesComputed_Issue55()
        {
            // Issue #55: post-match progression data (XP, rank, challenges, mastery)
            // must be computable from a completed match state.
            var config = new GameConfig();
            config.UnlockedTier = UnlockRegistry.GetTier(0);

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42, state.Players.Length);
            BossLogic.Reset(42, state.Players.Length);

            while (state.Phase != MatchPhase.Ended)
                GameSimulation.Tick(state, Dt);

            ref PlayerState p = ref state.Players[0];
            var matchStats = new MatchStats
            {
                Won = state.WinnerIndex == 0,
                Draw = state.WinnerIndex < 0,
                TotalDamage = p.TotalDamageDealt,
                ShotsFired = p.ShotsFired,
                DirectHits = p.DirectHits,
                DamageTaken = p.TotalDamageTaken,
                MaxSingleDamage = p.MaxSingleDamage,
                LandedFirstBlood = state.FirstBloodPlayerIndex == 0
            };

            // XP calculation should produce valid results
            var xpResult = RankSystem.CalculateMatchXP(matchStats);
            Assert.That(xpResult.TotalXP, Is.GreaterThan(0), "Should earn some XP");
            Assert.That(xpResult.BaseXP, Is.GreaterThan(0), "Should have base XP");

            // Daily challenges should be evaluable
            var now = System.DateTime.Now;
            var challenges = ChallengeSystem.GetDailyChallenges(now.Year, now.Month, now.Day);
            Assert.That(challenges.Length, Is.EqualTo(3), "Should have 3 daily challenges");

            var challengeStats = ChallengeSystem.BuildStats(state, 0);
            var results = ChallengeSystem.EvaluateChallenges(challenges, challengeStats);
            Assert.That(results.Length, Is.EqualTo(3), "Should evaluate all 3 challenges");

            // Weapon mastery calculation should work
            int masteryXP = WeaponMasteryCalc.Calculate(p.DirectHits, 0, true);
            Assert.That(masteryXP, Is.GreaterThanOrEqualTo(0), "Mastery XP should be non-negative");
        }

        [Test]
        public void AI_WithGrapplingHookAndMend_DoesNotCrash_Issue91()
        {
            // Issue #91: AI should handle GrapplingHook and Mend skills
            // without crashing. Full stochastic testing is impractical,
            // but we verify the skill pipeline processes these types correctly.
            var config = new GameConfig();
            config.UnlockedTier = UnlockRegistry.GetTier(0);

            // Give AI skills that include GrapplingHook and Mend
            var state = GameSimulation.CreateMatch(config, 42,
                playerSkill0: 1, playerSkill1: 17); // grapple + mend for player
            AILogic.Reset(42, state.Players.Length);
            BossLogic.Reset(42, state.Players.Length);

            // Force AI to have GrapplingHook and Mend
            if (state.Players[1].SkillSlots != null && state.Players[1].SkillSlots.Length >= 2)
            {
                state.Players[1].SkillSlots[0].Type = SkillType.GrapplingHook;
                state.Players[1].SkillSlots[0].SkillId = "grapple";
                state.Players[1].SkillSlots[1].Type = SkillType.Mend;
                state.Players[1].SkillSlots[1].SkillId = "mend";
            }

            // Put AI in low-HP danger scenario to trigger Mend
            state.Players[1].Health = state.Players[1].MaxHealth * 0.3f;

            // Tick 500 frames — should not crash with these skills equipped
            for (int i = 0; i < 500; i++)
            {
                GameSimulation.Tick(state, Dt);
                if (state.Phase == MatchPhase.Ended) break;
            }

            Assert.Pass("AI with GrapplingHook/Mend skills ran without error");
        }
    }
}
