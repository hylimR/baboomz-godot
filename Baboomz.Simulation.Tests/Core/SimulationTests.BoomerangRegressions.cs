using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Boomerang apex detection regression tests (issue #259) ---

        [Test]
        public void Boomerang_HorizontalShot_DoesNotReturnImmediately()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Horizontal shot: Velocity.y = 0 (aim angle 0°)
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = new Vec2(15f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true
            });

            // Tick one frame — before the fix, IsReturning would be true here
            ProjectileSimulation.Update(state, 0.016f);

            Assert.IsTrue(state.Projectiles.Count > 0 && state.Projectiles[0].Alive,
                "Boomerang should still be alive after 1 frame");
            Assert.IsFalse(state.Projectiles[0].IsReturning,
                "Horizontal boomerang must NOT return on frame 1");
        }

        [Test]
        public void Boomerang_DownwardShot_DoesNotReturnImmediately()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Downward shot: Velocity.y < 0 (aim angle below horizon)
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 20f),
                Velocity = new Vec2(10f, -3f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true
            });

            // Tick a few frames
            for (int i = 0; i < 5; i++)
                ProjectileSimulation.Update(state, 0.016f);

            if (state.Projectiles.Count > 0 && state.Projectiles[0].Alive)
            {
                Assert.IsFalse(state.Projectiles[0].IsReturning,
                    "Downward boomerang should not return via apex detection");
            }
        }

        [Test]
        public void Boomerang_HorizontalShot_TravelsForward()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            float startX = 0f;
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(startX, 15f),
                Velocity = new Vec2(15f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true
            });

            // Tick 10 frames — boomerang should move rightward
            for (int i = 0; i < 10; i++)
                ProjectileSimulation.Update(state, 0.016f);

            Assert.IsTrue(state.Projectiles.Count > 0, "Boomerang should still exist");
            Assert.Greater(state.Projectiles[0].Position.x, startX + 1f,
                "Horizontal boomerang should travel forward, not snap back");
        }

        [Test]
        public void Boomerang_UpwardShot_StillReturnsAtApex()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Upward shot — should still return after ascending then descending
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = new Vec2(5f, 12f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true
            });

            bool returned = false;
            for (int i = 0; i < 200; i++)
            {
                ProjectileSimulation.Update(state, 0.05f);
                if (state.Projectiles.Count == 0) break;
                if (state.Projectiles[0].IsReturning)
                {
                    returned = true;
                    break;
                }
            }

            Assert.IsTrue(returned,
                "Upward boomerang should still return after apex (HasAscended path)");
        }

        // --- Boomerang owner-death regression (issue #317) ---

        [Test]
        public void Boomerang_DoesNotDespawnWhenOwnerKilledMidFlight()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Place a returning boomerang near (but not at) the dead owner
            state.Players[0].IsDead = true;
            state.Players[0].Position = new Vec2(5f, 10f);

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(6f, 10.5f), // within 1.5 of dead owner
                Velocity = new Vec2(-3f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true,
                IsReturning = true
            });

            // Tick one frame — boomerang is within catch range of dead owner
            ProjectileSimulation.Update(state, 0.05f);

            Assert.IsTrue(state.Projectiles.Count > 0 && state.Projectiles[0].Alive,
                "Boomerang should NOT be caught by dead owner — it should continue flying");
        }

        [Test]
        public void Boomerang_StopsHomingWhenOwnerDies()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Owner alive, boomerang returning — record velocity after one homing tick
            state.Players[0].Position = new Vec2(0f, 10f);
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(10f, 15f),
                Velocity = new Vec2(-2f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true,
                IsReturning = true
            });

            ProjectileSimulation.Update(state, 0.05f);
            float vxHoming = state.Projectiles[0].Velocity.x;

            // Now kill the owner and reset projectile
            state.Players[0].IsDead = true;
            var proj = state.Projectiles[0];
            proj.Position = new Vec2(10f, 15f);
            proj.Velocity = new Vec2(-2f, 0f);
            state.Projectiles[0] = proj;

            ProjectileSimulation.Update(state, 0.05f);
            float vxNoHoming = state.Projectiles[0].Velocity.x;

            // With homing, velocity.x should steer more negative (toward owner at x=0)
            // Without homing (dead owner), velocity.x should be less negative (only gravity/wind)
            Assert.IsTrue(vxHoming < vxNoHoming,
                "Boomerang should not steer toward dead owner — homing should stop");
        }

        [Test]
        public void Boomerang_HorizontalShot_ReturnsAfterMinTravelDistance()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Position = new Vec2(0f, 15f);
            state.Players[0].IsDead = false;

            // Fire horizontally — Velocity.y == 0, so HasAscended will never be set
            state.Projectiles.Add(new ProjectileState
            {
                Position = new Vec2(0f, 15f),
                Velocity = new Vec2(15f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true
            });

            // Tick until IsReturning becomes true
            bool returned = false;
            for (int i = 0; i < 200; i++)
            {
                ProjectileSimulation.Update(state, 0.016f);
                if (state.Projectiles.Count == 0) break;
                if (state.Projectiles[0].IsReturning)
                {
                    returned = true;
                    break;
                }
            }

            Assert.IsTrue(returned,
                "Boomerang fired horizontally should return via min-travel-distance fallback");
        }

        [Test]
        public void Boomerang_DownwardShot_ReturnsAfterMinTravelDistance()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].Position = new Vec2(0f, 20f);
            state.Players[0].IsDead = false;

            // Fire slightly downward — Velocity.y < 0, so HasAscended will never be set
            state.Projectiles.Add(new ProjectileState
            {
                Position = new Vec2(0f, 20f),
                Velocity = new Vec2(12f, -3f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 4f,
                Alive = true,
                IsBoomerang = true
            });

            bool returned = false;
            for (int i = 0; i < 200; i++)
            {
                ProjectileSimulation.Update(state, 0.016f);
                if (state.Projectiles.Count == 0) break;
                if (state.Projectiles[0].IsReturning)
                {
                    returned = true;
                    break;
                }
            }

            Assert.IsTrue(returned,
                "Boomerang fired downward should return via min-travel-distance fallback");
        }

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
