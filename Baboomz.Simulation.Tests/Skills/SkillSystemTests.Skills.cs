using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class SkillSystemTests
    {
        // --- Teleport tests ---

        [Test]
        public void Teleport_MovesPlayer_InAimDirection()
        {
            var state = CreateState();
            // Ensure slot 0 is teleport
            Assert.AreEqual(SkillType.Teleport, state.Players[0].SkillSlots[0].Type);

            Vec2 posBefore = state.Players[0].Position;
            state.Players[0].AimAngle = 0f;
            state.Players[0].FacingDirection = 1;

            SkillSystem.ActivateSkill(state, 0, 0);

            // Player should have moved to the right
            Assert.Greater(state.Players[0].Position.x, posBefore.x);
        }

        [Test]
        public void Teleport_MaxRange_Respected()
        {
            var state = CreateState();
            Vec2 posBefore = state.Players[0].Position;
            state.Players[0].AimAngle = 0f;
            state.Players[0].FacingDirection = 1;
            float range = state.Players[0].SkillSlots[0].Range;

            SkillSystem.ActivateSkill(state, 0, 0);

            float moved = state.Players[0].Position.x - posBefore.x;
            Assert.LessOrEqual(moved, range + 1f); // small tolerance for terrain resolution
        }

        [Test]
        public void Teleport_IsInstant_NoDuration()
        {
            var state = CreateState();

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive);
        }

        // --- Shield tests ---

        [Test]
        public void Shield_IncreasesArmorMultiplier()
        {
            var state = CreateState();
            // Set slot 1 to shield
            SetSkillSlot(ref state.Players[0].SkillSlots[1],
                state.Config.Skills[2]); // shield is index 2

            SkillSystem.ActivateSkill(state, 0, 1);

            Assert.AreEqual(3f, state.Players[0].ArmorMultiplier, 0.01f);
        }

        [Test]
        public void Shield_ExpiresAfterDuration()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[1],
                state.Config.Skills[2]);

            SkillSystem.ActivateSkill(state, 0, 1);
            Assert.IsTrue(state.Players[0].SkillSlots[1].IsActive);

            // Tick past shield duration (3s)
            SkillSystem.Update(state, 4f);

            Assert.IsFalse(state.Players[0].SkillSlots[1].IsActive);
        }

        [Test]
        public void Shield_ArmorReverts_OnExpiry()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[1],
                state.Config.Skills[2]);

            SkillSystem.ActivateSkill(state, 0, 1);

            // Tick past shield duration
            SkillSystem.Update(state, 4f);

            Assert.AreEqual(state.Config.DefaultArmorMultiplier,
                state.Players[0].ArmorMultiplier, 0.01f);
        }

        [Test]
        public void Shield_RestoresNonDefaultArmor_OnExpiry()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[1],
                state.Config.Skills[2]); // shield

            // Simulate boss/campaign bonus armor
            state.Players[0].ArmorMultiplier = 2f;

            SkillSystem.ActivateSkill(state, 0, 1);
            Assert.AreEqual(3f, state.Players[0].ArmorMultiplier, 0.01f,
                "Shield should set armor to skill value");

            // Tick past shield duration
            SkillSystem.Update(state, 4f);

            Assert.AreEqual(2f, state.Players[0].ArmorMultiplier, 0.01f,
                "Shield expiry must restore pre-shield armor, not default");
        }

        // --- Dash tests ---

        [Test]
        public void Dash_AppliesHorizontalVelocity()
        {
            var state = CreateState();
            // Default slot 1 is dash (index 3)
            Assert.AreEqual(SkillType.Dash, state.Players[0].SkillSlots[1].Type);

            state.Players[0].FacingDirection = 1;

            SkillSystem.ActivateSkill(state, 0, 1);

            Assert.Greater(state.Players[0].Velocity.x, 0f);
        }

        [Test]
        public void Dash_RespectsFacingDirection()
        {
            var state = CreateState();
            state.Players[0].FacingDirection = -1;

            SkillSystem.ActivateSkill(state, 0, 1);

            Assert.Less(state.Players[0].Velocity.x, 0f);
        }

        // --- Heal tests ---

        [Test]
        public void Heal_RestoresHP()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[4]); // heal is index 4
            state.Players[0].Health = 50f;

            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.Update(state, 1f);

            Assert.Greater(state.Players[0].Health, 50f);
        }

        [Test]
        public void Heal_CapsAtMaxHealth()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[4]);
            state.Players[0].Health = 95f;

            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.Update(state, 5f); // tick well past duration

            Assert.AreEqual(state.Players[0].MaxHealth,
                state.Players[0].Health, 0.01f);
        }

        // --- Jetpack tests ---

        [Test]
        public void Jetpack_AppliesUpwardForce()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[5]); // jetpack is index 5

            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.Update(state, 0.1f);

            Assert.Greater(state.Players[0].Velocity.y, 0f);
        }

        [Test]
        public void Jetpack_Expires()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[5]);

            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].SkillSlots[0].IsActive);

            SkillSystem.Update(state, 3f); // past 2s duration

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive);
        }

        // --- Grappling Hook tests ---

        [Test]
        public void GrapplingHook_NoTerrainHit_Refunds()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[1]); // grapple is index 1

            // Aim straight up where there's no terrain
            state.Players[0].AimAngle = 90f;
            float energyBefore = state.Players[0].Energy;

            SkillSystem.ActivateSkill(state, 0, 0);

            // Energy should be refunded (grapple missed)
            Assert.AreEqual(energyBefore, state.Players[0].Energy, 0.01f);
        }

        [Test]
        public void GrapplingHook_RangePreservedAfterUse()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[1]); // grapple is index 1

            float originalRange = state.Players[0].SkillSlots[0].Range;
            Assert.Greater(originalRange, 0f);

            // Aim toward terrain (angle 0 = forward) — will hit nearby terrain
            state.Players[0].AimAngle = 0f;
            state.Players[0].Energy = 100f;

            SkillSystem.ActivateSkill(state, 0, 0);

            // If grapple connected, the rope length is stored on PlayerState
            if (state.Players[0].SkillSlots[0].IsActive)
            {
                Assert.Greater(state.Players[0].RopeLength, 0f,
                    "RopeLength should be set on player state");

                // skill.Range must NOT have been mutated
                Assert.AreEqual(originalRange, state.Players[0].SkillSlots[0].Range, 0.01f,
                    "skill.Range must not be overwritten by rope length");

                // Deactivate by expiring duration
                state.Players[0].SkillSlots[0].DurationRemaining = 0f;
                SkillSystem.Update(state, 0.016f);

                // Range still preserved for next activation
                Assert.AreEqual(originalRange, state.Players[0].SkillSlots[0].Range, 0.01f,
                    "skill.Range must be preserved after grapple deactivation");
            }
        }

        // --- Fall damage regression tests (#372) ---

        [Test]
        public void Teleport_ResetsLastGroundedY_WhenAirborne()
        {
            // Regression: teleporting to an airborne position should reset LastGroundedY
            // so fall damage is measured from the teleport destination, not the pre-teleport position.
            var state = CreateState();
            ref PlayerState p = ref state.Players[0];
            Assert.AreEqual(SkillType.Teleport, p.SkillSlots[0].Type);

            // Simulate a player grounded at a high Y
            p.LastGroundedY = 20f;
            p.IsGrounded = true;
            // Aim straight right so they teleport to a lower position with no ground
            p.AimAngle = 0f;
            p.FacingDirection = 1;

            SkillSystem.ActivateSkill(state, 0, 0);

            // LastGroundedY must now equal the teleport destination Y, not 20
            Assert.AreEqual(p.Position.y, p.LastGroundedY, 0.01f,
                "Teleport must reset LastGroundedY to the new position to prevent inflated fall damage");
        }

        [Test]
        public void ShadowStep_ResetsLastGroundedY_OnRecall_WhenAirborne()
        {
            // Regression: ShadowStep recall should reset LastGroundedY to the recall destination
            // even when the destination has no ground (terrain destroyed), not the mark Y.
            var state = CreateState();
            ref PlayerState p = ref state.Players[0];
            SetSkillSlot(ref p.SkillSlots[0], FindSkill(state.Config, SkillType.ShadowStep));
            state.Players[0].Energy = 100f;

            // Mark position at a high Y
            p.LastGroundedY = 20f;
            p.IsGrounded = true;

            // Activate ShadowStep to record current position as recall point
            SkillSystem.ActivateSkill(state, 0, 0);
            float markedY = p.SkillTargetPosition.y;

            // Move player high (simulate flight), then force recall
            p.Position = new Vec2(p.Position.x, markedY + 10f);
            p.LastGroundedY = markedY + 10f;
            p.IsGrounded = false;

            // Force deactivation (recall)
            state.Players[0].SkillSlots[0].DurationRemaining = 0f;
            SkillSystem.Update(state, 0.016f);

            // LastGroundedY must equal the recall destination Y, not pre-ShadowStep Y
            Assert.AreEqual(p.Position.y, p.LastGroundedY, 0.01f,
                "ShadowStep recall must reset LastGroundedY to prevent inflated fall damage");
        }

        [Test]
        public void ShadowStep_RecallSkillEvent_Position_IsPreRecallPosition()
        {
            // Regression #464: SkillEvent.Position must be where the player came FROM,
            // not the mark (TargetPosition). Before fix both fields were equal to the mark.
            var state = CreateState();
            ref PlayerState p = ref state.Players[0];
            SetSkillSlot(ref p.SkillSlots[0], FindSkill(state.Config, SkillType.ShadowStep));
            state.Players[0].Energy = 100f;
            p.IsGrounded = true;

            // Activate ShadowStep — records current position as the recall mark
            Vec2 markPos = p.Position;
            SkillSystem.ActivateSkill(state, 0, 0);

            // Move player far away before recall fires
            Vec2 preRecallPos = new Vec2(markPos.x + 8f, markPos.y + 5f);
            p.Position = preRecallPos;
            p.IsGrounded = false;

            // Force deactivation (recall)
            state.SkillEvents.Clear();
            state.Players[0].SkillSlots[0].DurationRemaining = 0f;
            SkillSystem.Update(state, 0.016f);

            Assert.AreEqual(1, state.SkillEvents.Count, "Recall must emit exactly one SkillEvent");
            var ev = state.SkillEvents[0];
            Assert.AreEqual(SkillType.ShadowStep, ev.Type);

            // Position = where the player was before recall (not the mark)
            Assert.AreEqual(preRecallPos.x, ev.Position.x, 0.01f,
                "SkillEvent.Position must be the pre-recall position (from), not the mark");
            Assert.AreEqual(preRecallPos.y, ev.Position.y, 0.01f,
                "SkillEvent.Position must be the pre-recall position (from), not the mark");

            // TargetPosition = the mark (where they arrived)
            Assert.AreNotEqual(ev.Position.x, ev.TargetPosition.x,
                "Position and TargetPosition must differ — player moved away from the mark");
        }
    }
}
