using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    // Regression tests for specific CTF bug fixes (issues #83, #108).
    // Kept in a separate partial file so CtfTests.cs stays under the 400-line
    // SOLID file-size budget (#119).
    public partial class CtfTests
    {
        [Test]
        public void Ctf_CarrierSpeedPenalty_PreservesWarCry_Issue83()
        {
            // Issue #83: Carrier speed should preserve WarCry buff.
            var config = CtfConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42, state.Players.Length);

            float warCryBuff = 1.5f;

            // Simulate WarCry active + carrying flag
            state.Players[0].WarCryTimer = 5f;
            state.Players[0].WarCrySpeedBuff = warCryBuff;

            // Make player 0 carry enemy flag
            state.Ctf.Flags[1].CarrierIndex = 0;

            // Tick to apply carrier speed penalty
            GameSimulation.Tick(state, 0.016f);

            // Speed should be base * warCry * carrier mult
            float expected = config.DefaultMoveSpeed * warCryBuff * config.CtfCarrierSpeedMult;
            Assert.AreEqual(expected, state.Players[0].MoveSpeed, 0.5f,
                "Carrier speed should be base * WarCry * carrier mult (issue #83)");
        }

        [Test]
        public void Ctf_CarrierSpeed_DoesNotDecayOverTicks_Issue108()
        {
            // Issue #108: carrier speed decayed exponentially because *= was applied every frame.
            var config = CtfConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42, state.Players.Length);

            // Make player 0 carry enemy flag
            state.Ctf.Flags[1].CarrierIndex = 0;
            state.Ctf.Flags[1].IsHome = false;

            float expected = config.DefaultMoveSpeed * config.CtfCarrierSpeedMult;

            // Tick 60 frames — speed must remain stable
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(expected, state.Players[0].MoveSpeed, 0.01f,
                "Carrier speed should remain stable after 60 ticks, not decay exponentially");
        }

        [Test]
        public void Ctf_NonCarrier_SpeedRestored_Issue83()
        {
            // Issue #83: After dropping flag, speed was never restored
            var config = CtfConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            AILogic.Reset(42, state.Players.Length);

            float baseSpeed = config.DefaultMoveSpeed;

            // Set player speed below base (simulating lingering penalty)
            state.Players[0].MoveSpeed = baseSpeed * 0.5f;
            state.Players[0].WarCryTimer = 0f; // no WarCry

            // Not carrying any flag
            state.Ctf.Flags[0].CarrierIndex = -1;
            state.Ctf.Flags[1].CarrierIndex = -1;

            GameSimulation.Tick(state, 0.016f);

            Assert.GreaterOrEqual(state.Players[0].MoveSpeed, baseSpeed * 0.9f,
                "Non-carrier speed should be restored to base (issue #83)");
        }
    }
}
