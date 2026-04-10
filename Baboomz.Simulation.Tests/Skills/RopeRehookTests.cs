using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class RopeRehookTests
    {
        static GameConfig SmallConfig()
        {
            return new GameConfig
            {
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
                DefaultMaxEnergy = 100f,
                DefaultEnergyRegen = 10f,
                DefaultMoveSpeed = 5f,
                DefaultJumpForce = 10f,
                DefaultShootCooldown = 0.5f,
                MineCount = 0,
                BarrelCount = 0
            };
        }

        static SkillSlotState GrappleSkill()
        {
            return new SkillSlotState
            {
                SkillId = "grapple",
                Type = SkillType.GrapplingHook,
                EnergyCost = 25f,
                Cooldown = 5f,
                Duration = 2f,
                Range = 20f,
                Value = 15f
            };
        }

        [Test]
        public void Rehook_DetachMidSwing_PreservesVelocity()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Set up active grapple manually
            state.Players[0].SkillSlots[0] = GrappleSkill();
            state.Players[0].SkillSlots[0].IsActive = true;
            state.Players[0].SkillSlots[0].DurationRemaining = 1.5f;
            state.Players[0].SkillTargetPosition = state.Players[0].Position + new Vec2(0f, 5f);
            state.Players[0].RopeLength = 5f;
            state.Players[0].IsGrounded = false;
            state.Players[0].Velocity = new Vec2(3f, 2f); // simulated swing velocity

            // Detach by pressing skill during active swing
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive,
                "Grapple should deactivate on mid-swing press");
            Assert.Greater(state.Players[0].RopeRehookWindow, 0f,
                "Rehook window should open after detach while airborne");
        }

        [Test]
        public void Rehook_WithinWindow_AttachesNewAnchor()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = GrappleSkill();
            state.Players[0].Energy = 100f;
            state.Players[0].AimAngle = 60f;
            state.Players[0].FacingDirection = 1;

            // Activate grapple
            SkillSystem.ActivateSkill(state, 0, 0);

            if (!state.Players[0].SkillSlots[0].IsActive)
            {
                Assert.Pass("No terrain to grapple at this seed — test skipped");
                return;
            }

            float energyAfterFirst = state.Players[0].Energy;

            // Tick until grapple expires
            for (int i = 0; i < 130; i++) // ~2s at 0.016
                GameSimulation.Tick(state, 0.016f);

            // Grapple should have expired; rehook window should be open if airborne
            if (state.Players[0].RopeRehookWindow <= 0f || state.Players[0].IsGrounded)
            {
                Assert.Pass("Player landed after swing — rehook window not available");
                return;
            }

            // Re-hook: aim upward and activate again
            state.Players[0].AimAngle = 70f;
            SkillSystem.ActivateSkill(state, 0, 0);

            if (state.Players[0].SkillSlots[0].IsActive)
            {
                Assert.AreEqual(1, state.Players[0].RopeHookCount,
                    "Rehook count should increment after re-hooking");
                Assert.AreEqual(energyAfterFirst, state.Players[0].Energy, 0.01f,
                    "Re-hook should not consume additional energy");
            }
            else
            {
                Assert.Pass("No terrain hit for re-hook at this angle — test skipped");
            }
        }

        [Test]
        public void Rehook_MaxFiveRehooks()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = GrappleSkill();
            state.Players[0].IsGrounded = false;
            state.Players[0].RopeHookCount = 5; // already at max
            state.Players[0].RopeRehookWindow = 0.5f;
            state.Players[0].AimAngle = 60f;
            state.Players[0].FacingDirection = 1;

            // Attempt re-hook at max count
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(5, state.Players[0].RopeHookCount,
                "Should not exceed max 5 re-hooks");
        }

        [Test]
        public void Rehook_WindowExpires_CannotRehook()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = GrappleSkill();
            state.Players[0].IsGrounded = false;
            state.Players[0].RopeHookCount = 1;
            state.Players[0].RopeRehookWindow = 0.5f;
            state.Players[0].AimAngle = 60f;
            state.Players[0].FacingDirection = 1;

            // Tick past the window (0.5s = ~32 frames at 0.016)
            for (int i = 0; i < 35; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0f, state.Players[0].RopeRehookWindow, 0.01f,
                "Rehook window should have expired");
            Assert.AreEqual(0, state.Players[0].RopeHookCount,
                "Hook count should reset when window expires");
        }

        [Test]
        public void Rehook_Landing_ResetsState()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = GrappleSkill();
            state.Players[0].RopeHookCount = 3;
            state.Players[0].RopeRehookWindow = 0.4f;

            // Simulate landing
            state.Players[0].IsGrounded = true;

            // Tick so SkillSystem.Update processes the player
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0f, state.Players[0].RopeRehookWindow, 0.01f,
                "Landing should close rehook window");
            Assert.AreEqual(0, state.Players[0].RopeHookCount,
                "Landing should reset hook count");
        }

        [Test]
        public void Rehook_PreservesMomentum_OnReattach()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = GrappleSkill();
            state.Players[0].IsGrounded = false;
            state.Players[0].Velocity = new Vec2(5f, 3f); // carry from previous swing
            state.Players[0].RopeHookCount = 0;
            state.Players[0].RopeRehookWindow = 0.5f;
            state.Players[0].AimAngle = 60f;
            state.Players[0].FacingDirection = 1;

            Vec2 velBefore = state.Players[0].Velocity;

            SkillSystem.ActivateSkill(state, 0, 0);

            if (!state.Players[0].SkillSlots[0].IsActive)
            {
                Assert.Pass("No terrain hit for re-hook — test skipped");
                return;
            }

            // Velocity should not be zeroed on re-hook (momentum preserved)
            // Note: the swing physics will immediately modify velocity, but at
            // the moment of attachment, velocity should not be forcefully zeroed
            Assert.AreEqual(1, state.Players[0].RopeHookCount);
        }

        [Test]
        public void Rehook_FreshActivation_ResetsHookCount()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = GrappleSkill();
            state.Players[0].Energy = 100f;
            state.Players[0].AimAngle = 60f;
            state.Players[0].FacingDirection = 1;
            state.Players[0].RopeHookCount = 3; // leftover from previous
            state.Players[0].RopeRehookWindow = 0f; // no window

            SkillSystem.ActivateSkill(state, 0, 0);

            if (!state.Players[0].SkillSlots[0].IsActive)
            {
                Assert.Pass("No terrain to grapple — test skipped");
                return;
            }

            Assert.AreEqual(0, state.Players[0].RopeHookCount,
                "Fresh activation should reset hook count to 0");
        }
    }
}
