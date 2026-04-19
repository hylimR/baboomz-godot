using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Earthquake skill tests ---

        [Test]
        public void Earthquake_DamagesGroundedPlayers()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "earthquake",
                Type = SkillType.Earthquake,
                EnergyCost = 35f,
                Cooldown = 20f,
                Value = 20f
            };
            state.Players[0].Energy = 100f;

            // Ensure target is grounded
            state.Players[1].IsGrounded = true;
            float hpBefore = state.Players[1].Health;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.Less(state.Players[1].Health, hpBefore,
                "Earthquake should damage grounded players");
        }

        [Test]
        public void Earthquake_DoesNotDamageAirborne()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "earthquake",
                Type = SkillType.Earthquake,
                EnergyCost = 35f,
                Cooldown = 20f,
                Value = 20f
            };
            state.Players[0].Energy = 100f;
            state.Players[1].IsGrounded = false;
            float hpBefore = state.Players[1].Health;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(hpBefore, state.Players[1].Health,
                "Earthquake should not damage airborne players");
        }

        [Test]
        public void Earthquake_SkipsTeammatesInTeamMode()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.TeamMode = true;
            var state = GameSimulation.CreateMatch(config, 42);

            // Assign teams: player 0 and 1 on same team
            state.Players[0].TeamIndex = 0;
            state.Players[1].TeamIndex = 0;

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "earthquake", Type = SkillType.Earthquake,
                EnergyCost = 0f, Cooldown = 0f, Value = 20f
            };
            state.Players[0].Energy = 100f;
            state.Players[1].IsGrounded = true;

            float hpBefore = state.Players[1].Health;
            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(hpBefore, state.Players[1].Health, 0.01f,
                "Earthquake should not damage teammates in team mode");
        }

        [Test]
        public void Earthquake_RespectsArmorMultiplier()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "earthquake", Type = SkillType.Earthquake,
                EnergyCost = 0f, Cooldown = 0f, Value = 20f
            };
            state.Players[0].Energy = 100f;

            // Give target Shield-level armor (3x reduction → takes 1/3 damage)
            state.Players[1].IsGrounded = true;
            state.Players[1].ArmorMultiplier = 3f;
            float hpBefore = state.Players[1].Health;

            SkillSystem.ActivateSkill(state, 0, 0);

            float expected = hpBefore - 20f * (1f / 3f);
            Assert.AreEqual(expected, state.Players[1].Health, 0.1f,
                "Earthquake should apply ArmorMultiplier damage reduction");
        }

        [Test]
        public void Earthquake_AppliesCasterDamageMultiplier()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "earthquake", Type = SkillType.Earthquake,
                EnergyCost = 0f, Cooldown = 0f, Value = 20f
            };
            state.Players[0].Energy = 100f;
            state.Players[0].DamageMultiplier = 2f; // DoubleDamage

            state.Players[1].IsGrounded = true;
            float hpBefore = state.Players[1].Health;

            SkillSystem.ActivateSkill(state, 0, 0);

            float expected = hpBefore - 20f * 2f;
            Assert.AreEqual(expected, state.Players[1].Health, 0.1f,
                "Earthquake should apply caster's DamageMultiplier (e.g. DoubleDamage)");
        }

        [Test]
        public void Earthquake_TracksDamageInPostMatchStats()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "earthquake", Type = SkillType.Earthquake,
                EnergyCost = 0f, Cooldown = 0f, Value = 20f
            };
            state.Players[0].Energy = 100f;
            state.Players[0].TotalDamageDealt = 0f;
            state.Players[0].DirectHits = 0;
            state.Players[0].MaxSingleDamage = 0f;

            state.Players[1].IsGrounded = true;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.Greater(state.Players[0].TotalDamageDealt, 0f,
                "Earthquake damage should be tracked in TotalDamageDealt");
            Assert.Greater(state.Players[0].DirectHits, 0,
                "Earthquake hits should be tracked in DirectHits");
            Assert.Greater(state.Players[0].MaxSingleDamage, 0f,
                "Earthquake damage should update MaxSingleDamage");
        }

        [Test]
        public void Earthquake_SetsLastDamagedByIndex()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "earthquake", Type = SkillType.Earthquake,
                EnergyCost = 0f, Cooldown = 0f, Value = 20f
            };
            state.Players[0].Energy = 100f;
            state.Players[1].IsGrounded = true;
            state.Players[1].LastDamagedByIndex = -1;

            SkillSystem.ActivateSkill(state, 0, 0);

            Assert.AreEqual(0, state.Players[1].LastDamagedByIndex,
                "Earthquake should set LastDamagedByIndex for knockback kill attribution");
            Assert.AreEqual(5f, state.Players[1].LastDamagedByTimer, 0.01f,
                "Earthquake should set LastDamagedByTimer grace window");
        }

        [Test]
        public void Earthquake_ZeroArmorMultiplier_DoesNotCauseInfinityDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].SkillSlots[0] = new SkillSlotState
            {
                SkillId = "earthquake", Type = SkillType.Earthquake,
                EnergyCost = 0f, Cooldown = 0f, Value = 20f
            };
            state.Players[0].Energy = 100f;

            // Set target ArmorMultiplier to 0 — should NOT cause Infinity damage
            state.Players[1].IsGrounded = true;
            state.Players[1].ArmorMultiplier = 0f;
            float hpBefore = state.Players[1].Health;

            SkillSystem.ActivateSkill(state, 0, 0);

            float damage = hpBefore - state.Players[1].Health;
            Assert.IsFalse(float.IsInfinity(damage),
                "Earthquake with ArmorMultiplier=0 should not produce Infinity damage");
            Assert.IsFalse(float.IsNaN(damage),
                "Earthquake with ArmorMultiplier=0 should not produce NaN damage");
            Assert.Greater(damage, 0f,
                "Earthquake should still deal positive damage when ArmorMultiplier=0");
        }

        [Test]
        public void Explosion_ShieldDoesNotAbsorbOverheadHit()
        {
            // Regression: #340 — IsFrontalHit used >= and <= so dx=0 (overhead/below)
            // was treated as frontal for BOTH facing directions, letting a horizontal
            // shield absorb a bomb falling directly overhead.
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].Position = new Vec2(5f, 5f);
            state.Players[1].FacingDirection = 1;
            state.Players[1].ShieldHP = 500f;
            state.Players[1].MaxShieldHP = 500f;
            float hpBefore = state.Players[1].Health;
            float shieldBefore = state.Players[1].ShieldHP;

            // Explosion at exact same X as the player (overhead / same column)
            Vec2 overheadPos = new Vec2(state.Players[1].Position.x, state.Players[1].Position.y + 2f);
            CombatResolver.ApplyExplosion(state, overheadPos, 5f, 50f, 5f, 0, false);

            Assert.Less(state.Players[1].Health, hpBefore,
                "Overhead explosion (dx=0) should damage HP — horizontal shield cannot block vertical hits");
            Assert.AreEqual(shieldBefore, state.Players[1].ShieldHP, 0.01f,
                "Shield should NOT absorb an overhead hit (dx=0) regardless of facing");
        }

        [Test]
        public void Explosion_ZeroArmorMultiplier_DoesNotCauseInfinityDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].ArmorMultiplier = 0f;
            float hpBefore = state.Players[1].Health;

            CombatResolver.ApplyExplosion(state, state.Players[1].Position, 5f, 50f, 5f, 0, false);

            float damage = hpBefore - state.Players[1].Health;
            Assert.IsFalse(float.IsInfinity(damage),
                "Explosion with ArmorMultiplier=0 should not produce Infinity damage");
            Assert.IsFalse(float.IsNaN(damage),
                "Explosion with ArmorMultiplier=0 should not produce NaN damage");
            Assert.Greater(damage, 0f,
                "Explosion should still deal positive damage when ArmorMultiplier=0");
        }

        [Test]
        public void FireZone_ZeroArmorMultiplier_DoesNotCauseInfinityDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].ArmorMultiplier = 0f;
            state.FireZones.Add(new FireZoneState
            {
                Position = state.Players[1].Position,
                Radius = 5f,
                DamagePerSecond = 20f,
                RemainingTime = 5f,
                OwnerIndex = 0,
                Active = true
            });
            float hpBefore = state.Players[1].Health;

            GameSimulation.Tick(state, 0.1f);

            float damage = hpBefore - state.Players[1].Health;
            Assert.IsFalse(float.IsInfinity(damage),
                "FireZone with ArmorMultiplier=0 should not produce Infinity damage");
            Assert.IsFalse(float.IsNaN(damage),
                "FireZone with ArmorMultiplier=0 should not produce NaN damage");
            Assert.Greater(damage, 0f,
                "FireZone should still deal positive damage when ArmorMultiplier=0");
        }

        [Test]
        public void Hitscan_ZeroArmorMultiplier_DoesNotCauseInfinityDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Set player 0's active weapon to hitscan
            state.Players[0].WeaponSlots[0] = new WeaponSlotState
            {
                WeaponId = "test_laser", Ammo = -1,
                MaxDamage = 30f, IsHitscan = true,
                MinPower = 10f, MaxPower = 30f
            };
            state.Players[0].ActiveWeaponSlot = 0;

            // Place players facing each other
            state.Players[0].Position = new Vec2(5f, 2f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].AimAngle = 0f;
            state.Players[1].Position = new Vec2(8f, 2f);
            state.Players[1].ArmorMultiplier = 0f;
            float hpBefore = state.Players[1].Health;

            GameSimulation.Fire(state, 0);

            float damage = hpBefore - state.Players[1].Health;
            Assert.IsFalse(float.IsInfinity(damage),
                "Hitscan with ArmorMultiplier=0 should not produce Infinity damage");
            Assert.IsFalse(float.IsNaN(damage),
                "Hitscan with ArmorMultiplier=0 should not produce NaN damage");
        }

        [Test]
        public void PierceDamage_ZeroArmorMultiplier_DoesNotCauseInfinityDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[1].ArmorMultiplier = 0f;
            float hpBefore = state.Players[1].Health;

            CombatResolver.ApplyPierceDamage(state, 1, 25f, 0f, state.Players[1].Position, 0);

            float damage = hpBefore - state.Players[1].Health;
            Assert.IsFalse(float.IsInfinity(damage),
                "PierceDamage with ArmorMultiplier=0 should not produce Infinity damage");
            Assert.IsFalse(float.IsNaN(damage),
                "PierceDamage with ArmorMultiplier=0 should not produce NaN damage");
            Assert.Greater(damage, 0f,
                "PierceDamage should still deal positive damage when ArmorMultiplier=0");
        }

    }
}
