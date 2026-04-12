using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public partial class SkillSystemTests
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
                DeathBoundaryY = -25f
            };
        }

        static GameState CreateState()
        {
            return GameSimulation.CreateMatch(SmallConfig(), 42);
        }

        // --- Activation guard tests ---

        [Test]
        public void ActivateSkill_DeductsEnergy()
        {
            var state = CreateState();
            float before = state.Players[0].Energy;
            float cost = state.Players[0].SkillSlots[0].EnergyCost;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(before - cost, state.Players[0].Energy, 0.01f);
        }

        [Test]
        public void ActivateSkill_SetsCooldownRemaining()
        {
            var state = CreateState();

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.Greater(state.Players[0].SkillSlots[0].CooldownRemaining, 0f);
        }

        [Test]
        public void ActivateSkill_AppliesCooldownMultiplier()
        {
            // Regression test for #31: skill cooldowns ignored CooldownMultiplier.
            var state = CreateState();
            state.Players[0].CooldownMultiplier = 0.5f;
            float baseCooldown = state.Players[0].SkillSlots[0].Cooldown;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(baseCooldown * 0.5f,
                state.Players[0].SkillSlots[0].CooldownRemaining, 0.01f,
                "Skill cooldown should be scaled by CooldownMultiplier (same as weapon firing)");
        }

        [Test]
        public void ActivateSkill_CooldownMultiplierAboveOne_SlowsSkill()
        {
            // Regression test for #31: multiplier > 1 should lengthen cooldown.
            var state = CreateState();
            state.Players[0].CooldownMultiplier = 2f;
            float baseCooldown = state.Players[0].SkillSlots[0].Cooldown;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(baseCooldown * 2f,
                state.Players[0].SkillSlots[0].CooldownRemaining, 0.01f);
        }

        [Test]
        public void ActivateSkill_OnCooldown_Blocked()
        {
            var state = CreateState();
            state.Players[0].SkillSlots[0].CooldownRemaining = 5f;
            float energyBefore = state.Players[0].Energy;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(energyBefore, state.Players[0].Energy, 0.01f);
        }

        [Test]
        public void ActivateSkill_InsufficientEnergy_Blocked()
        {
            var state = CreateState();
            state.Players[0].Energy = 0f;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(0f, state.Players[0].SkillSlots[0].CooldownRemaining, 0.01f);
        }

        [Test]
        public void ActivateSkill_DeadPlayer_Blocked()
        {
            var state = CreateState();
            state.Players[0].IsDead = true;
            float energyBefore = state.Players[0].Energy;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(energyBefore, state.Players[0].Energy, 0.01f);
        }

        [Test]
        public void ActivateSkill_FrozenPlayer_Blocked()
        {
            var state = CreateState();
            state.Players[0].FreezeTimer = 2f;
            float energyBefore = state.Players[0].Energy;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(energyBefore, state.Players[0].Energy, 0.01f,
                "Frozen player should not be able to activate skills");
            Assert.AreEqual(0f, state.Players[0].SkillSlots[0].CooldownRemaining, 0.01f,
                "Skill cooldown should not start when blocked by freeze");
        }

        [Test]
        public void ActivateSkill_EmitsSkillEvent()
        {
            var state = CreateState();

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(1, state.SkillEvents.Count);
            Assert.AreEqual(0, state.SkillEvents[0].PlayerIndex);
        }

        // --- Cooldown tests ---

        [Test]
        public void Cooldown_DecreasesOverTime()
        {
            var state = CreateState();
            SkillSystem.ActivateSkill(state, 0, 0);
            float cdBefore = state.Players[0].SkillSlots[0].CooldownRemaining;

            SkillSystem.Update(state, 1f);

            Assert.Less(state.Players[0].SkillSlots[0].CooldownRemaining, cdBefore);
        }

        [Test]
        public void Cooldown_ReachesZero_SkillAvailable()
        {
            var state = CreateState();
            SkillSystem.ActivateSkill(state, 0, 0);
            float cd = state.Players[0].SkillSlots[0].Cooldown;

            // Tick past the full cooldown
            SkillSystem.Update(state, cd + 1f);

            Assert.AreEqual(0f, state.Players[0].SkillSlots[0].CooldownRemaining, 0.01f);
        }

        // --- Helpers ---

        static void SetSkillSlot(ref SkillSlotState slot, SkillDef def)
        {
            slot = new SkillSlotState
            {
                SkillId = def.SkillId,
                Type = def.Type,
                EnergyCost = def.EnergyCost,
                Cooldown = def.Cooldown,
                Duration = def.Duration,
                Range = def.Range,
                Value = def.Value
            };
        }

        static SkillDef FindSkill(GameConfig config, SkillType type)
        {
            for (int i = 0; i < config.Skills.Length; i++)
                if (config.Skills[i].Type == type) return config.Skills[i];
            throw new System.Exception("Skill not found: " + type);
        }
    }
}
