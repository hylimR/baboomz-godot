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

        [Test]
        public void Teleport_Config_BalancedVsDash_Issue171()
        {
            // Balance #171: Teleport 40E→28E so it's no longer 2.2x worse E/u than Dash.
            // 28E/15u = 1.87 E/u — still pricier than Dash (1.20) but viable for
            // instant terrain-bypass repositioning.
            var cfg = new GameConfig();
            SkillDef? teleport = null;
            foreach (var s in cfg.Skills)
                if (s.SkillId == "teleport") { teleport = s; break; }

            Assert.NotNull(teleport, "Teleport skill missing from GameConfig.Skills");
            Assert.AreEqual(28f, teleport!.Value.EnergyCost, 0.001f, "Teleport EnergyCost");
            Assert.AreEqual(8f, teleport!.Value.Cooldown, 0.001f, "Teleport Cooldown (unchanged)");
            Assert.AreEqual(15f, teleport!.Value.Range, 0.001f, "Teleport Range (unchanged)");
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

        [Test]
        public void Dash_Config_BalancedForShortFrequentDisengage()
        {
            // Balance #155: Dash owns the "cheap frequent short disengage" niche.
            // Expected: 18E / 3s CD / 0.3s duration / 50u burst velocity
            // (~15u reach — shortest mobility but fastest to recover and cheapest).
            var cfg = new GameConfig();
            SkillDef? dash = null;
            foreach (var s in cfg.Skills)
                if (s.SkillId == "dash") { dash = s; break; }

            Assert.NotNull(dash, "Dash skill missing from GameConfig.Skills");
            Assert.AreEqual(18f, dash!.Value.EnergyCost, 0.001f, "Dash EnergyCost");
            Assert.AreEqual(3f, dash!.Value.Cooldown, 0.001f, "Dash Cooldown");
            Assert.AreEqual(0.3f, dash!.Value.Duration, 0.001f, "Dash Duration");
            Assert.AreEqual(50f, dash!.Value.Value, 0.001f, "Dash burst velocity (Value)");

            // Invariant: remains the cheapest mobility skill (vs grapple/jetpack/teleport/shadow_step)
            foreach (var s in cfg.Skills)
            {
                if (s.SkillId is "grapple" or "jetpack" or "teleport" or "shadow_step")
                    Assert.Less(dash!.Value.EnergyCost, s.EnergyCost,
                        $"Dash must remain cheaper than {s.SkillId}");
            }
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

    }
}
