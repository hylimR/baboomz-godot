using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class SkillSystemTests
    {
        [Test]
        public void Petrify_ExistsInConfig()
        {
            var config = new GameConfig();
            bool found = false;
            for (int i = 0; i < config.Skills.Length; i++)
            {
                if (config.Skills[i].SkillId == "petrify")
                {
                    Assert.AreEqual(SkillType.Petrify, config.Skills[i].Type);
                    Assert.AreEqual(35f, config.Skills[i].EnergyCost);
                    Assert.AreEqual(14f, config.Skills[i].Cooldown);
                    Assert.AreEqual(2f, config.Skills[i].Duration);
                    Assert.AreEqual(10f, config.Skills[i].Range);
                    Assert.AreEqual(2f, config.Skills[i].Value);
                    found = true;
                }
            }
            Assert.IsTrue(found, "petrify should exist in GameConfig.Skills");
        }

        [Test]
        public void Petrify_FreezesEnemyInRadius()
        {
            var state = CreateState();

            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].AimAngle = 0f;
            state.Players[0].Energy = 100f;
            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "petrify", Type = SkillType.Petrify,
                    EnergyCost = 35f, Cooldown = 16f, Duration = 2f,
                    Range = 10f, Value = 2f
                },
                new SkillSlotState()
            };

            // Place enemy at aim target (10u in front of caster)
            state.Players[1].Position = new Vec2(10f, 10f);
            state.Players[1].FreezeTimer = 0f;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(2f, state.Players[1].FreezeTimer, 0.01f);
            Assert.AreEqual(65f, state.Players[0].Energy, 0.01f);
        }

        [Test]
        public void Petrify_DoesNotFreezeOutOfRadius()
        {
            var state = CreateState();

            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].AimAngle = 0f;
            state.Players[0].Energy = 100f;
            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "petrify", Type = SkillType.Petrify,
                    EnergyCost = 35f, Cooldown = 16f, Duration = 2f,
                    Range = 10f, Value = 2f
                },
                new SkillSlotState()
            };

            // Place enemy far from target position
            state.Players[1].Position = new Vec2(15f, 10f);
            state.Players[1].FreezeTimer = 0f;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(0f, state.Players[1].FreezeTimer);
        }

        [Test]
        public void Petrify_BlockedWhileCasterFrozen()
        {
            var state = CreateState();

            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].AimAngle = 0f;
            state.Players[0].Energy = 100f;
            state.Players[0].FreezeTimer = 1f;
            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "petrify", Type = SkillType.Petrify,
                    EnergyCost = 35f, Cooldown = 16f, Duration = 2f,
                    Range = 10f, Value = 2f
                },
                new SkillSlotState()
            };

            state.Players[1].Position = new Vec2(10f, 10f);
            state.Players[1].FreezeTimer = 0f;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(0f, state.Players[1].FreezeTimer);
            Assert.AreEqual(100f, state.Players[0].Energy);
        }

        [Test]
        public void Petrify_DoesNotFreezeSelf()
        {
            var state = CreateState();

            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].AimAngle = 0f;
            state.Players[0].Energy = 100f;
            state.Players[0].FreezeTimer = 0f;
            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "petrify", Type = SkillType.Petrify,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 2f,
                    Range = 0.1f, Value = 50f // huge radius, tiny range = targets self position
                },
                new SkillSlotState()
            };

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(0f, state.Players[0].FreezeTimer);
        }

        [Test]
        public void Petrify_DoesNotFreezeTeammates()
        {
            var state = CreateState();
            state.Config.TeamMode = true;

            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].AimAngle = 0f;
            state.Players[0].Energy = 100f;
            state.Players[0].TeamIndex = 0;
            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "petrify", Type = SkillType.Petrify,
                    EnergyCost = 35f, Cooldown = 16f, Duration = 2f,
                    Range = 10f, Value = 2f
                },
                new SkillSlotState()
            };

            state.Players[1].Position = new Vec2(10f, 10f);
            state.Players[1].TeamIndex = 0; // same team
            state.Players[1].FreezeTimer = 0f;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(0f, state.Players[1].FreezeTimer);
        }

        [Test]
        public void Petrify_ExtendsExistingFreeze()
        {
            var state = CreateState();

            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].AimAngle = 0f;
            state.Players[0].Energy = 100f;
            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "petrify", Type = SkillType.Petrify,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 2f,
                    Range = 10f, Value = 2f
                },
                new SkillSlotState()
            };

            // Target already has a short freeze
            state.Players[1].Position = new Vec2(10f, 10f);
            state.Players[1].FreezeTimer = 0.5f;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(2f, state.Players[1].FreezeTimer, 0.01f);
        }
    }
}
