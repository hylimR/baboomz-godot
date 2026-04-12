using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Overcharge skill tests ---

        static SkillSlotState MakeOverchargeSlot()
        {
            return new SkillSlotState
            {
                SkillId = "overcharge", Type = SkillType.Overcharge,
                EnergyCost = 0f, Cooldown = 18f, Duration = 5f,
                Range = 60f, Value = 2f
            };
        }

        [Test]
        public void Overcharge_ExistsInConfig()
        {
            var config = new GameConfig();
            // Overcharge is skill index 16 (after shadow_step at 15)
            Assert.IsTrue(config.Skills.Length >= 17, "Should have at least 17 skills");
            Assert.AreEqual("overcharge", config.Skills[16].SkillId);
            Assert.AreEqual(SkillType.Overcharge, config.Skills[16].Type);
            Assert.AreEqual(0f, config.Skills[16].EnergyCost);
            Assert.AreEqual(18f, config.Skills[16].Cooldown);
            Assert.AreEqual(5f, config.Skills[16].Duration);
            Assert.AreEqual(60f, config.Skills[16].Range); // min-energy gate
            Assert.AreEqual(2f, config.Skills[16].Value);  // damage multiplier
        }

        [Test]
        public void Overcharge_AppliesBuffAndDrainsEnergy()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Players[0].SkillSlots[0] = MakeOverchargeSlot();
            state.Players[0].Energy = 100f;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(0f, state.Players[0].Energy, 0.01f,
                "Overcharge should drain all energy");
            Assert.AreEqual(2f, state.Players[0].DamageMultiplier, 0.01f,
                "Overcharge should set DamageMultiplier to 2x");
            Assert.Greater(state.Players[0].OverchargeTimer, 0f,
                "OverchargeTimer should be active");
            // Cooldown is scaled by player's CooldownMultiplier (issue #31).
            // Seed 42 lands on "Clockwork Foundry" biome which sets DefaultCooldownMultiplier=0.8.
            float expectedCooldown = 18f * state.Players[0].CooldownMultiplier;
            Assert.AreEqual(expectedCooldown, state.Players[0].SkillSlots[0].CooldownRemaining, 0.01f,
                "Cooldown should be active after successful activation (scaled by CooldownMultiplier)");
        }

        [Test]
        public void Overcharge_BelowMinEnergyGate_FailsSilently()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Players[0].SkillSlots[0] = MakeOverchargeSlot();
            state.Players[0].Energy = 50f; // below 60 gate

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(50f, state.Players[0].Energy, 0.01f,
                "Energy should be unchanged when gate fails");
            Assert.AreEqual(0f, state.Players[0].OverchargeTimer, 0.01f,
                "OverchargeTimer should not activate");
            Assert.AreEqual(0f, state.Players[0].SkillSlots[0].CooldownRemaining, 0.01f,
                "Cooldown should NOT trigger on silent failure");
        }

        [Test]
        public void Overcharge_ConsumedOnFire_RevertsMultiplier()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Players[0].SkillSlots[0] = MakeOverchargeSlot();
            state.Players[0].Energy = 100f;
            state.Players[0].AimPower = 20f;
            state.Players[0].AimAngle = 45f;

            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.AreEqual(2f, state.Players[0].DamageMultiplier, 0.01f);

            int before = state.Projectiles.Count;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(before + 1, state.Projectiles.Count,
                "Shot should be fired");
            Assert.AreEqual(0f, state.Players[0].OverchargeTimer, 0.01f,
                "OverchargeTimer should be cleared after firing");
            Assert.AreEqual(config.DefaultDamageMultiplier, state.Players[0].DamageMultiplier, 0.01f,
                "DamageMultiplier should revert to default after the buffed shot");
        }

        [Test]
        public void Overcharge_ProjectileInheritsDoubleDamage()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Players[0].SkillSlots[0] = MakeOverchargeSlot();
            state.Players[0].Energy = 100f;
            state.Players[0].AimPower = 20f;
            state.Players[0].AimAngle = 45f;

            float baseMax = state.Players[0].WeaponSlots[state.Players[0].ActiveWeaponSlot].MaxDamage;

            SkillSystem.ActivateSkill(state, 0, 0);
            GameSimulation.Fire(state, 0);

            var proj = state.Projectiles[state.Projectiles.Count - 1];
            Assert.AreEqual(baseMax * 2f, proj.MaxDamage, 0.01f,
                "Projectile spawned during Overcharge should carry 2x damage");
        }

        [Test]
        public void Overcharge_ExpiresUnused_RevertsMultiplier()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Players[0].SkillSlots[0] = MakeOverchargeSlot();
            state.Players[0].Energy = 100f;

            SkillSystem.ActivateSkill(state, 0, 0);
            Assert.AreEqual(2f, state.Players[0].DamageMultiplier, 0.01f);

            // Tick past 5s duration without firing
            for (int i = 0; i < 350; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0f, state.Players[0].OverchargeTimer, 0.01f,
                "OverchargeTimer should have expired");
            Assert.AreEqual(config.DefaultDamageMultiplier, state.Players[0].DamageMultiplier, 0.01f,
                "Multiplier should revert to default after expiry");
        }

        [Test]
        public void Overcharge_DoesNotStackWithDoubleDamage()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            // Already have DoubleDamage active (2x)
            state.Players[0].DamageMultiplier = 2f;
            state.Players[0].DoubleDamageTimer = 8f;

            state.Players[0].SkillSlots[0] = MakeOverchargeSlot();
            state.Players[0].Energy = 100f;

            SkillSystem.ActivateSkill(state, 0, 0);

            // Both are 2x — should stay at 2x, not stack to 4x
            Assert.AreEqual(2f, state.Players[0].DamageMultiplier, 0.01f,
                "Overcharge should not stack with DoubleDamage (both 2x)");
        }

        [Test]
        public void Overcharge_FireConsume_PreservesDoubleDamageBuff()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Players[0].AimPower = 20f;
            state.Players[0].AimAngle = 45f;

            // DoubleDamage active underneath
            state.Players[0].DamageMultiplier = 2f;
            state.Players[0].DoubleDamageTimer = 8f;

            state.Players[0].SkillSlots[0] = MakeOverchargeSlot();
            state.Players[0].Energy = 100f;
            SkillSystem.ActivateSkill(state, 0, 0);

            GameSimulation.Fire(state, 0);

            Assert.AreEqual(0f, state.Players[0].OverchargeTimer, 0.01f,
                "Overcharge should clear on fire");
            Assert.AreEqual(2f, state.Players[0].DamageMultiplier, 0.01f,
                "DoubleDamage (2x) should remain active after Overcharge consumed");
            Assert.Greater(state.Players[0].DoubleDamageTimer, 0f,
                "DoubleDamageTimer should still be running");
        }

        [Test]
        public void Overcharge_FrozenPlayer_CannotActivate()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Players[0].SkillSlots[0] = MakeOverchargeSlot();
            state.Players[0].Energy = 100f;
            state.Players[0].FreezeTimer = 2f; // frozen

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(100f, state.Players[0].Energy, 0.01f,
                "Frozen player should not drain energy");
            Assert.AreEqual(0f, state.Players[0].OverchargeTimer, 0.01f,
                "Frozen player should not get Overcharge buff");
        }

    }
}
