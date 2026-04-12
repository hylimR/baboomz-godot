using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class SkillSystemTests
    {
        // --- HookShot tests (#198) ---

        [Test]
        public void HookShot_PullsTargetTowardCaster()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]); // hookshot
            state.Players[0].Energy = 100f;
            // Place target within hookshot range (10 units)
            state.Players[1].Position = state.Players[0].Position + new Vec2(8f, 0f);

            Vec2 targetPosBefore = state.Players[1].Position;
            Vec2 casterPos = state.Players[0].Position;

            SkillSystem.ActivateSkill(state, 0, 0);

            // Target should be closer to caster than before
            float distBefore = Vec2.Distance(casterPos, targetPosBefore);
            float distAfter = Vec2.Distance(casterPos, state.Players[1].Position);
            Assert.Less(distAfter, distBefore, "Target should be pulled closer to caster");
        }

        [Test]
        public void HookShot_DealsDamage()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            state.Players[1].Position = state.Players[0].Position + new Vec2(8f, 0f);
            float hpBefore = state.Players[1].Health;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.Less(state.Players[1].Health, hpBefore, "Target should take damage");
            Assert.AreEqual(1, state.DamageEvents.Count, "Should emit a DamageEvent");
            Assert.AreEqual(1, state.DamageEvents[0].TargetIndex);
        }

        [Test]
        public void HookShot_NoTarget_RefundsEnergy()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;

            // Kill the only target
            state.Players[1].IsDead = true;

            float energyBefore = state.Players[0].Energy;
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(energyBefore, state.Players[0].Energy, 0.01f,
                "Energy should be refunded when no valid target");
            Assert.AreEqual(0, state.SkillEvents.Count,
                "No skill event should be emitted on refund");
        }

        [Test]
        public void HookShot_SkipsTeammates()
        {
            var state = CreateState();
            state.Config.TeamMode = true;
            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 0; // same team

            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            float energyBefore = state.Players[0].Energy;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(energyBefore, state.Players[0].Energy, 0.01f,
                "Energy should be refunded when only target is a teammate");
        }

        [Test]
        public void HookShot_SkipsFrozenTargets()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            state.Players[1].FreezeTimer = 5f; // frozen target

            float energyBefore = state.Players[0].Energy;
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(energyBefore, state.Players[0].Energy, 0.01f,
                "Energy should be refunded when only target is frozen");
        }

        [Test]
        public void HookShot_IsInstant()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive,
                "HookShot should be instant (no duration)");
        }

        [Test]
        public void HookShot_EmitsSkillEvent()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            state.Players[1].Position = state.Players[0].Position + new Vec2(8f, 0f);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(1, state.SkillEvents.Count);
            Assert.AreEqual(SkillType.HookShot, state.SkillEvents[0].Type);
        }

        [Test]
        public void HookShot_TracksDamageStats()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            state.Players[1].Position = state.Players[0].Position + new Vec2(8f, 0f);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.Greater(state.Players[0].TotalDamageDealt, 0f);
            Assert.AreEqual(1, state.Players[0].DirectHits);
        }

        [Test]
        public void HookShot_TargetGetsUpwardVelocity()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            state.Players[1].Position = state.Players[0].Position + new Vec2(8f, 0f);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.Greater(state.Players[1].Velocity.y, 0f,
                "Target should receive upward velocity after pull");
        }

        // --- Freeze override regression tests (#165) ---

        [Test]
        public void Jetpack_FrozenPlayer_ZerosVelocity()
        {
            var state = CreateState();
            ref var p = ref state.Players[0];
            SetSkillSlot(ref p.SkillSlots[0], state.Config.Skills[5]); // jetpack
            p.Energy = 100f;
            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(p.SkillSlots[0].IsActive, "Jetpack should be active");

            p.FreezeTimer = 2f;
            SkillSystem.Update(state, 0.016f);

            Assert.AreEqual(0f, p.Velocity.x, 0.01f, "Frozen jetpack player X velocity should be zero");
            Assert.AreEqual(0f, p.Velocity.y, 0.01f, "Frozen jetpack player Y velocity should be zero");
        }

        [Test]
        public void Dash_FrozenPlayer_ZerosVelocity()
        {
            var state = CreateState();
            ref var p = ref state.Players[0];
            SetSkillSlot(ref p.SkillSlots[0], state.Config.Skills[3]); // dash
            p.Energy = 100f;
            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(p.SkillSlots[0].IsActive, "Dash should be active");

            p.FreezeTimer = 2f;
            SkillSystem.Update(state, 0.016f);

            Assert.AreEqual(0f, p.Velocity.x, 0.01f, "Frozen dash player X velocity should be zero");
            Assert.AreEqual(0f, p.Velocity.y, 0.01f, "Frozen dash player Y velocity should be zero");
        }

        // --- Mend tests (#353) ---

        static void ClearTerrainRegion(TerrainState terrain, int cx, int cy, int radiusPx)
        {
            terrain.ClearCircle(cx, cy, radiusPx);
        }

        [Test]
        public void Mend_FillsDestroyedPixels_InRadius()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                FindSkill(state.Config, SkillType.Mend));
            state.Players[0].Energy = 100f;
            state.Players[0].AimAngle = 0f;
            state.Players[0].FacingDirection = 1;

            // Carve a hole 8 world units to the right of the player (within range 12)
            Vec2 target = state.Players[0].Position + new Vec2(12f, 0f); // matches ExecuteMend (clamped range)
            int cx = state.Terrain.WorldToPixelX(target.x);
            int cy = state.Terrain.WorldToPixelY(target.y);
            // Fill a patch solid first so there's something to blow up
            state.Terrain.FillRect(cx - 20, cy - 20, 40, 40);
            ClearTerrainRegion(state.Terrain, cx, cy, 15);
            Assert.IsFalse(state.Terrain.IsSolid(cx, cy), "Precondition: center pixel cleared");

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsTrue(state.Terrain.IsSolid(cx, cy),
                "Mend should refill the center of the crater");
        }

        [Test]
        public void Mend_DoesNotOverwriteIndestructible()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                FindSkill(state.Config, SkillType.Mend));
            state.Players[0].Energy = 100f;
            state.Players[0].AimAngle = 0f;
            state.Players[0].FacingDirection = 1;

            Vec2 target = state.Players[0].Position + new Vec2(12f, 0f); // matches ExecuteMend (clamped range)
            int cx = state.Terrain.WorldToPixelX(target.x);
            int cy = state.Terrain.WorldToPixelY(target.y);
            state.Terrain.SetIndestructible(cx, cy, true);
            Assert.IsTrue(state.Terrain.IsIndestructible(cx, cy));

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsTrue(state.Terrain.IsIndestructible(cx, cy),
                "Mend must not overwrite indestructible pixels");
        }

        [Test]
        public void Mend_SkipsPixelsUnderLivingPlayer()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                FindSkill(state.Config, SkillType.Mend));
            state.Players[0].Energy = 100f;
            state.Players[0].AimAngle = 0f;
            state.Players[0].FacingDirection = 1;

            // Put player 1 exactly where the mend target will land
            Vec2 target = state.Players[0].Position + new Vec2(12f, 0f); // matches ExecuteMend (clamped range)
            state.Players[1].Position = target;
            state.Players[1].IsDead = false;

            int cx = state.Terrain.WorldToPixelX(target.x);
            int cy = state.Terrain.WorldToPixelY(target.y + 0.5f); // inside player bbox
            // Ensure the pixel under player 1 is empty so we would otherwise fill it
            state.Terrain.SetSolid(cx, cy, false);
            Assert.IsFalse(state.Terrain.IsSolid(cx, cy));

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsFalse(state.Terrain.IsSolid(cx, cy),
                "Mend must not refill pixels under a living player");
        }

        [Test]
        public void Mend_DeductsEnergy_AndStartsCooldown()
        {
            var state = CreateState();
            var def = FindSkill(state.Config, SkillType.Mend);
            SetSkillSlot(ref state.Players[0].SkillSlots[0], def);
            state.Players[0].Energy = 100f;
            float before = state.Players[0].Energy;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(before - def.EnergyCost, state.Players[0].Energy, 0.01f);
            Assert.Greater(state.Players[0].SkillSlots[0].CooldownRemaining, 0f);
        }

        [Test]
        public void Mend_IsInstant_NoActiveDuration()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                FindSkill(state.Config, SkillType.Mend));
            state.Players[0].Energy = 100f;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive,
                "Mend should be instant — no active duration state");
        }

        [Test]
        public void Mend_EmitsSkillEvent()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                FindSkill(state.Config, SkillType.Mend));
            state.Players[0].Energy = 100f;
            int before = state.SkillEvents.Count;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(before + 1, state.SkillEvents.Count);
            Assert.AreEqual(SkillType.Mend, state.SkillEvents[state.SkillEvents.Count - 1].Type);
        }

        // --- EnergyDrain invulnerable bypass regression (#357) ---

        [Test]
        public void EnergyDrain_DoesNotDrainInvulnerableTarget()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                FindSkill(state.Config, SkillType.EnergyDrain));
            state.Players[0].Energy = 100f;
            // Place target close enough to be in range
            state.Players[1].Position = state.Players[0].Position + new Vec2(3f, 0f);
            state.Players[1].Energy = 50f;
            state.Players[1].IsInvulnerable = true;

            float targetEnergyBefore = state.Players[1].Energy;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(targetEnergyBefore, state.Players[1].Energy, 0.01f,
                "EnergyDrain must not drain energy from an invulnerable target");
        }

        // --- Decoy early-reveal skill-lock regression (#358) ---

        [Test]
        public void Decoy_EarlyReveal_ClearsSkillActive()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                FindSkill(state.Config, SkillType.Decoy));
            state.Players[0].Energy = 100f;

            // Activate Decoy
            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].SkillSlots[0].IsActive, "Decoy should be active after activation");

            // Simulate damage while decoy is active
            state.DamageEvents.Add(new DamageEvent { TargetIndex = 0, Amount = 10f });

            // Tick — this should trigger early reveal and deactivate the skill slot
            SkillSystem.Update(state, 0.016f);

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive,
                "After taking damage during Decoy, skill slot must be deactivated so other skills can be used");
            Assert.IsFalse(state.Players[0].IsInvisible,
                "Player must be visible after being hit during Decoy");
        }

        [Test]
        public void Decoy_EarlyReveal_UnlocksOtherSkillActivation()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                FindSkill(state.Config, SkillType.Decoy));
            // Put a second skill (Heal) in slot 1
            SetSkillSlot(ref state.Players[0].SkillSlots[1],
                FindSkill(state.Config, SkillType.Heal));
            state.Players[0].Energy = 100f;

            // Activate Decoy (slot 0)
            SkillSystem.ActivateSkill(state, 0, 0);

            // Damage player — triggers early reveal
            state.DamageEvents.Add(new DamageEvent { TargetIndex = 0, Amount = 5f });
            SkillSystem.Update(state, 0.016f);

            // Now slot 0 is deactivated; slot 1 should be activatable
            float cooldown1Before = state.Players[0].SkillSlots[1].CooldownRemaining;
            SkillSystem.ActivateSkill(state, 0, 1);

            Assert.Greater(state.Players[0].SkillSlots[1].CooldownRemaining, cooldown1Before,
                "After Decoy early reveal, other skill slots should be activatable again");
        }

    }
}
