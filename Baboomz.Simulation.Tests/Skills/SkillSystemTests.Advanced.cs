using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class SkillSystemTests
    {
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

            Vec2 target = state.Players[0].Position + new Vec2(12f, 0f);
            int cx = state.Terrain.WorldToPixelX(target.x);
            int cy = state.Terrain.WorldToPixelY(target.y);
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

            Vec2 target = state.Players[0].Position + new Vec2(12f, 0f);
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

            Vec2 target = state.Players[0].Position + new Vec2(12f, 0f);
            state.Players[1].Position = target;
            state.Players[1].IsDead = false;

            int cx = state.Terrain.WorldToPixelX(target.x);
            int cy = state.Terrain.WorldToPixelY(target.y + 0.5f);
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

            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].SkillSlots[0].IsActive, "Decoy should be active after activation");

            state.DamageEvents.Add(new DamageEvent { TargetIndex = 0, Amount = 10f });

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
            SetSkillSlot(ref state.Players[0].SkillSlots[1],
                FindSkill(state.Config, SkillType.Heal));
            state.Players[0].Energy = 100f;

            SkillSystem.ActivateSkill(state, 0, 0);

            state.DamageEvents.Add(new DamageEvent { TargetIndex = 0, Amount = 5f });
            SkillSystem.Update(state, 0.016f);

            float cooldown1Before = state.Players[0].SkillSlots[1].CooldownRemaining;
            SkillSystem.ActivateSkill(state, 0, 1);

            Assert.Greater(state.Players[0].SkillSlots[1].CooldownRemaining, cooldown1Before,
                "After Decoy early reveal, other skill slots should be activatable again");
        }

        [Test]
        public void Decoy_Cooldown_AlignedWithDeflect_Issue173()
        {
            var cfg = new GameConfig();
            SkillDef? decoy = null;
            SkillDef? deflect = null;
            foreach (var s in cfg.Skills)
            {
                if (s.SkillId == "decoy") decoy = s;
                if (s.SkillId == "deflect") deflect = s;
            }

            Assert.NotNull(decoy, "Decoy skill missing from GameConfig.Skills");
            Assert.NotNull(deflect, "Deflect skill missing from GameConfig.Skills");
            Assert.AreEqual(13f, decoy!.Value.Cooldown, 0.001f,
                "Decoy CD should be 13s (issue #173)");
            Assert.AreEqual(decoy!.Value.Cooldown, deflect!.Value.Cooldown, 0.001f,
                "Decoy CD should match Deflect CD — both are reactive evasion skills");
        }

    }
}
