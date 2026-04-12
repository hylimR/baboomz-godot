using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Emote system tests ---

        [Test]
        public void Emote_TriggerSetsActiveEmote()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            GameSimulation.TriggerEmote(state, 0, EmoteType.Taunt);

            Assert.AreEqual(EmoteType.Taunt, state.Players[0].ActiveEmote);
            Assert.Greater(state.Players[0].EmoteTimer, 0f);
            Assert.AreEqual(1, state.EmoteEvents.Count);
            Assert.AreEqual(EmoteType.Taunt, state.EmoteEvents[0].Emote);
        }

        [Test]
        public void Emote_ExpiresAfterDuration()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].ActiveEmote = EmoteType.Laugh;
            state.Players[0].EmoteTimer = 0.5f; // short timer

            // Tick past emote duration
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(EmoteType.None, state.Players[0].ActiveEmote,
                "Emote should clear after timer expires");
        }

        [Test]
        public void Emote_DeadPlayerCannotEmote()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].IsDead = true;
            GameSimulation.TriggerEmote(state, 0, EmoteType.ThumbsUp);

            Assert.AreEqual(EmoteType.None, state.Players[0].ActiveEmote,
                "Dead player should not be able to emote");
        }

        [Test]
        public void Emote_CannotInterruptActiveEmote()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            GameSimulation.TriggerEmote(state, 0, EmoteType.Laugh);
            Assert.AreEqual(EmoteType.Laugh, state.Players[0].ActiveEmote);

            // Try to trigger another emote while first is active
            state.EmoteEvents.Clear();
            GameSimulation.TriggerEmote(state, 0, EmoteType.Taunt);
            Assert.AreEqual(EmoteType.Laugh, state.Players[0].ActiveEmote,
                "Active emote should not be interrupted");
            Assert.AreEqual(0, state.EmoteEvents.Count,
                "No emote event should be emitted when interrupted");
        }

        [Test]
        public void Emote_BlockedWhileInvisible_Regression139()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42);

            // Make player invisible (decoy active)
            state.Players[0].IsInvisible = true;
            state.Players[0].DecoyTimer = 5f;

            // Try to emote via input while invisible
            state.Input.EmotePressed = 1; // Taunt
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(EmoteType.None, state.Players[0].ActiveEmote,
                "Invisible player should not be able to emote via input");
            Assert.AreEqual(0, state.EmoteEvents.Count,
                "No emote event should be emitted for invisible player");
        }

    }
}
