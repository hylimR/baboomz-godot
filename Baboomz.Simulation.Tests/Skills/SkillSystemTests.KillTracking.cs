using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class SkillSystemTests
    {
        // --- Earthquake / HookShot kill stat tracking (#191) ---

        [Test]
        public void Earthquake_Kill_TracksTotalKills()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                FindSkill(state.Config, SkillType.Earthquake));

            state.Players[1].Health = 1f;
            state.Players[1].IsGrounded = true;
            state.Players[0].TotalKills = 0;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsTrue(state.Players[1].IsDead);
            Assert.AreEqual(1, state.Players[0].TotalKills,
                "Earthquake kill should increment TotalKills");
        }

        [Test]
        public void Earthquake_Kill_TracksKillsInWindow()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                FindSkill(state.Config, SkillType.Earthquake));

            state.Players[1].Health = 1f;
            state.Players[1].IsGrounded = true;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsTrue(state.Players[1].IsDead);
            Assert.AreEqual(1, state.Players[0].KillsInWindow,
                "Earthquake kill should track KillsInWindow for combo system");
        }

        [Test]
        public void Earthquake_Kill_TracksCloseRangeKills()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                FindSkill(state.Config, SkillType.Earthquake));

            // Place target close to caster (within 5 units)
            state.Players[1].Position = state.Players[0].Position + new Vec2(3f, 0f);
            state.Players[1].Health = 1f;
            state.Players[1].IsGrounded = true;
            state.Players[0].CloseRangeKills = 0;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsTrue(state.Players[1].IsDead);
            Assert.AreEqual(1, state.Players[0].CloseRangeKills,
                "Close-range Earthquake kill should increment CloseRangeKills");
        }

        [Test]
        public void HookShot_Kill_TracksTotalKills()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                FindSkill(state.Config, SkillType.HookShot));

            // Place target in range and at lethal health
            state.Players[1].Position = state.Players[0].Position + new Vec2(5f, 0f);
            state.Players[1].Health = 1f;
            state.Players[0].FacingDirection = 1;
            state.Players[0].AimAngle = 0f;
            state.Players[0].TotalKills = 0;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsTrue(state.Players[1].IsDead);
            Assert.AreEqual(1, state.Players[0].TotalKills,
                "HookShot kill should increment TotalKills");
        }

        [Test]
        public void HookShot_Kill_TracksCombo()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                FindSkill(state.Config, SkillType.HookShot));

            // First kill via HookShot to set up KillsInWindow
            state.Players[1].Position = state.Players[0].Position + new Vec2(5f, 0f);
            state.Players[1].Health = 1f;
            state.Players[0].FacingDirection = 1;
            state.Players[0].AimAngle = 0f;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsTrue(state.Players[1].IsDead);
            Assert.AreEqual(1, state.Players[0].KillsInWindow,
                "HookShot kill should track KillsInWindow for combo system");
        }

        // --- Rope swing double-gravity regression (#359) ---

        [Test]
        public void RopeSwing_DoesNotApplyGravityTwice()
        {
            // Manually construct a player mid-swing (GrapplingHook active, airborne)
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;
            var state = GameSimulation.CreateMatch(config, 42);

            // Disable AI so it doesn't fire projectiles that add knockback to player 0
            state.Players[1].IsAI = false;
            state.Players[1].IsDead = true; // Kill AI to prevent any interference

            ref PlayerState p = ref state.Players[0];
            SetSkillSlot(ref p.SkillSlots[0], FindSkill(state.Config, SkillType.GrapplingHook));

            // Move player high above terrain to avoid collision effects
            p.Position = new Vec2(0f, 15f);
            p.IsGrounded = false;
            p.Velocity = new Vec2(0f, 0f);

            // Manually activate the skill slot (bypass terrain hit requirement)
            p.SkillSlots[0].IsActive = true;
            p.SkillSlots[0].DurationRemaining = 2f;
            // Anchor point directly above — at angle 0, pendulum gravity is sin(0) = 0
            p.SkillTargetPosition = new Vec2(p.Position.x, p.Position.y + 8f);
            p.RopeLength = 8f;

            float dt = 0.016f;
            float gravity = state.Config.Gravity;

            // Tick once — SkillSystem.Update runs UpdateRopeSwing, then UpdatePlayer runs.
            // If double-gravity bug is present, velocity.y will decrease by 2 × gravity × dt.
            // With the fix, gravity is NOT applied in UpdatePlayer when swinging.
            GameSimulation.Tick(state, dt);

            // UpdateRopeSwing sets velocity from pendulum tangential motion.
            // At angle=0 (anchor directly above) the gravity term is sin(0)=0,
            // so velocity after one tick should remain near-zero in y (not gravity × dt downward).
            float maxExpectedGravityContribution = gravity * dt * 1.5f; // 50% margin
            Assert.Less(MathF.Abs(state.Players[0].Velocity.y), maxExpectedGravityContribution,
                "While rope-swinging, UpdatePlayer must not add an extra gravity term to velocity");
        }
    }
}
