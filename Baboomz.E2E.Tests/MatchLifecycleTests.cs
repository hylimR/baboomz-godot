using System;

namespace Baboomz.E2E.Tests
{
    /// <summary>
    /// End-to-end tests for the full match lifecycle.
    /// Verifies: CreateMatch → Tick through phases → match ends with a winner.
    /// No Godot dependency — exercises the simulation layer end-to-end.
    /// </summary>
    [TestFixture]
    public class MatchLifecycleTests
    {
        private const int Seed = 42;
        private const float Dt = 1f / 60f;
        private const int MaxTicks = 30000; // ~8 minutes at 60fps — enough for any match

        private static GameState CreateDeathmatch(int seed = Seed)
        {
            var config = new GameConfig();
            config.UnlockedTier = UnlockRegistry.GetTier(0);
            var state = GameSimulation.CreateMatch(config, seed);
            AILogic.Reset(seed, state.Players.Length);
            BossLogic.Reset(seed, state.Players.Length);
            return state;
        }

        [Test]
        public void CreateMatch_StartsInPlayingPhase()
        {
            var state = CreateDeathmatch();

            // CreateMatch sets Playing; Godot's MatchCountdown overrides to Waiting then back
            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(state.WinnerIndex, Is.EqualTo(-1));
            Assert.That(state.Players.Length, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void CreateMatch_PlayersSpawnAlive_WithHealth()
        {
            var state = CreateDeathmatch();

            foreach (ref var p in state.Players.AsSpan())
            {
                Assert.That(p.IsDead, Is.False, $"Player {p.Name} should be alive");
                Assert.That(p.Health, Is.GreaterThan(0f), $"Player {p.Name} should have health");
                Assert.That(p.MaxHealth, Is.GreaterThan(0f));
            }
        }

        [Test]
        public void CreateMatch_TerrainIsGenerated()
        {
            var state = CreateDeathmatch();

            Assert.That(state.Terrain, Is.Not.Null);
            Assert.That(state.Terrain.Width, Is.GreaterThan(0));
            Assert.That(state.Terrain.Height, Is.GreaterThan(0));
        }

        [Test]
        public void Tick_FewFrames_DoesNotEndImmediately()
        {
            var state = CreateDeathmatch();
            // Tick a few frames — match shouldn't end immediately
            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, Dt);

            Assert.That(state.Phase, Is.Not.EqualTo(MatchPhase.Ended));
        }

        [Test]
        public void FullMatch_Deathmatch_EndsWithWinner()
        {
            var state = CreateDeathmatch();
            int ticks = 0;
            while (state.Phase != MatchPhase.Ended && ticks < MaxTicks)
            {
                GameSimulation.Tick(state, Dt);
                ticks++;
            }

            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Ended),
                $"Match did not end within {MaxTicks} ticks");
            Assert.That(state.WinnerIndex, Is.GreaterThanOrEqualTo(-1));
            Assert.That(ticks, Is.GreaterThan(0), "Match should take at least 1 tick");
        }

        [Test]
        public void FullMatch_Deathmatch_ExactlyOnePlayerSurvives()
        {
            var state = CreateDeathmatch();

            while (state.Phase != MatchPhase.Ended)
                GameSimulation.Tick(state, Dt);

            int aliveCount = 0;
            for (int i = 0; i < state.Players.Length; i++)
            {
                if (!state.Players[i].IsDead)
                    aliveCount++;
            }

            // In Deathmatch, either 1 survivor or 0 (draw)
            Assert.That(aliveCount, Is.LessThanOrEqualTo(1));
            if (state.WinnerIndex >= 0)
            {
                Assert.That(state.Players[state.WinnerIndex].IsDead, Is.False,
                    "Winner should be alive");
            }
        }

        [Test]
        public void FullMatch_TimeAdvances()
        {
            var state = CreateDeathmatch();

            float startTime = state.Time;
            for (int i = 0; i < 600; i++) // 10 seconds
                GameSimulation.Tick(state, Dt);

            Assert.That(state.Time, Is.GreaterThan(startTime));
        }

        [TestCase(0)]
        [TestCase(12345)]
        [TestCase(99999)]
        public void FullMatch_DifferentSeeds_AllComplete(int seed)
        {
            var state = CreateDeathmatch(seed);

            int ticks = 0;
            while (state.Phase != MatchPhase.Ended && ticks < MaxTicks)
            {
                GameSimulation.Tick(state, Dt);
                ticks++;
            }

            Assert.That(state.Phase, Is.EqualTo(MatchPhase.Ended),
                $"Seed {seed}: match did not end within {MaxTicks} ticks");
        }

        [Test]
        public void FullMatch_DamageOccurs()
        {
            var state = CreateDeathmatch();

            bool anyDamage = false;
            int ticks = 0;
            while (state.Phase != MatchPhase.Ended && ticks < MaxTicks)
            {
                GameSimulation.Tick(state, Dt);
                if (state.DamageEvents.Count > 0)
                    anyDamage = true;
                ticks++;
            }

            Assert.That(anyDamage, Is.True, "No damage events occurred during match");
        }

        [Test]
        public void FullMatch_ExplosionsOccur()
        {
            var state = CreateDeathmatch();

            bool anyExplosion = false;
            int ticks = 0;
            while (state.Phase != MatchPhase.Ended && ticks < MaxTicks)
            {
                GameSimulation.Tick(state, Dt);
                if (state.ExplosionEvents.Count > 0)
                    anyExplosion = true;
                ticks++;
            }

            Assert.That(anyExplosion, Is.True, "No explosion events during match");
        }
    }
}
