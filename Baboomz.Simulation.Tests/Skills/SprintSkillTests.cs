using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class SprintSkillTests
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

        static SkillDef FindSkill(GameConfig config, SkillType type)
        {
            for (int i = 0; i < config.Skills.Length; i++)
                if (config.Skills[i].Type == type) return config.Skills[i];
            throw new Exception("Skill not found: " + type);
        }

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

        // --- Config tests ---

        [Test]
        public void Sprint_Config_MatchesDesign_Issue285()
        {
            var cfg = new GameConfig();
            SkillDef? sprint = null;
            foreach (var s in cfg.Skills)
                if (s.SkillId == "sprint") { sprint = s; break; }

            Assert.NotNull(sprint, "Sprint skill missing from GameConfig.Skills");
            Assert.AreEqual(SkillType.Sprint, sprint!.Value.Type);
            Assert.AreEqual(22f, sprint!.Value.EnergyCost, 0.001f, "Sprint EnergyCost");
            Assert.AreEqual(7f, sprint!.Value.Cooldown, 0.001f, "Sprint Cooldown");
            Assert.AreEqual(2f, sprint!.Value.Duration, 0.001f, "Sprint Duration");
            Assert.AreEqual(1.5f, sprint!.Value.Value, 0.001f, "Sprint SpeedMult");
        }

        // --- Activation tests ---

        [Test]
        public void Sprint_Activation_IncreasesSpeed()
        {
            var state = CreateState();
            var sprintDef = FindSkill(state.Config, SkillType.Sprint);
            SetSkillSlot(ref state.Players[0].SkillSlots[1], sprintDef);

            float speedBefore = state.Players[0].MoveSpeed;

            SkillSystem.ActivateSkill(state, 0, 1);

            Assert.AreEqual(speedBefore * 1.5f, state.Players[0].MoveSpeed, 0.01f,
                "Sprint should multiply MoveSpeed by 1.5x");
        }

        [Test]
        public void Sprint_Activation_SetsTimersAndActive()
        {
            var state = CreateState();
            var sprintDef = FindSkill(state.Config, SkillType.Sprint);
            SetSkillSlot(ref state.Players[0].SkillSlots[1], sprintDef);

            SkillSystem.ActivateSkill(state, 0, 1);

            Assert.IsTrue(state.Players[0].SkillSlots[1].IsActive, "Skill should be active");
            Assert.AreEqual(2f, state.Players[0].SkillSlots[1].DurationRemaining, 0.01f);
            Assert.Greater(state.Players[0].SprintTimer, 0f, "SprintTimer should be set");
            Assert.AreEqual(1.5f, state.Players[0].SprintSpeedBuff, 0.01f, "SprintSpeedBuff should store multiplier");
        }

        [Test]
        public void Sprint_Activation_DeductsEnergy()
        {
            var state = CreateState();
            var sprintDef = FindSkill(state.Config, SkillType.Sprint);
            SetSkillSlot(ref state.Players[0].SkillSlots[1], sprintDef);

            float energyBefore = state.Players[0].Energy;

            SkillSystem.ActivateSkill(state, 0, 1);

            Assert.AreEqual(energyBefore - 22f, state.Players[0].Energy, 0.01f);
        }

        // --- Duration and expiry tests ---

        [Test]
        public void Sprint_ExpiresAfterDuration_RestoresSpeed()
        {
            var state = CreateState();
            var sprintDef = FindSkill(state.Config, SkillType.Sprint);
            SetSkillSlot(ref state.Players[0].SkillSlots[1], sprintDef);

            float speedBefore = state.Players[0].MoveSpeed;

            SkillSystem.ActivateSkill(state, 0, 1);
            Assert.IsTrue(state.Players[0].SkillSlots[1].IsActive);

            // Tick past sprint duration (2s)
            SkillSystem.Update(state, 3f);

            Assert.IsFalse(state.Players[0].SkillSlots[1].IsActive,
                "Sprint should deactivate after duration");
            Assert.AreEqual(speedBefore, state.Players[0].MoveSpeed, 0.01f,
                "MoveSpeed should be restored after Sprint expires");
        }

        [Test]
        public void Sprint_BuffTimer_ClearsAfterDuration()
        {
            var state = CreateState();
            var sprintDef = FindSkill(state.Config, SkillType.Sprint);
            SetSkillSlot(ref state.Players[0].SkillSlots[1], sprintDef);

            SkillSystem.ActivateSkill(state, 0, 1);

            // Tick past sprint duration
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0f, state.Players[0].SprintTimer, 0.01f);
            Assert.AreEqual(0f, state.Players[0].SprintSpeedBuff, 0.01f);
        }

        // --- Fire block tests ---

        [Test]
        public void Sprint_BlocksFiring()
        {
            var state = CreateState();
            state.Phase = MatchPhase.Playing;
            var sprintDef = FindSkill(state.Config, SkillType.Sprint);
            SetSkillSlot(ref state.Players[0].SkillSlots[1], sprintDef);

            // Activate sprint
            SkillSystem.ActivateSkill(state, 0, 1);
            Assert.Greater(state.Players[0].SprintTimer, 0f);

            // Attempt to fire
            int shotsBefore = state.Players[0].ShotsFired;
            state.PlayerInputs[0].FireHeld = true;
            GameSimulation.Tick(state, 0.1f);
            state.PlayerInputs[0].FireHeld = false;
            state.PlayerInputs[0].FireReleased = true;
            GameSimulation.Tick(state, 0.1f);

            Assert.AreEqual(shotsBefore, state.Players[0].ShotsFired,
                "Player should not be able to fire while sprinting");
        }

        // --- Deactivation on death ---

        [Test]
        public void Sprint_DeactivatesOnDeath()
        {
            var state = CreateState();
            var sprintDef = FindSkill(state.Config, SkillType.Sprint);
            SetSkillSlot(ref state.Players[0].SkillSlots[1], sprintDef);

            float speedBefore = state.Players[0].MoveSpeed;
            SkillSystem.ActivateSkill(state, 0, 1);
            Assert.IsTrue(state.Players[0].SkillSlots[1].IsActive);

            // Kill the player
            state.Players[0].IsDead = true;
            SkillSystem.Update(state, 0.1f);

            Assert.IsFalse(state.Players[0].SkillSlots[1].IsActive,
                "Sprint should deactivate on death");
            Assert.AreEqual(speedBefore, state.Players[0].MoveSpeed, 0.01f,
                "MoveSpeed should be restored on death");
        }

        // --- Cooldown test ---

        [Test]
        public void Sprint_SetsCooldown()
        {
            var state = CreateState();
            var sprintDef = FindSkill(state.Config, SkillType.Sprint);
            SetSkillSlot(ref state.Players[0].SkillSlots[1], sprintDef);

            SkillSystem.ActivateSkill(state, 0, 1);

            float expected = 7f * state.Players[0].CooldownMultiplier;
            Assert.AreEqual(expected, state.Players[0].SkillSlots[1].CooldownRemaining, 0.01f,
                "Sprint cooldown should be 7s * CooldownMultiplier");
        }

        // --- Emits skill event ---

        [Test]
        public void Sprint_EmitsSkillEvent()
        {
            var state = CreateState();
            var sprintDef = FindSkill(state.Config, SkillType.Sprint);
            SetSkillSlot(ref state.Players[0].SkillSlots[1], sprintDef);

            SkillSystem.ActivateSkill(state, 0, 1);

            Assert.AreEqual(1, state.SkillEvents.Count);
            Assert.AreEqual(SkillType.Sprint, state.SkillEvents[0].Type);
        }

        // --- Encyclopedia ---

        [Test]
        public void Sprint_EncyclopediaDescription_NotUnknown()
        {
            string desc = EncyclopediaContent.GetSkillDescription("sprint");
            Assert.AreNotEqual("Unknown skill.", desc);
            Assert.IsFalse(string.IsNullOrEmpty(desc));
        }

        [Test]
        public void Sprint_EncyclopediaEffectDescription_NotUnknown()
        {
            string desc = EncyclopediaContent.GetSkillEffectDescription("sprint", 1.5f);
            Assert.AreNotEqual("Unknown effect", desc);
            Assert.IsFalse(string.IsNullOrEmpty(desc));
        }

        // --- Unlock registry ---

        [Test]
        public void Sprint_UnlockedAtTier1()
        {
            Assert.IsFalse(UnlockRegistry.IsSkillIndexUnlocked(20, 0),
                "Sprint should not be unlocked at Tier 0");
            Assert.IsTrue(UnlockRegistry.IsSkillIndexUnlocked(20, 1),
                "Sprint should be unlocked at Tier 1 (Veteran)");
        }
    }
}
