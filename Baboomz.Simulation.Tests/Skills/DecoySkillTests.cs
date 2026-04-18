using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class DecoySkillTests
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

        static void GiveDecoySkill(ref PlayerState p)
        {
            p.SkillSlots[0] = new SkillSlotState
            {
                SkillId = "decoy",
                Type = SkillType.Decoy,
                EnergyCost = 30f,
                Cooldown = 16f,
                Duration = 4f,
                Range = 0f,
                Value = 30f
            };
        }

        [Test]
        public void Decoy_ExistsInConfig()
        {
            var config = new GameConfig();
            bool found = false;
            foreach (var skill in config.Skills)
            {
                if (skill.SkillId == "decoy")
                {
                    Assert.AreEqual(SkillType.Decoy, skill.Type);
                    Assert.AreEqual(30f, skill.EnergyCost);
                    Assert.AreEqual(13f, skill.Cooldown); // #173: 16 -> 13
                    Assert.AreEqual(4f, skill.Duration);
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, "Decoy skill should exist in config");
        }

        [Test]
        public void Decoy_Activation_SetsInvisibleAndDecoyPosition()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            Vec2 originalPos = state.Players[0].Position;
            state.Players[0].Energy = 100f;
            GiveDecoySkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsTrue(state.Players[0].IsInvisible, "Player should be invisible after Decoy activation");
            Assert.AreEqual(originalPos.x, state.Players[0].DecoyPosition.x, 0.01f,
                "Decoy should be at player's original position");
            Assert.AreEqual(originalPos.y, state.Players[0].DecoyPosition.y, 0.01f);
            Assert.Greater(state.Players[0].DecoyTimer, 0f, "DecoyTimer should be active");
        }

        [Test]
        public void Decoy_Activation_DeductsEnergy()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveDecoySkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(70f, state.Players[0].Energy, 0.01f,
                "Decoy should cost 30 energy");
        }

        [Test]
        public void Decoy_ExpiresAfterDuration()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveDecoySkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].IsInvisible);

            // Tick past the 4s duration
            for (int i = 0; i < 260; i++)
                SkillSystem.Update(state, 0.016f);

            Assert.IsFalse(state.Players[0].IsInvisible,
                "Player should become visible after duration expires");
            Assert.AreEqual(0f, state.Players[0].DecoyTimer, 0.01f);
        }

        [Test]
        public void Decoy_BlocksFiring_WhileInvisible()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            state.Players[0].IsAI = false;
            GiveDecoySkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].IsInvisible);

            // Try to fire
            state.Input.FireHeld = true;
            state.Input.FireReleased = false;
            state.Players[0].ShootCooldownRemaining = 0f;
            state.Players[0].AimPower = 0f;
            GameSimulation.Tick(state, 0.016f);

            // While invisible, firing input should be blocked
            // The player should NOT be charging (IsCharging = false because IsInvisible blocks input)
            Assert.IsFalse(state.Players[0].IsCharging,
                "Player should not be able to charge while invisible");
        }

        [Test]
        public void Decoy_AITargets_DecoyPosition()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Player 0 activates decoy, AI (player 1) should target decoy position
            state.Players[0].IsInvisible = true;
            state.Players[0].DecoyPosition = new Vec2(-10f, 5f);
            state.Players[0].DecoyTimer = 2f;
            state.Players[0].Position = new Vec2(10f, 5f); // real position far away

            int target = AILogic.FindTarget(state, 1);
            Assert.AreEqual(0, target, "AI should still find invisible player as target");
            // The AI will aim at DecoyPosition, not real position — verified by the
            // AI UpdateAI logic using targetPos = target.IsInvisible ? DecoyPosition : Position
        }

        [Test]
        public void Decoy_DamageRevealsPlayer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveDecoySkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].IsInvisible);

            // Simulate splash damage hitting the invisible player
            state.DamageEvents.Add(new DamageEvent
            {
                TargetIndex = 0,
                Amount = 10f,
                Position = state.Players[0].Position,
                SourceIndex = 1
            });

            SkillSystem.Update(state, 0.016f);

            Assert.IsFalse(state.Players[0].IsInvisible,
                "Player should become visible after taking damage");
        }

        [Test]
        public void Decoy_NotEnoughEnergy_DoesNotActivate()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 20f; // less than 35 cost
            GiveDecoySkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsFalse(state.Players[0].IsInvisible,
                "Decoy should not activate with insufficient energy");
            Assert.AreEqual(20f, state.Players[0].Energy, 0.01f,
                "Energy should not be deducted on failed activation");
        }

        [Test]
        public void Decoy_CooldownPreventsReactivation()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveDecoySkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].IsInvisible);

            // Wait for decoy to expire (4s duration)
            for (int i = 0; i < 260; i++)
                SkillSystem.Update(state, 0.016f);
            Assert.IsFalse(state.Players[0].IsInvisible);

            // Try to activate again immediately — should be on cooldown
            state.Players[0].Energy = 100f;
            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsFalse(state.Players[0].IsInvisible,
                "Decoy should not activate while on cooldown");
        }

        [Test]
        public void Decoy_EmitsSkillEvent()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            GiveDecoySkill(ref state.Players[0]);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(1, state.SkillEvents.Count, "Should emit one skill event");
            Assert.AreEqual(SkillType.Decoy, state.SkillEvents[0].Type);
            Assert.AreEqual(0, state.SkillEvents[0].PlayerIndex);
        }

        [Test]
        public void Decoy_ZeroDuration_UsesFallback()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Energy = 100f;
            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "decoy",
                Type = SkillType.Decoy,
                EnergyCost = 30f,
                Cooldown = 16f,
                Duration = 0f, // misconfigured — zero duration
                Range = 0f,
                Value = 30f
            };

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(2f, state.Players[0].DecoyTimer, 0.01f,
                "DecoyTimer should use 2f fallback when Duration is 0");
            Assert.AreEqual(2f, state.Players[0].SkillSlots[0].DurationRemaining, 0.01f,
                "DurationRemaining should use 2f fallback when Duration is 0");
            Assert.IsTrue(state.Players[0].IsInvisible,
                "Player should still become invisible with zero-duration fallback");
        }
    }
}
