using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class ShadowStepSkillTests
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
                DefaultMoveSpeed = 5f,
                DefaultJumpForce = 10f,
                DefaultShootCooldown = 0.5f
            };
        }

        static void GiveShadowStepSkill(ref PlayerState p)
        {
            p.SkillSlots[0] = new SkillSlotState
            {
                SkillId = "shadow_step",
                Type = SkillType.ShadowStep,
                EnergyCost = 25f,
                Cooldown = 12f,
                Duration = 3f,
                Range = 0f,
                Value = 0f
            };
        }

        [Test]
        public void ShadowStep_ExistsInConfig()
        {
            var config = new GameConfig();
            bool found = false;
            foreach (var skill in config.Skills)
            {
                if (skill.SkillId == "shadow_step")
                {
                    Assert.AreEqual(SkillType.ShadowStep, skill.Type);
                    Assert.AreEqual(25f, skill.EnergyCost);
                    Assert.AreEqual(12f, skill.Cooldown);
                    Assert.AreEqual(3f, skill.Duration);
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, "ShadowStep skill should exist in config");
        }

        [Test]
        public void ShadowStep_Activation_StoresPositionAndActivates()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            Vec2 originalPos = state.Players[0].Position;
            state.Players[0].Energy = 100f;
            GiveShadowStepSkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsTrue(state.Players[0].SkillSlots[0].IsActive, "ShadowStep should be active");
            Assert.AreEqual(originalPos.x, state.Players[0].SkillTargetPosition.x, 0.01f,
                "Mark position X should match original");
            Assert.AreEqual(originalPos.y, state.Players[0].SkillTargetPosition.y, 0.01f,
                "Mark position Y should match original");
        }

        [Test]
        public void ShadowStep_Activation_DeductsEnergy()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveShadowStepSkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(75f, state.Players[0].Energy, 0.01f,
                "ShadowStep should cost 25 energy");
        }

        [Test]
        public void ShadowStep_ExpiresAfterDuration_RecallsPlayer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveShadowStepSkill(ref state.Players[0]);

            Vec2 markPos = state.Players[0].Position;
            SkillSystem.ActivateSkill(state, 0, 0);

            // Move player away from mark
            state.Players[0].Position = new Vec2(markPos.x + 5f, markPos.y);

            // Tick past the 3s duration
            for (int i = 0; i < 200; i++)
                SkillSystem.Update(state, 0.016f);

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive,
                "ShadowStep should have expired");
            Assert.AreEqual(markPos.x, state.Players[0].Position.x, 0.5f,
                "Player should recall to mark position X");
        }

        [Test]
        public void ShadowStep_EarlyReturn_RecallsPlayer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveShadowStepSkill(ref state.Players[0]);

            Vec2 markPos = state.Players[0].Position;
            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].SkillSlots[0].IsActive);

            // Move player away
            state.Players[0].Position = new Vec2(markPos.x + 5f, markPos.y);

            // Re-activate to trigger early return
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive,
                "ShadowStep should deactivate on early return");
            Assert.AreEqual(markPos.x, state.Players[0].Position.x, 0.5f,
                "Player should recall to mark position X on early return");
        }

        [Test]
        public void ShadowStep_RecallResetsVelocity()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveShadowStepSkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);
            state.Players[0].Position = new Vec2(5f, 5f);
            state.Players[0].Velocity = new Vec2(10f, 5f);

            // Early return
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(0f, state.Players[0].Velocity.x, 0.01f, "Velocity X should be zero after recall");
            Assert.AreEqual(0f, state.Players[0].Velocity.y, 0.01f, "Velocity Y should be zero after recall");
        }

        [Test]
        public void ShadowStep_Death_ClearsActiveState()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveShadowStepSkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].SkillSlots[0].IsActive);

            // Kill the player
            state.Players[0].IsDead = true;
            SkillSystem.Update(state, 0.016f);

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive,
                "ShadowStep should deactivate on death");
        }

        [Test]
        public void ShadowStep_NotEnoughEnergy_DoesNotActivate()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 10f; // less than 25 cost
            GiveShadowStepSkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive,
                "ShadowStep should not activate with insufficient energy");
            Assert.AreEqual(10f, state.Players[0].Energy, 0.01f,
                "Energy should not be deducted on failed activation");
        }

        [Test]
        public void ShadowStep_EmitsSkillEvent_OnActivation()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveShadowStepSkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(1, state.SkillEvents.Count, "Should emit one skill event on activation");
            Assert.AreEqual(SkillType.ShadowStep, state.SkillEvents[0].Type);
            Assert.AreEqual(0, state.SkillEvents[0].PlayerIndex);
        }

        [Test]
        public void ShadowStep_EmitsSkillEvent_OnRecall()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveShadowStepSkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);
            state.SkillEvents.Clear(); // clear activation event

            // Trigger early return
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.GreaterOrEqual(state.SkillEvents.Count, 1, "Should emit skill event on recall");
            Assert.AreEqual(SkillType.ShadowStep, state.SkillEvents[0].Type);
        }

        [Test]
        public void ShadowStep_CooldownStartsOnActivation()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveShadowStepSkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);

            // Cooldown is scaled by player's CooldownMultiplier (issue #31).
            // Seed 42 lands on "Clockwork Foundry" biome which sets DefaultCooldownMultiplier=0.8.
            float expected = 12f * state.Players[0].CooldownMultiplier;
            Assert.AreEqual(expected, state.Players[0].SkillSlots[0].CooldownRemaining, 0.01f,
                "Cooldown should start on activation (scaled by CooldownMultiplier)");
        }
    }
}
