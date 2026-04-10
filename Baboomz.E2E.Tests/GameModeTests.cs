using System;

namespace Baboomz.E2E.Tests
{
    /// <summary>
    /// End-to-end tests for different game modes.
    /// Verifies each MatchType can create a match, tick, and reach completion.
    /// </summary>
    [TestFixture]
    public class GameModeTests
    {
        private const float Dt = 1f / 60f;
        private const int MaxTicks = 30000;

        private static GameState CreateMatch(MatchType matchType, int seed = 42)
        {
            var config = new GameConfig { MatchType = matchType };
            config.UnlockedTier = UnlockRegistry.GetTier(0);
            var state = GameSimulation.CreateMatch(config, seed);
            AILogic.Reset(seed, state.Players.Length);
            BossLogic.Reset(seed, state.Players.Length);
            return state;
        }

        [Test]
        public void Deathmatch_CreatesAndCompletes()
        {
            var state = CreateMatch(MatchType.Deathmatch);

            int ticks = RunUntilEnd(state);
            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Ended));
            Assert.That(ticks, Is.LessThan(MaxTicks), "Match should end in time");
        }

        [Test]
        public void TargetPractice_CreatesAndCompletes()
        {
            var state = CreateMatch(MatchType.TargetPractice);

            int ticks = RunUntilEnd(state);
            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Ended));
        }

        [Test]
        public void Survival_CreateSucceeds()
        {
            var state = CreateMatch(MatchType.Survival);
            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(state.Players.Length, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void Campaign_CreateSucceeds()
        {
            var state = CreateMatch(MatchType.Campaign);
            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(state.Players.Length, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void KingOfTheHill_CreateSucceeds()
        {
            var state = CreateMatch(MatchType.KingOfTheHill);
            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(state.Players.Length, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void ArmsRace_CreateSucceeds()
        {
            var state = CreateMatch(MatchType.ArmsRace);
            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Playing));
        }

        [Test]
        public void Demolition_CreateSucceeds()
        {
            var state = CreateMatch(MatchType.Demolition);
            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Playing));
        }

        [Test]
        public void Payload_CreateSucceeds()
        {
            var state = CreateMatch(MatchType.Payload);
            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Playing));
        }

        [Test]
        public void Roulette_CreateSucceeds()
        {
            var state = CreateMatch(MatchType.Roulette);
            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Playing));
        }

        [Test]
        public void CaptureTheFlag_CreateSucceeds()
        {
            var state = CreateMatch(MatchType.CaptureTheFlag);
            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Playing));
        }

        [Test]
        public void Headhunter_CreateSucceeds()
        {
            var state = CreateMatch(MatchType.Headhunter);
            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Playing));
        }

        [Test]
        public void Territories_CreateSucceeds()
        {
            var state = CreateMatch(MatchType.Territories);
            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Playing));
        }

        [TestCase(MatchType.Deathmatch)]
        [TestCase(MatchType.KingOfTheHill)]
        [TestCase(MatchType.ArmsRace)]
        [TestCase(MatchType.Roulette)]
        public void GameMode_CanTickWithoutCrash(MatchType matchType)
        {
            var state = CreateMatch(matchType);

            // Tick for 10 simulated seconds without crashing
            for (int i = 0; i < 600; i++)
                GameSimulation.Tick(state, Dt);

            Assert.Pass($"{matchType} ticked 600 frames without error");
        }

        private static int RunUntilEnd(GameState state)
        {
            int ticks = 0;
            while (state.Phase != MatchPhase.Ended && ticks < MaxTicks)
            {
                GameSimulation.Tick(state, Dt);
                ticks++;
            }
            return ticks;
        }
    }
}
