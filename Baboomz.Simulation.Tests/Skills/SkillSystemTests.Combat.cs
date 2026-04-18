using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class SkillSystemTests
    {
        // --- Input integration tests ---

        [Test]
        public void Skill1Input_ActivatesSlot0()
        {
            var state = CreateState();
            state.Input.Skill1Pressed = true;
            Vec2 posBefore = state.Players[0].Position;

            // ProcessInput is internal, test through full Tick
            GameSimulation.Tick(state, 0.016f);

            // Teleport should have moved the player
            Assert.That(System.Math.Abs(state.Players[0].Position.x - posBefore.x) > 0.5f,
                "Teleport should have moved the player");
        }

        [Test]
        public void Skill2Input_ActivatesSlot1()
        {
            var state = CreateState();
            state.Input.Skill2Pressed = true;

            GameSimulation.Tick(state, 0.016f);

            // Dash should have applied velocity
            // (checking cooldown as proxy — skill was used)
            Assert.Greater(state.Players[0].SkillSlots[1].CooldownRemaining, 0f);
        }

        // --- Multi-skill tests ---

        [Test]
        public void MultipleSkills_IndependentCooldowns()
        {
            var state = CreateState();

            // Activate both skills
            SkillSystem.ActivateSkill(state, 0, 0); // teleport
            SkillSystem.ActivateSkill(state, 0, 1); // dash

            // Both should be on cooldown
            Assert.Greater(state.Players[0].SkillSlots[0].CooldownRemaining, 0f);
            Assert.Greater(state.Players[0].SkillSlots[1].CooldownRemaining, 0f);

            // Cooldowns are independent (different values)
            float cd0 = state.Players[0].SkillSlots[0].CooldownRemaining;
            float cd1 = state.Players[0].SkillSlots[1].CooldownRemaining;
            Assert.That(System.Math.Abs(cd0 - cd1) > 0.01f,
                "Cooldowns should be different values");
        }

        [Test]
        public void CreateMatch_InitializesSkillSlots()
        {
            var state = CreateState();

            Assert.IsNotNull(state.Players[0].SkillSlots);
            Assert.AreEqual(2, state.Players[0].SkillSlots.Length);
            Assert.IsNotNull(state.Players[0].SkillSlots[0].SkillId);
            Assert.IsNotNull(state.Players[0].SkillSlots[1].SkillId);
        }

        [Test]
        public void CreateMatch_DefaultSkills_AreCorrect()
        {
            var state = CreateState();

            // Default: slot 0 = teleport (index 0), slot 1 = dash (index 3)
            Assert.AreEqual(SkillType.Teleport, state.Players[0].SkillSlots[0].Type);
            Assert.AreEqual(SkillType.Dash, state.Players[0].SkillSlots[1].Type);
        }

        [Test]
        public void SkillDuringCharge_StillWorks()
        {
            var state = CreateState();
            state.Players[0].IsCharging = true;
            Vec2 posBefore = state.Players[0].Position;

            SkillSystem.ActivateSkill(state, 0, 0); // teleport

            // Teleport should still work while charging
            Assert.That(System.Math.Abs(state.Players[0].Position.x - posBefore.x) > 0.5f,
                "Teleport should have moved the player");
        }

        [Test]
        public void AlreadyActive_Blocked()
        {
            var state = CreateState();
            // Use dash (duration-based)
            SkillSystem.ActivateSkill(state, 0, 1);
            Assert.IsTrue(state.Players[0].SkillSlots[1].IsActive);

            float energyAfterFirst = state.Players[0].Energy;

            // Reset cooldown so only IsActive blocks
            state.Players[0].SkillSlots[1].CooldownRemaining = 0f;

            SkillSystem.ActivateSkill(state, 0, 1);

            // Energy unchanged — blocked by IsActive
            Assert.AreEqual(energyAfterFirst, state.Players[0].Energy, 0.01f);
        }

        // --- Deflect tests ---

        [Test]
        public void Deflect_SetsDeflectTimerAndIsActive()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[12]); // deflect

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsTrue(state.Players[0].SkillSlots[0].IsActive);
            Assert.Greater(state.Players[0].DeflectTimer, 0f);
        }

        [Test]
        public void Deflect_ExpiresAfterDuration()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[12]);

            SkillSystem.ActivateSkill(state, 0, 0);

            SkillSystem.Update(state, 2f); // past 1s duration

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive);
            Assert.AreEqual(0f, state.Players[0].DeflectTimer, 0.01f);
        }

        [Test]
        public void Deflect_ReflectsProjectileInRange()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[12]);

            // Place a projectile near player 0 owned by player 1
            state.Projectiles.Add(new ProjectileState
            {
                Alive = true,
                Position = state.Players[0].Position + new Vec2(1f, 0f),
                Velocity = new Vec2(-10f, 0f),
                OwnerIndex = 1,
                MaxDamage = 30f,
                ExplosionRadius = 2f
            });

            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.Update(state, 0.016f);

            // Projectile ownership should flip to deflector (player 0)
            Assert.AreEqual(0, state.Projectiles[0].OwnerIndex,
                "Deflected projectile should belong to the deflector");
        }

        [Test]
        public void Deflect_AimsAtOriginalShooter()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[12]);

            // Player 1 is to the right of player 0
            // Projectile is near player 0, moving left (toward player 0)
            state.Projectiles.Add(new ProjectileState
            {
                Alive = true,
                Position = state.Players[0].Position + new Vec2(1f, 0f),
                Velocity = new Vec2(-10f, 0f),
                OwnerIndex = 1,
                MaxDamage = 30f,
                ExplosionRadius = 2f
            });

            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.Update(state, 0.016f);

            // Velocity should now point toward player 1 (positive x direction)
            Assert.Greater(state.Projectiles[0].Velocity.x, 0f,
                "Deflected projectile should aim toward original shooter");
        }

        [Test]
        public void Deflect_IgnoresProjectilesOutsideRange()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[12]);

            // Place a projectile far away from player 0
            state.Projectiles.Add(new ProjectileState
            {
                Alive = true,
                Position = state.Players[0].Position + new Vec2(20f, 0f),
                Velocity = new Vec2(-10f, 0f),
                OwnerIndex = 1,
                MaxDamage = 30f,
                ExplosionRadius = 2f
            });

            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.Update(state, 0.016f);

            // Projectile should still belong to player 1
            Assert.AreEqual(1, state.Projectiles[0].OwnerIndex,
                "Projectile outside range should not be deflected");
        }

        [Test]
        public void Deflect_IgnoresStuckStickyBombs()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[12]);

            // Sticky bomb stuck to player
            state.Projectiles.Add(new ProjectileState
            {
                Alive = true,
                Position = state.Players[0].Position + new Vec2(0.5f, 0f),
                Velocity = Vec2.Zero,
                OwnerIndex = 1,
                MaxDamage = 50f,
                ExplosionRadius = 2f,
                IsSticky = true,
                StuckToPlayerId = 0
            });

            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.Update(state, 0.016f);

            Assert.AreEqual(1, state.Projectiles[0].OwnerIndex,
                "Stuck sticky bomb should not be deflectable");
        }

        [Test]
        public void Deflect_MultipleProjectiles_AllReflected()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[12]);

            // Add two projectiles in range
            for (int i = 0; i < 2; i++)
            {
                state.Projectiles.Add(new ProjectileState
                {
                    Alive = true,
                    Position = state.Players[0].Position + new Vec2(1f, i * 0.5f),
                    Velocity = new Vec2(-10f, 0f),
                    OwnerIndex = 1,
                    MaxDamage = 30f,
                    ExplosionRadius = 2f
                });
            }

            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.Update(state, 0.016f);

            Assert.AreEqual(0, state.Projectiles[0].OwnerIndex);
            Assert.AreEqual(0, state.Projectiles[1].OwnerIndex);
        }

        [Test]
        public void Deflect_IgnoresOwnProjectiles()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[12]);

            // Player 0 fires a projectile that is still near them
            var originalVelocity = new Vec2(10f, 5f);
            state.Projectiles.Add(new ProjectileState
            {
                Alive = true,
                Position = state.Players[0].Position + new Vec2(1f, 0f),
                Velocity = originalVelocity,
                OwnerIndex = 0, // owned by the deflector
                MaxDamage = 30f,
                ExplosionRadius = 2f
            });

            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.Update(state, 0.016f);

            // Projectile should remain owned by player 0 with unchanged direction
            Assert.AreEqual(0, state.Projectiles[0].OwnerIndex,
                "Own projectile ownership should not change");
            Assert.Greater(state.Projectiles[0].Velocity.x, 0f,
                "Own projectile should not be redirected back at self");
        }

        [Test]
        public void Deflect_MineProjectile_NoCrash()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[12]); // deflect

            // Mine projectile with OwnerIndex = -1 (environment mine)
            state.Projectiles.Add(new ProjectileState
            {
                Alive = true,
                Position = state.Players[0].Position + new Vec2(1f, 0f),
                Velocity = new Vec2(-5f, 0f),
                OwnerIndex = -1,
                MaxDamage = 40f,
                ExplosionRadius = 3f
            });

            SkillSystem.ActivateSkill(state, 0, 0);

            // Should not throw IndexOutOfRangeException
            Assert.DoesNotThrow(() => SkillSystem.Update(state, 0.016f));

            // Projectile should be deflected (ownership flipped to deflector)
            Assert.AreEqual(0, state.Projectiles[0].OwnerIndex,
                "Mine projectile should be claimed by the deflector");

            // Velocity should be reversed (no owner to aim at)
            Assert.Greater(state.Projectiles[0].Velocity.x, 0f,
                "Mine projectile velocity should be reversed on deflect");
        }

    }
}
