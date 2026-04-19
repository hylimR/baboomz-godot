using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Freeze mechanic tests ---

        [Test]
        public void Freeze_BlocksMovementAndFiring()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].FreezeTimer = 2f;
            state.Players[0].AimPower = 15f;
            state.Players[0].ShootCooldownRemaining = 0f;

            int projBefore = state.Projectiles.Count;
            GameSimulation.Fire(state, 0);
            Assert.AreEqual(projBefore, state.Projectiles.Count,
                "Frozen player should not be able to fire");
        }

        [Test]
        public void Freeze_ExpiresAfterDuration()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].FreezeTimer = 0.5f;
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.LessOrEqual(state.Players[0].FreezeTimer, 0f,
                "Freeze timer should expire");
        }

        [Test]
        public void Freeze_StopsRopeSwing()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Set up player on rope swing
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].IsGrounded = false;
            state.Players[0].Velocity = new Vec2(5f, 0f); // swinging
            state.Players[0].SkillTargetPosition = new Vec2(0f, 10f); // anchor above

            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "grapple", Type = SkillType.GrapplingHook,
                    IsActive = true, DurationRemaining = 2f,
                    Range = 5f // rope length
                },
                new SkillSlotState()
            };

            Vec2 posBefore = state.Players[0].Position;

            // Freeze the player
            state.Players[0].FreezeTimer = 2f;

            // Tick the skill system
            SkillSystem.Update(state, 0.1f);

            // Velocity should be zeroed, position unchanged
            Assert.AreEqual(0f, state.Players[0].Velocity.x, 0.01f,
                "Frozen player on rope should have zero X velocity");
            Assert.AreEqual(0f, state.Players[0].Velocity.y, 0.01f,
                "Frozen player on rope should have zero Y velocity");
            Assert.AreEqual(posBefore.x, state.Players[0].Position.x, 0.01f,
                "Frozen player on rope should not move");
        }

        [Test]
        public void Freeze_ZeroesHorizontalVelocityOnly()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Give player upward velocity (simulating mid-jump or knockback)
            state.Players[0].Velocity = new Vec2(5f, 10f);
            state.Players[0].IsGrounded = false;
            state.Players[0].FreezeTimer = 2f;
            state.Players[0].IsAI = false;

            // Provide movement input so ProcessInput runs
            state.Input.MoveX = 1f;

            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0f, state.Players[0].Velocity.x, 0.01f,
                "Frozen player should have zero X velocity");
            Assert.Less(state.Players[0].Velocity.y, 10f,
                "Gravity should reduce Y velocity even when frozen");
        }

        [Test]
        public void Freeze_AirbornePlayerFallsWithGravity()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place player high above terrain so they are airborne
            state.Players[0].Position = new Vec2(5f, 20f);
            state.Players[0].Velocity = Vec2.Zero;
            state.Players[0].IsGrounded = false;
            state.Players[0].FreezeTimer = 2f;
            state.Players[0].IsAI = false;
            float startY = state.Players[0].Position.y;

            // Tick several frames
            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Less(state.Players[0].Position.y, startY,
                "Frozen airborne player should fall due to gravity, not hover (#279)");
        }

        [Test]
        public void FreezeGrenade_ProducesSingleExplosionEvent()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Place player 1 within freeze radius
            state.Players[1].Position = new Vec2(1f, 5f);

            Vec2 hitPoint = new Vec2(1f, 5f);
            float radius = 3f;

            // ApplyFreezeExplosion should NOT add its own ExplosionEvent
            CombatResolver.ApplyFreezeExplosion(state, hitPoint, radius, 2f, 0);
            Assert.AreEqual(0, state.ExplosionEvents.Count,
                "ApplyFreezeExplosion should not add an ExplosionEvent (ApplyExplosion handles it)");

            // The follow-up ApplyExplosion adds exactly one
            CombatResolver.ApplyExplosion(state, hitPoint, radius * 0.5f, 5f, 2f, 0, false);
            Assert.AreEqual(1, state.ExplosionEvents.Count,
                "Freeze grenade detonation should produce exactly one ExplosionEvent");
        }

        // --- Freeze Shatter tests (#262) ---

        [Test]
        public void Shatter_FrozenTarget_Takes1_5xDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].Position = new Vec2(1f, 5f);
            state.Players[1].Health = 100f;
            state.Players[1].FreezeTimer = 2f;

            CombatResolver.ApplyExplosion(state, state.Players[1].Position, 3f, 30f, 5f, 0, false);

            float expected = 30f * config.ShatterMultiplier;
            Assert.AreEqual(100f - expected, state.Players[1].Health, 0.5f,
                "Frozen target should take 1.5x damage from shatter");
        }

        [Test]
        public void Shatter_ConsumesFreezeTimer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].Position = new Vec2(1f, 5f);
            state.Players[1].FreezeTimer = 2f;

            CombatResolver.ApplyExplosion(state, state.Players[1].Position, 3f, 30f, 5f, 0, false);

            Assert.AreEqual(0f, state.Players[1].FreezeTimer,
                "Shatter should consume the freeze timer");
        }

        [Test]
        public void Shatter_SelfDamage_DoesNotTrigger()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(1f, 5f);
            state.Players[0].Health = 100f;
            state.Players[0].FreezeTimer = 2f;

            CombatResolver.ApplyExplosion(state, state.Players[0].Position, 3f, 30f, 5f, 0, false);

            Assert.AreEqual(100f - 30f, state.Players[0].Health, 0.5f,
                "Self-damage should NOT trigger shatter bonus");
            Assert.Greater(state.Players[0].FreezeTimer, 0f,
                "Self-damage should not consume freeze timer");
        }

        [Test]
        public void Shatter_DamageEvent_HasIsShatterFlag()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].Position = new Vec2(1f, 5f);
            state.Players[1].FreezeTimer = 2f;

            CombatResolver.ApplyExplosion(state, state.Players[1].Position, 3f, 30f, 5f, 0, false);

            bool foundShatter = false;
            for (int i = 0; i < state.DamageEvents.Count; i++)
                if (state.DamageEvents[i].TargetIndex == 1 && state.DamageEvents[i].IsShatter)
                    foundShatter = true;

            Assert.IsTrue(foundShatter, "DamageEvent should have IsShatter=true for shattered target");
        }

        [Test]
        public void Shatter_PierceDamage_AppliesBonus()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].Position = new Vec2(5f, 5f);
            state.Players[1].Health = 100f;
            state.Players[1].FreezeTimer = 2f;

            CombatResolver.ApplyPierceDamage(state, 1, 20f, 0f, state.Players[1].Position, 0);

            float expected = 20f * config.ShatterMultiplier;
            Assert.AreEqual(100f - expected, state.Players[1].Health, 0.5f,
                "Pierce damage should trigger shatter on frozen target");
            Assert.AreEqual(0f, state.Players[1].FreezeTimer,
                "Pierce shatter should consume freeze timer");
        }

        [Test]
        public void Shatter_SetsFreezeToHitCombo_Issue268()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].Position = new Vec2(1f, 5f);
            state.Players[1].Health = 100f;
            state.Players[1].FreezeTimer = 2f;

            CombatResolver.ApplyExplosion(state, state.Players[1].Position, 3f, 30f, 5f, 0, false);

            Assert.IsTrue(state.Players[0].FreezeToHitCombo,
                "Hitting a frozen target should set FreezeToHitCombo for Freeze Tag challenge");
        }

        // --- Dash skill tests ---

        [Test]
        public void Dash_VelocitySustainsForDuration()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Set up dash skill
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].IsGrounded = true;
            state.Players[0].SkillSlots = new SkillSlotState[]
            {
                new SkillSlotState
                {
                    SkillId = "dash", Type = SkillType.Dash,
                    EnergyCost = 0f, Cooldown = 0f, Duration = 0.2f, Value = 40f
                },
                new SkillSlotState()
            };
            state.Players[0].Energy = 100f;

            float xBefore = state.Players[0].Position.x;
            SkillSystem.ActivateSkill(state, 0, 0);

            // Tick several frames — velocity should be sustained during duration
            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < 12; i++) // 12 * 0.016 = 0.192s < 0.2s duration
                GameSimulation.Tick(state, 0.016f);

            float distMoved = state.Players[0].Position.x - xBefore;
            Assert.Greater(distMoved, 3f,
                "Dash should sustain velocity for its duration, moving player significantly");
        }

    }
}
