using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class SkillSystemTests
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

        // --- Teleport tests ---

        [Test]
        public void Teleport_MovesPlayer_InAimDirection()
        {
            var state = CreateState();
            // Ensure slot 0 is teleport
            Assert.AreEqual(SkillType.Teleport, state.Players[0].SkillSlots[0].Type);

            Vec2 posBefore = state.Players[0].Position;
            state.Players[0].AimAngle = 0f;
            state.Players[0].FacingDirection = 1;

            SkillSystem.ActivateSkill(state, 0, 0);

            // Player should have moved to the right
            Assert.Greater(state.Players[0].Position.x, posBefore.x);
        }

        [Test]
        public void Teleport_MaxRange_Respected()
        {
            var state = CreateState();
            Vec2 posBefore = state.Players[0].Position;
            state.Players[0].AimAngle = 0f;
            state.Players[0].FacingDirection = 1;
            float range = state.Players[0].SkillSlots[0].Range;

            SkillSystem.ActivateSkill(state, 0, 0);

            float moved = state.Players[0].Position.x - posBefore.x;
            Assert.LessOrEqual(moved, range + 1f); // small tolerance for terrain resolution
        }

        [Test]
        public void Teleport_IsInstant_NoDuration()
        {
            var state = CreateState();

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive);
        }

        // --- Shield tests ---

        [Test]
        public void Shield_IncreasesArmorMultiplier()
        {
            var state = CreateState();
            // Set slot 1 to shield
            SetSkillSlot(ref state.Players[0].SkillSlots[1],
                state.Config.Skills[2]); // shield is index 2

            SkillSystem.ActivateSkill(state, 0, 1);

            Assert.AreEqual(3f, state.Players[0].ArmorMultiplier, 0.01f);
        }

        [Test]
        public void Shield_ExpiresAfterDuration()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[1],
                state.Config.Skills[2]);

            SkillSystem.ActivateSkill(state, 0, 1);
            Assert.IsTrue(state.Players[0].SkillSlots[1].IsActive);

            // Tick past shield duration (3s)
            SkillSystem.Update(state, 4f);

            Assert.IsFalse(state.Players[0].SkillSlots[1].IsActive);
        }

        [Test]
        public void Shield_ArmorReverts_OnExpiry()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[1],
                state.Config.Skills[2]);

            SkillSystem.ActivateSkill(state, 0, 1);

            // Tick past shield duration
            SkillSystem.Update(state, 4f);

            Assert.AreEqual(state.Config.DefaultArmorMultiplier,
                state.Players[0].ArmorMultiplier, 0.01f);
        }

        [Test]
        public void Shield_RestoresNonDefaultArmor_OnExpiry()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[1],
                state.Config.Skills[2]); // shield

            // Simulate boss/campaign bonus armor
            state.Players[0].ArmorMultiplier = 2f;

            SkillSystem.ActivateSkill(state, 0, 1);
            Assert.AreEqual(3f, state.Players[0].ArmorMultiplier, 0.01f,
                "Shield should set armor to skill value");

            // Tick past shield duration
            SkillSystem.Update(state, 4f);

            Assert.AreEqual(2f, state.Players[0].ArmorMultiplier, 0.01f,
                "Shield expiry must restore pre-shield armor, not default");
        }

        // --- Dash tests ---

        [Test]
        public void Dash_AppliesHorizontalVelocity()
        {
            var state = CreateState();
            // Default slot 1 is dash (index 3)
            Assert.AreEqual(SkillType.Dash, state.Players[0].SkillSlots[1].Type);

            state.Players[0].FacingDirection = 1;

            SkillSystem.ActivateSkill(state, 0, 1);

            Assert.Greater(state.Players[0].Velocity.x, 0f);
        }

        [Test]
        public void Dash_RespectsFacingDirection()
        {
            var state = CreateState();
            state.Players[0].FacingDirection = -1;

            SkillSystem.ActivateSkill(state, 0, 1);

            Assert.Less(state.Players[0].Velocity.x, 0f);
        }

        // --- Heal tests ---

        [Test]
        public void Heal_RestoresHP()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[4]); // heal is index 4
            state.Players[0].Health = 50f;

            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.Update(state, 1f);

            Assert.Greater(state.Players[0].Health, 50f);
        }

        [Test]
        public void Heal_CapsAtMaxHealth()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[4]);
            state.Players[0].Health = 95f;

            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.Update(state, 5f); // tick well past duration

            Assert.AreEqual(state.Players[0].MaxHealth,
                state.Players[0].Health, 0.01f);
        }

        // --- Jetpack tests ---

        [Test]
        public void Jetpack_AppliesUpwardForce()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[5]); // jetpack is index 5

            SkillSystem.ActivateSkill(state, 0, 0);
            SkillSystem.Update(state, 0.1f);

            Assert.Greater(state.Players[0].Velocity.y, 0f);
        }

        [Test]
        public void Jetpack_Expires()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[5]);

            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].SkillSlots[0].IsActive);

            SkillSystem.Update(state, 3f); // past 2s duration

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive);
        }

        // --- Grappling Hook tests ---

        [Test]
        public void GrapplingHook_NoTerrainHit_Refunds()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[1]); // grapple is index 1

            // Aim straight up where there's no terrain
            state.Players[0].AimAngle = 90f;
            float energyBefore = state.Players[0].Energy;

            SkillSystem.ActivateSkill(state, 0, 0);

            // Energy should be refunded (grapple missed)
            Assert.AreEqual(energyBefore, state.Players[0].Energy, 0.01f);
        }

        [Test]
        public void GrapplingHook_RangePreservedAfterUse()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[1]); // grapple is index 1

            float originalRange = state.Players[0].SkillSlots[0].Range;
            Assert.Greater(originalRange, 0f);

            // Aim toward terrain (angle 0 = forward) — will hit nearby terrain
            state.Players[0].AimAngle = 0f;
            state.Players[0].Energy = 100f;

            SkillSystem.ActivateSkill(state, 0, 0);

            // If grapple connected, the rope length is stored on PlayerState
            if (state.Players[0].SkillSlots[0].IsActive)
            {
                Assert.Greater(state.Players[0].RopeLength, 0f,
                    "RopeLength should be set on player state");

                // skill.Range must NOT have been mutated
                Assert.AreEqual(originalRange, state.Players[0].SkillSlots[0].Range, 0.01f,
                    "skill.Range must not be overwritten by rope length");

                // Deactivate by expiring duration
                state.Players[0].SkillSlots[0].DurationRemaining = 0f;
                SkillSystem.Update(state, 0.016f);

                // Range still preserved for next activation
                Assert.AreEqual(originalRange, state.Players[0].SkillSlots[0].Range, 0.01f,
                    "skill.Range must be preserved after grapple deactivation");
            }
        }

        // --- Rope swing double-gravity regression (#359) ---

        [Test]
        public void RopeSwing_DoesNotApplyGravityTwice()
        {
            // Manually construct a player mid-swing (GrapplingHook active, airborne)
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;
            var state = GameSimulation.CreateMatch(config, 42);

            // Disable AI so it doesn't fire projectiles that add knockback to player 0
            state.Players[1].IsAI = false;
            state.Players[1].IsDead = true; // Kill AI to prevent any interference

            ref PlayerState p = ref state.Players[0];
            SetSkillSlot(ref p.SkillSlots[0], FindSkill(state.Config, SkillType.GrapplingHook));

            // Move player high above terrain to avoid collision effects
            p.Position = new Vec2(0f, 15f);
            p.IsGrounded = false;
            p.Velocity = new Vec2(0f, 0f);

            // Manually activate the skill slot (bypass terrain hit requirement)
            p.SkillSlots[0].IsActive = true;
            p.SkillSlots[0].DurationRemaining = 2f;
            // Anchor point directly above — at angle 0, pendulum gravity is sin(0) = 0
            p.SkillTargetPosition = new Vec2(p.Position.x, p.Position.y + 8f);
            p.RopeLength = 8f;

            float dt = 0.016f;
            float gravity = state.Config.Gravity;

            // Tick once — SkillSystem.Update runs UpdateRopeSwing, then UpdatePlayer runs.
            // If double-gravity bug is present, velocity.y will decrease by 2 × gravity × dt.
            // With the fix, gravity is NOT applied in UpdatePlayer when swinging.
            GameSimulation.Tick(state, dt);

            // UpdateRopeSwing sets velocity from pendulum tangential motion.
            // At angle=0 (anchor directly above) the gravity term is sin(0)=0,
            // so velocity after one tick should remain near-zero in y (not gravity × dt downward).
            float maxExpectedGravityContribution = gravity * dt * 1.5f; // 50% margin
            Assert.Less(MathF.Abs(state.Players[0].Velocity.y), maxExpectedGravityContribution,
                "While rope-swinging, UpdatePlayer must not add an extra gravity term to velocity");
        }

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

        // --- Helper ---

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

        // --- HookShot tests (#198) ---

        [Test]
        public void HookShot_PullsTargetTowardCaster()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]); // hookshot
            state.Players[0].Energy = 100f;
            // Place target within hookshot range (10 units)
            state.Players[1].Position = state.Players[0].Position + new Vec2(8f, 0f);

            Vec2 targetPosBefore = state.Players[1].Position;
            Vec2 casterPos = state.Players[0].Position;

            SkillSystem.ActivateSkill(state, 0, 0);

            // Target should be closer to caster than before
            float distBefore = Vec2.Distance(casterPos, targetPosBefore);
            float distAfter = Vec2.Distance(casterPos, state.Players[1].Position);
            Assert.Less(distAfter, distBefore, "Target should be pulled closer to caster");
        }

        [Test]
        public void HookShot_DealsDamage()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            state.Players[1].Position = state.Players[0].Position + new Vec2(8f, 0f);
            float hpBefore = state.Players[1].Health;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.Less(state.Players[1].Health, hpBefore, "Target should take damage");
            Assert.AreEqual(1, state.DamageEvents.Count, "Should emit a DamageEvent");
            Assert.AreEqual(1, state.DamageEvents[0].TargetIndex);
        }

        [Test]
        public void HookShot_NoTarget_RefundsEnergy()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;

            // Kill the only target
            state.Players[1].IsDead = true;

            float energyBefore = state.Players[0].Energy;
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(energyBefore, state.Players[0].Energy, 0.01f,
                "Energy should be refunded when no valid target");
            Assert.AreEqual(0, state.SkillEvents.Count,
                "No skill event should be emitted on refund");
        }

        [Test]
        public void HookShot_SkipsTeammates()
        {
            var state = CreateState();
            state.Config.TeamMode = true;
            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 0; // same team

            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            float energyBefore = state.Players[0].Energy;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(energyBefore, state.Players[0].Energy, 0.01f,
                "Energy should be refunded when only target is a teammate");
        }

        [Test]
        public void HookShot_SkipsFrozenTargets()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            state.Players[1].FreezeTimer = 5f; // frozen target

            float energyBefore = state.Players[0].Energy;
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(energyBefore, state.Players[0].Energy, 0.01f,
                "Energy should be refunded when only target is frozen");
        }

        [Test]
        public void HookShot_IsInstant()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.IsFalse(state.Players[0].SkillSlots[0].IsActive,
                "HookShot should be instant (no duration)");
        }

        [Test]
        public void HookShot_EmitsSkillEvent()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            state.Players[1].Position = state.Players[0].Position + new Vec2(8f, 0f);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(1, state.SkillEvents.Count);
            Assert.AreEqual(SkillType.HookShot, state.SkillEvents[0].Type);
        }

        [Test]
        public void HookShot_TracksDamageStats()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            state.Players[1].Position = state.Players[0].Position + new Vec2(8f, 0f);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.Greater(state.Players[0].TotalDamageDealt, 0f);
            Assert.AreEqual(1, state.Players[0].DirectHits);
        }

        [Test]
        public void HookShot_TargetGetsUpwardVelocity()
        {
            var state = CreateState();
            SetSkillSlot(ref state.Players[0].SkillSlots[0],
                state.Config.Skills[14]);
            state.Players[0].Energy = 100f;
            state.Players[1].Position = state.Players[0].Position + new Vec2(8f, 0f);

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.Greater(state.Players[1].Velocity.y, 0f,
                "Target should receive upward velocity after pull");
        }

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

        static SkillDef FindSkill(GameConfig config, SkillType type)
        {
            for (int i = 0; i < config.Skills.Length; i++)
                if (config.Skills[i].Type == type) return config.Skills[i];
            throw new System.Exception("Skill not found: " + type);
        }

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

            // Carve a hole 8 world units to the right of the player (within range 12)
            Vec2 target = state.Players[0].Position + new Vec2(12f, 0f); // matches ExecuteMend (clamped range)
            int cx = state.Terrain.WorldToPixelX(target.x);
            int cy = state.Terrain.WorldToPixelY(target.y);
            // Fill a patch solid first so there's something to blow up
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

            Vec2 target = state.Players[0].Position + new Vec2(12f, 0f); // matches ExecuteMend (clamped range)
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

            // Put player 1 exactly where the mend target will land
            Vec2 target = state.Players[0].Position + new Vec2(12f, 0f); // matches ExecuteMend (clamped range)
            state.Players[1].Position = target;
            state.Players[1].IsDead = false;

            int cx = state.Terrain.WorldToPixelX(target.x);
            int cy = state.Terrain.WorldToPixelY(target.y + 0.5f); // inside player bbox
            // Ensure the pixel under player 1 is empty so we would otherwise fill it
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
            // Place target close enough to be in range
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

            // Activate Decoy
            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.IsTrue(state.Players[0].SkillSlots[0].IsActive, "Decoy should be active after activation");

            // Simulate damage while decoy is active
            state.DamageEvents.Add(new DamageEvent { TargetIndex = 0, Amount = 10f });

            // Tick — this should trigger early reveal and deactivate the skill slot
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
            // Put a second skill (Heal) in slot 1
            SetSkillSlot(ref state.Players[0].SkillSlots[1],
                FindSkill(state.Config, SkillType.Heal));
            state.Players[0].Energy = 100f;

            // Activate Decoy (slot 0)
            SkillSystem.ActivateSkill(state, 0, 0);

            // Damage player — triggers early reveal
            state.DamageEvents.Add(new DamageEvent { TargetIndex = 0, Amount = 5f });
            SkillSystem.Update(state, 0.016f);

            // Now slot 0 is deactivated; slot 1 should be activatable
            float cooldown1Before = state.Players[0].SkillSlots[1].CooldownRemaining;
            SkillSystem.ActivateSkill(state, 0, 1);

            Assert.Greater(state.Players[0].SkillSlots[1].CooldownRemaining, cooldown1Before,
                "After Decoy early reveal, other skill slots should be activatable again");
        }

        // --- Fall damage regression tests (#372) ---

        [Test]
        public void Teleport_ResetsLastGroundedY_WhenAirborne()
        {
            // Regression: teleporting to an airborne position should reset LastGroundedY
            // so fall damage is measured from the teleport destination, not the pre-teleport position.
            var state = CreateState();
            ref PlayerState p = ref state.Players[0];
            Assert.AreEqual(SkillType.Teleport, p.SkillSlots[0].Type);

            // Simulate a player grounded at a high Y
            p.LastGroundedY = 20f;
            p.IsGrounded = true;
            // Aim straight right so they teleport to a lower position with no ground
            p.AimAngle = 0f;
            p.FacingDirection = 1;

            SkillSystem.ActivateSkill(state, 0, 0);

            // LastGroundedY must now equal the teleport destination Y, not 20
            Assert.AreEqual(p.Position.y, p.LastGroundedY, 0.01f,
                "Teleport must reset LastGroundedY to the new position to prevent inflated fall damage");
        }

        [Test]
        public void ShadowStep_ResetsLastGroundedY_OnRecall_WhenAirborne()
        {
            // Regression: ShadowStep recall should reset LastGroundedY to the recall destination
            // even when the destination has no ground (terrain destroyed), not the mark Y.
            var state = CreateState();
            ref PlayerState p = ref state.Players[0];
            SetSkillSlot(ref p.SkillSlots[0], FindSkill(state.Config, SkillType.ShadowStep));
            state.Players[0].Energy = 100f;

            // Mark position at a high Y
            p.LastGroundedY = 20f;
            p.IsGrounded = true;

            // Activate ShadowStep to record current position as recall point
            SkillSystem.ActivateSkill(state, 0, 0);
            float markedY = p.SkillTargetPosition.y;

            // Move player high (simulate flight), then force recall
            p.Position = new Vec2(p.Position.x, markedY + 10f);
            p.LastGroundedY = markedY + 10f;
            p.IsGrounded = false;

            // Force deactivation (recall)
            state.Players[0].SkillSlots[0].DurationRemaining = 0f;
            SkillSystem.Update(state, 0.016f);

            // LastGroundedY must equal the recall destination Y, not pre-ShadowStep Y
            Assert.AreEqual(p.Position.y, p.LastGroundedY, 0.01f,
                "ShadowStep recall must reset LastGroundedY to prevent inflated fall damage");
        }

        [Test]
        public void ShadowStep_RecallSkillEvent_Position_IsPreRecallPosition()
        {
            // Regression #464: SkillEvent.Position must be where the player came FROM,
            // not the mark (TargetPosition). Before fix both fields were equal to the mark.
            var state = CreateState();
            ref PlayerState p = ref state.Players[0];
            SetSkillSlot(ref p.SkillSlots[0], FindSkill(state.Config, SkillType.ShadowStep));
            state.Players[0].Energy = 100f;
            p.IsGrounded = true;

            // Activate ShadowStep — records current position as the recall mark
            Vec2 markPos = p.Position;
            SkillSystem.ActivateSkill(state, 0, 0);

            // Move player far away before recall fires
            Vec2 preRecallPos = new Vec2(markPos.x + 8f, markPos.y + 5f);
            p.Position = preRecallPos;
            p.IsGrounded = false;

            // Force deactivation (recall)
            state.SkillEvents.Clear();
            state.Players[0].SkillSlots[0].DurationRemaining = 0f;
            SkillSystem.Update(state, 0.016f);

            Assert.AreEqual(1, state.SkillEvents.Count, "Recall must emit exactly one SkillEvent");
            var ev = state.SkillEvents[0];
            Assert.AreEqual(SkillType.ShadowStep, ev.Type);

            // Position = where the player was before recall (not the mark)
            Assert.AreEqual(preRecallPos.x, ev.Position.x, 0.01f,
                "SkillEvent.Position must be the pre-recall position (from), not the mark");
            Assert.AreEqual(preRecallPos.y, ev.Position.y, 0.01f,
                "SkillEvent.Position must be the pre-recall position (from), not the mark");

            // TargetPosition = the mark (where they arrived)
            Assert.AreNotEqual(ev.Position.x, ev.TargetPosition.x,
                "Position and TargetPosition must differ — player moved away from the mark");
        }
    }
}
