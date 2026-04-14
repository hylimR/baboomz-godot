using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class CtfTests
    {
        static GameConfig CtfConfig()
        {
            return new GameConfig
            {
                MatchType = MatchType.CaptureTheFlag,
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
                SuddenDeathTime = 0f,
                CtfCapturesToWin = 3,
                CtfFlagDropTime = 10f,
                CtfFlagPickupRadius = 2f,
                CtfCarrierSpeedMult = 0.7f
            };
        }

        [Test]
        public void CreateMatch_Ctf_InitializesFlags()
        {
            var state = GameSimulation.CreateMatch(CtfConfig(), 42);

            Assert.IsNotNull(state.Ctf.Flags);
            Assert.AreEqual(2, state.Ctf.Flags.Length);
            Assert.AreEqual(0, state.Ctf.Flags[0].TeamIndex);
            Assert.AreEqual(1, state.Ctf.Flags[1].TeamIndex);
            Assert.IsTrue(state.Ctf.Flags[0].IsHome);
            Assert.IsTrue(state.Ctf.Flags[1].IsHome);
            Assert.AreEqual(-1, state.Ctf.Flags[0].CarrierIndex);
            Assert.AreEqual(-1, state.Ctf.Flags[1].CarrierIndex);
        }

        [Test]
        public void CreateMatch_Ctf_InitializesCaptures()
        {
            var state = GameSimulation.CreateMatch(CtfConfig(), 42);

            Assert.IsNotNull(state.Ctf.Captures);
            Assert.AreEqual(2, state.Ctf.Captures.Length);
            Assert.AreEqual(0, state.Ctf.Captures[0]);
            Assert.AreEqual(0, state.Ctf.Captures[1]);
        }

        [Test]
        public void CreateMatch_Ctf_FlagsNearSpawns()
        {
            var config = CtfConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Flag 0 should be near player 1 spawn (left side)
            Assert.Less(state.Ctf.Flags[0].HomePosition.x, 0f,
                "Team 0 flag should be on left side");
            // Flag 1 should be near player 2 spawn (right side)
            Assert.Greater(state.Ctf.Flags[1].HomePosition.x, 0f,
                "Team 1 flag should be on right side");
        }

        [Test]
        public void CreateMatch_Deathmatch_NoCtfInit()
        {
            var config = CtfConfig();
            config.MatchType = MatchType.Deathmatch;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.IsNull(state.Ctf.Flags, "Deathmatch should not init CTF flags");
        }

        [Test]
        public void Ctf_PlayerPicksUpEnemyFlag()
        {
            var state = GameSimulation.CreateMatch(CtfConfig(), 42);

            // Move P1 to team 1's flag (enemy flag)
            state.Players[0].Position = state.Ctf.Flags[1].HomePosition;
            state.Players[0].IsGrounded = true;
            state.Players[1].Position = new Vec2(-20f, 0f); // far away

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Ctf.Flags[1].CarrierIndex,
                "P1 should be carrying team 1's flag");
            Assert.IsFalse(state.Ctf.Flags[1].IsHome);
        }

        [Test]
        public void Ctf_PlayerCannotPickUpOwnFlag()
        {
            var state = GameSimulation.CreateMatch(CtfConfig(), 42);

            // Move P1 to team 0's flag (own flag)
            state.Players[0].Position = state.Ctf.Flags[0].HomePosition;
            state.Players[0].IsGrounded = true;
            state.Players[1].Position = new Vec2(20f, 0f); // far away

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(-1, state.Ctf.Flags[0].CarrierIndex,
                "P1 should not pick up own flag");
            Assert.IsTrue(state.Ctf.Flags[0].IsHome);
        }

        [Test]
        public void Ctf_FlagFollowsCarrier()
        {
            var state = GameSimulation.CreateMatch(CtfConfig(), 42);

            // P1 picks up enemy flag
            state.Players[0].Position = state.Ctf.Flags[1].HomePosition;
            state.Players[0].IsGrounded = true;
            state.Players[1].Position = new Vec2(-20f, 0f);
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Ctf.Flags[1].CarrierIndex);

            // Move P1 somewhere else
            var newPos = new Vec2(0f, 5f);
            state.Players[0].Position = newPos;
            GameSimulation.Tick(state, 0.016f);

            // Flag should follow carrier
            Assert.AreEqual(newPos.x, state.Ctf.Flags[1].Position.x, 0.01f);
        }

        [Test]
        public void Ctf_CarrierDeath_DropsFlag()
        {
            var state = GameSimulation.CreateMatch(CtfConfig(), 42);

            // P1 picks up enemy flag
            state.Players[0].Position = state.Ctf.Flags[1].HomePosition;
            state.Players[0].IsGrounded = true;
            state.Players[1].Position = new Vec2(-20f, 0f);
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Ctf.Flags[1].CarrierIndex);

            // Kill P1
            Vec2 deathPos = state.Players[0].Position;
            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;
            GameSimulation.DropCtfFlag(state, 0);

            Assert.AreEqual(-1, state.Ctf.Flags[1].CarrierIndex,
                "Flag should be dropped");
            Assert.IsFalse(state.Ctf.Flags[1].IsHome,
                "Flag should not be home after drop");
            Assert.Greater(state.Ctf.Flags[1].DropTimer, 0f,
                "Drop timer should be set");
        }

        [Test]
        public void Ctf_DroppedFlag_ReturnsAfterTimeout()
        {
            var state = GameSimulation.CreateMatch(CtfConfig(), 42);

            // P1 picks up enemy flag
            state.Players[0].Position = state.Ctf.Flags[1].HomePosition;
            state.Players[0].IsGrounded = true;
            state.Players[1].Position = new Vec2(-20f, 0f);
            GameSimulation.Tick(state, 0.016f);

            // Kill P1 to drop flag
            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;
            GameSimulation.DropCtfFlag(state, 0);

            // Tick past the drop timer
            float dropTime = state.Config.CtfFlagDropTime;
            for (int i = 0; i < (int)(dropTime / 0.1f) + 5; i++)
                GameSimulation.Tick(state, 0.1f);

            Assert.IsTrue(state.Ctf.Flags[1].IsHome,
                "Flag should return home after drop timer expires");
            Assert.AreEqual(state.Ctf.Flags[1].HomePosition.x, state.Ctf.Flags[1].Position.x, 0.01f);
        }

        [Test]
        public void Ctf_ReturnOwnDroppedFlag()
        {
            var state = GameSimulation.CreateMatch(CtfConfig(), 42);

            // P2 picks up team 0's flag
            state.Players[1].Position = state.Ctf.Flags[0].HomePosition;
            state.Players[1].IsGrounded = true;
            state.Players[0].Position = new Vec2(-20f, 0f);
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(1, state.Ctf.Flags[0].CarrierIndex);

            // Kill P2 to drop flag at a non-home position
            Vec2 midPos = new Vec2(0f, state.Players[1].Position.y);
            state.Players[1].Position = midPos;
            state.Players[1].Health = 0f;
            state.Players[1].IsDead = true;
            GameSimulation.DropCtfFlag(state, 1);

            // P1 walks to the dropped flag (which is their own)
            state.Players[0].Position = state.Ctf.Flags[0].Position;
            state.Players[0].IsGrounded = true;
            GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.Ctf.Flags[0].IsHome,
                "Player touching own dropped flag should return it");
        }

        [Test]
        public void Ctf_Capture_IncreasesScore()
        {
            var state = GameSimulation.CreateMatch(CtfConfig(), 42);

            // P1 picks up enemy flag
            state.Players[0].Position = state.Ctf.Flags[1].HomePosition;
            state.Players[0].IsGrounded = true;
            state.Players[1].Position = new Vec2(-20f, 0f);
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Ctf.Flags[1].CarrierIndex);

            // P1 returns to own flag (home, at home position)
            state.Players[0].Position = state.Ctf.Flags[0].HomePosition;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(1, state.Ctf.Captures[0],
                "Team 0 should have 1 capture");
            Assert.IsTrue(state.Ctf.Flags[1].IsHome,
                "Captured flag should return home");
        }

        [Test]
        public void Ctf_CaptureRequiresOwnFlagAtHome()
        {
            var state = GameSimulation.CreateMatch(CtfConfig(), 42);

            // P2 picks up team 0's flag first (so team 0's flag is NOT home)
            state.Players[1].Position = state.Ctf.Flags[0].HomePosition;
            state.Players[1].IsGrounded = true;
            state.Players[0].Position = new Vec2(-20f, 0f);
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(1, state.Ctf.Flags[0].CarrierIndex,
                "P2 should carry team 0's flag");

            // Now P1 picks up team 1's flag
            state.Players[0].Position = state.Ctf.Flags[1].HomePosition;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Ctf.Flags[1].CarrierIndex,
                "P1 should carry team 1's flag");

            // P1 goes to own flag home position — but own flag is NOT there
            state.Players[0].Position = state.Ctf.Flags[0].HomePosition;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Ctf.Captures[0],
                "Should NOT capture if own flag is not at home");
        }

        [Test]
        public void Ctf_ThreeCaptures_WinsMatch()
        {
            var config = CtfConfig();
            config.CtfCapturesToWin = 3;
            var state = GameSimulation.CreateMatch(config, 42);

            // Manually set captures to 2
            state.Ctf.Captures[0] = 2;

            // P1 picks up enemy flag
            state.Players[0].Position = state.Ctf.Flags[1].HomePosition;
            state.Players[0].IsGrounded = true;
            state.Players[1].Position = new Vec2(-20f, 0f);
            GameSimulation.Tick(state, 0.016f);

            // P1 captures (returns to own flag)
            state.Players[0].Position = state.Ctf.Flags[0].HomePosition;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(MatchPhase.Ended, state.Phase,
                "Match should end at 3 captures");
            Assert.AreEqual(0, state.WinnerIndex,
                "P1 (team 0) should win");
        }

        [Test]
        public void Ctf_CarrierSpeedPenalty()
        {
            var config = CtfConfig();
            config.CtfCarrierSpeedMult = 0.7f;
            var state = GameSimulation.CreateMatch(config, 42);

            float normalSpeed = state.Players[0].MoveSpeed;

            // P1 picks up enemy flag
            state.Players[0].Position = state.Ctf.Flags[1].HomePosition;
            state.Players[0].IsGrounded = true;
            state.Players[1].Position = new Vec2(-20f, 0f);
            GameSimulation.Tick(state, 0.016f);

            float expected = config.DefaultMoveSpeed * 0.7f;
            Assert.AreEqual(expected, state.Players[0].MoveSpeed, 0.1f,
                "Carrier should have reduced move speed");
        }

        [Test]
        public void Ctf_FlagEvents_EmittedOnPickup()
        {
            var state = GameSimulation.CreateMatch(CtfConfig(), 42);

            state.Players[0].Position = state.Ctf.Flags[1].HomePosition;
            state.Players[0].IsGrounded = true;
            state.Players[1].Position = new Vec2(-20f, 0f);
            GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.FlagEvents.Count > 0, "Should emit flag event");
            Assert.AreEqual(FlagEventType.Pickup, state.FlagEvents[0].Type);
            Assert.AreEqual(0, state.FlagEvents[0].PlayerIndex);
        }

        [Test]
        public void Ctf_DeadPlayerCannotPickUpFlag()
        {
            var state = GameSimulation.CreateMatch(CtfConfig(), 42);

            state.Players[0].Position = state.Ctf.Flags[1].HomePosition;
            state.Players[0].IsGrounded = true;
            state.Players[0].IsDead = true;
            state.Players[1].Position = new Vec2(-20f, 0f);

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(-1, state.Ctf.Flags[1].CarrierIndex,
                "Dead player should not pick up flag");
        }

        [Test]
        public void Ctf_DropCtfFlag_NotCtfMode_NoOp()
        {
            var config = CtfConfig();
            config.MatchType = MatchType.Deathmatch;
            var state = GameSimulation.CreateMatch(config, 42);

            // Should not throw
            GameSimulation.DropCtfFlag(state, 0);
        }

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
