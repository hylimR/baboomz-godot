using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class PayloadTests
    {
        [Test]
        public void Payload_DeadPlayer_RespawnsAfterDelay()
        {
            var config = PayloadConfig();
            config.PayloadRespawnDelay = 0.5f;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;

            for (int i = 0; i < 20; i++)
                GameSimulation.Tick(state, 0.016f);
            Assert.IsTrue(state.Players[1].IsDead, "Should still be dead before respawn delay");

            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);
            Assert.IsFalse(state.Players[1].IsDead, "Should respawn after delay");
            Assert.AreEqual(config.DefaultMaxHealth, state.Players[1].Health, 0.01f);
        }

        [Test]
        public void Payload_Respawn_RestoresFullHealth()
        {
            var config = PayloadConfig();
            config.PayloadRespawnDelay = 0.1f;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;

            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(state.Players[0].IsDead);
            Assert.AreEqual(config.DefaultMaxHealth, state.Players[0].Health, 0.01f);
            Assert.AreEqual(config.DefaultMaxEnergy, state.Players[0].Energy, 0.01f);
        }

        [Test]
        public void Payload_LivesLimit_PreventsRespawn()
        {
            var config = PayloadConfig();
            config.PayloadRespawnDelay = 0.1f;
            config.PayloadLivesPerPlayer = 1;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;

            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);
            Assert.IsFalse(state.Players[1].IsDead, "Should respawn with 1 life remaining");

            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;

            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);
            Assert.IsTrue(state.Players[1].IsDead, "Should not respawn with 0 lives");
        }

        [Test]
        public void Payload_UnlimitedLives_AlwaysRespawns()
        {
            var config = PayloadConfig();
            config.PayloadRespawnDelay = 0.1f;
            config.PayloadLivesPerPlayer = -1;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            for (int death = 0; death < 3; death++)
            {
                state.Players[0].Health = 0f;
                state.Players[0].IsDead = true;

                for (int i = 0; i < 30; i++)
                    GameSimulation.Tick(state, 0.016f);

                Assert.IsFalse(state.Players[0].IsDead,
                    $"Should respawn on death #{death + 1} with unlimited lives");
            }
        }
    }
}
