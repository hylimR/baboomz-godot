using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class SkillSystemTests
    {
        [Test]
        public void MagneticMine_ExistsInConfig()
        {
            var config = new GameConfig();
            bool found = false;
            for (int i = 0; i < config.Skills.Length; i++)
            {
                if (config.Skills[i].SkillId == "magnetic_mine")
                {
                    Assert.AreEqual(SkillType.MagneticMine, config.Skills[i].Type);
                    Assert.AreEqual(30f, config.Skills[i].EnergyCost);
                    Assert.AreEqual(12f, config.Skills[i].Cooldown);
                    Assert.AreEqual(30f, config.Skills[i].Value);
                    found = true;
                }
            }
            Assert.IsTrue(found, "magnetic_mine should exist in GameConfig.Skills");
        }

        [Test]
        public void MagneticMine_PlacesHomingMine()
        {
            var state = CreateState();
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].AimAngle = 0f;
            state.Players[0].Energy = 100f;
            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "magnetic_mine", Type = SkillType.MagneticMine,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 0f,
                    Range = 10f, Value = 30f
                },
                new SkillSlotState()
            };

            int minesBefore = state.Mines.Count;
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(minesBefore + 1, state.Mines.Count);
            var mine = state.Mines[state.Mines.Count - 1];
            Assert.IsTrue(mine.Active);
            Assert.IsTrue(mine.IsHoming);
            Assert.AreEqual(8f, mine.DetectionRange);
            Assert.AreEqual(1.5f, mine.MoveSpeed);
            Assert.AreEqual(1.0f, mine.TriggerRadius);
            Assert.AreEqual(12f, mine.Lifetime);
            Assert.AreEqual(1f, mine.ActivationDelay, 0.01f);
        }

        [Test]
        public void MagneticMine_MaxTwoPerPlayer()
        {
            var state = CreateState();
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].Energy = 1000f;
            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "magnetic_mine", Type = SkillType.MagneticMine,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 0f,
                    Range = 10f, Value = 30f
                },
                new SkillSlotState()
            };

            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.ActivateSkill(state, 0, 0);

            int activeHoming = 0;
            for (int i = 0; i < state.Mines.Count; i++)
                if (state.Mines[i].Active && state.Mines[i].IsHoming && state.Mines[i].OwnerIndex == 0)
                    activeHoming++;

            Assert.LessOrEqual(activeHoming, 2, "Max 2 active magnetic mines per player");
        }

        [Test]
        public void MagneticMine_DormantDuringActivationDelay()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[1].Position = new Vec2(5f, 5f);

            state.Mines.Add(new MineState
            {
                Position = new Vec2(5f, 5f),
                TriggerRadius = 1.0f,
                ExplosionRadius = 2.5f,
                Damage = 30f,
                Active = true,
                Lifetime = 12f,
                OwnerIndex = 0,
                IsHoming = true,
                DetectionRange = 8f,
                MoveSpeed = 1.5f,
                ActivationDelay = 1f
            });

            float healthBefore = state.Players[1].Health;
            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(healthBefore, state.Players[1].Health, 0.01f,
                "Mine should not trigger during activation delay");
        }

        [Test]
        public void MagneticMine_MovesTowardEnemy()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(-15f, 5f);
            state.Players[0].IsDead = false;
            state.Players[1].Position = new Vec2(5f, 5f);
            state.Players[1].IsDead = false;

            state.Mines.Add(new MineState
            {
                Position = new Vec2(0f, 5f),
                TriggerRadius = 1.0f,
                ExplosionRadius = 2.5f,
                Damage = 30f,
                Active = true,
                Lifetime = 12f,
                OwnerIndex = 0,
                IsHoming = true,
                DetectionRange = 8f,
                MoveSpeed = 1.5f,
                ActivationDelay = 0f
            });

            float startX = state.Mines[state.Mines.Count - 1].Position.x;
            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.5f);

            bool mineFound = false;
            for (int i = 0; i < state.Mines.Count; i++)
            {
                if (state.Mines[i].IsHoming && state.Mines[i].OwnerIndex == 0 && state.Mines[i].Active)
                {
                    Assert.Greater(state.Mines[i].Position.x, startX,
                        "Mine should move toward enemy (positive X direction)");
                    mineFound = true;
                }
            }
            Assert.IsTrue(mineFound, "Homing mine should still exist");
        }

        [Test]
        public void MagneticMine_DoesNotMoveWhenNoEnemyInRange()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(-15f, 5f);
            state.Players[1].Position = new Vec2(20f, 5f); // far away (>8u detection)

            state.Mines.Add(new MineState
            {
                Position = new Vec2(0f, 5f),
                TriggerRadius = 1.0f,
                ExplosionRadius = 2.5f,
                Damage = 30f,
                Active = true,
                Lifetime = 12f,
                OwnerIndex = 0,
                IsHoming = true,
                DetectionRange = 8f,
                MoveSpeed = 1.5f,
                ActivationDelay = 0f
            });

            float startX = state.Mines[state.Mines.Count - 1].Position.x;
            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.5f);

            for (int i = 0; i < state.Mines.Count; i++)
            {
                if (state.Mines[i].IsHoming && state.Mines[i].OwnerIndex == 0 && state.Mines[i].Active)
                {
                    Assert.AreEqual(startX, state.Mines[i].Position.x, 0.01f,
                        "Mine should not move when no enemy in detection range");
                }
            }
        }

        [Test]
        public void MagneticMine_DoesNotCountAgainstRegularMineLimit()
        {
            var state = CreateState();
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].Energy = 1000f;

            // Place 3 regular mines
            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "mine_layer", Type = SkillType.MineLay,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 0f,
                    Range = 10f, Value = 30f
                },
                new SkillSlotState()
            };
            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.ActivateSkill(state, 0, 0);

            // Switch to magnetic mine
            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "magnetic_mine", Type = SkillType.MagneticMine,
                EnergyCost = 0f, Cooldown = 0f, Duration = 0f,
                Range = 10f, Value = 30f
            };
            SkillSystem.ActivateSkill(state, 0, 0);

            int regularActive = 0;
            int homingActive = 0;
            for (int i = 0; i < state.Mines.Count; i++)
            {
                if (!state.Mines[i].Active || state.Mines[i].OwnerIndex != 0) continue;
                if (state.Mines[i].IsHoming) homingActive++;
                else regularActive++;
            }

            Assert.AreEqual(3, regularActive, "Regular mines should be unaffected");
            Assert.AreEqual(1, homingActive, "Magnetic mine should be placed");
        }
    }
}
