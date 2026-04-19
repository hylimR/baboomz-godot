using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Zero ArmorMultiplier edge-case tests ---

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

            state.Players[0].WeaponSlots[0] = new WeaponSlotState
            {
                WeaponId = "test_laser", Ammo = -1,
                MaxDamage = 30f, IsHitscan = true,
                MinPower = 10f, MaxPower = 30f
            };
            state.Players[0].ActiveWeaponSlot = 0;

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
