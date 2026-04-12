using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Energy Drain skill tests ---

        [Test]
        public void EnergyDrain_ExistsInConfig()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Skills.Length >= 12);
            Assert.AreEqual("energy_drain", config.Skills[11].SkillId);
            Assert.AreEqual(SkillType.EnergyDrain, config.Skills[11].Type);
            Assert.AreEqual(30f, config.Skills[11].Value);
        }

        [Test]
        public void EnergyDrain_TransfersEnergyFromTarget()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(5f, 5f); // within range
            state.Players[0].Energy = 50f;
            state.Players[1].Energy = 80f;

            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "energy_drain", Type = SkillType.EnergyDrain,
                    EnergyCost = 0f, Cooldown = 0f, Range = 12f, Value = 30f
                },
                new SkillSlotState()
            };

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(50f, state.Players[1].Energy, 0.01f,
                "Target should lose 30 energy");
            Assert.AreEqual(80f, state.Players[0].Energy, 0.01f,
                "Caster should gain 30 energy");
            Assert.AreEqual(1, state.EnergyDrainEvents.Count);
            Assert.AreEqual(30f, state.EnergyDrainEvents[0].AmountDrained, 0.01f);
        }

        [Test]
        public void EnergyDrain_RefundsOnMiss()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(50f, 5f); // out of range (12)
            state.Players[0].Energy = 50f;

            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "energy_drain", Type = SkillType.EnergyDrain,
                    EnergyCost = 15f, Cooldown = 14f, Range = 12f, Value = 30f
                },
                new SkillSlotState()
            };

            SkillSystem.ActivateSkill(state, 0, 0);

            // Energy deducted by ActivateSkill (15), then refunded on whiff (+15)
            Assert.AreEqual(50f, state.Players[0].Energy, 0.01f,
                "Energy should be refunded when no target in range");
            Assert.AreEqual(0, state.EnergyDrainEvents.Count,
                "No drain event on miss");
            Assert.AreEqual(0f, state.Players[0].SkillSlots[0].CooldownRemaining, 0.01f,
                "Cooldown should not be set on whiff");
            Assert.AreEqual(0, state.SkillEvents.Count,
                "No SkillEvent should be emitted on whiff");
        }

        [Test]
        public void EnergyDrain_CapsAtMaxEnergy_NotOvercap()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[1].Position = new Vec2(5f, 5f); // within range
            state.Players[0].Energy = 90f;
            state.Players[0].MaxEnergy = 100f;
            state.Players[1].Energy = 80f;

            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "energy_drain", Type = SkillType.EnergyDrain,
                    EnergyCost = 0f, Cooldown = 0f, Range = 12f, Value = 30f
                },
                new SkillSlotState()
            };

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(100f, state.Players[0].Energy, 0.01f,
                "Caster energy should be capped at MaxEnergy, not exceed it");
            Assert.AreEqual(50f, state.Players[1].Energy, 0.01f,
                "Target should lose 30 energy");
        }

    }
}
