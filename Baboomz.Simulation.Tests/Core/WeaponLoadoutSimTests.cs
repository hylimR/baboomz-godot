using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class WeaponLoadoutSimTests
    {
        static GameConfig BaseConfig()
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

        [Test]
        public void PlayerWeaponLoadout_NullLoadout_AllWeaponsAvailable()
        {
            var config = BaseConfig();
            config.PlayerWeaponLoadout = null;
            var state = GameSimulation.CreateMatch(config, 42);

            // With no loadout filter, player should have all non-null weapon slots
            int nonNullCount = 0;
            foreach (var slot in state.Players[0].WeaponSlots)
                if (slot.WeaponId != null) nonNullCount++;

            Assert.Greater(nonNullCount, 4, "Player should have more than 4 weapons with no loadout filter");
        }

        [Test]
        public void PlayerWeaponLoadout_4Slots_OnlyLoadoutWeaponsActive()
        {
            var config = BaseConfig();
            // Select slots 0, 2, 4, 6 (indices into Weapons[])
            config.PlayerWeaponLoadout = new[] { 0, 2, 4, 6 };
            var state = GameSimulation.CreateMatch(config, 42);

            var player = state.Players[0];
            int activeCount = 0;
            for (int i = 0; i < player.WeaponSlots.Length; i++)
            {
                bool inLoadout = System.Array.IndexOf(config.PlayerWeaponLoadout, i) >= 0;
                bool isActive = player.WeaponSlots[i].WeaponId != null;
                if (isActive) activeCount++;
                // Slots NOT in loadout must be null
                if (!inLoadout)
                    Assert.IsNull(player.WeaponSlots[i].WeaponId,
                        $"Slot {i} should be null (not in loadout)");
            }

            Assert.LessOrEqual(activeCount, 4, "Player should have at most 4 active weapons");
        }

        [Test]
        public void ValidateLoadout_ValidFour_ReturnsTrue()
        {
            var config = BaseConfig();
            Assert.IsTrue(GameSimulation.ValidateLoadout(new[] { 0, 1, 2, 3 }, config));
        }

        [Test]
        public void ValidateLoadout_NullOrWrongCount_ReturnsFalse()
        {
            var config = BaseConfig();
            Assert.IsFalse(GameSimulation.ValidateLoadout(null, config));
            Assert.IsFalse(GameSimulation.ValidateLoadout(new[] { 0, 1, 2 }, config));
            Assert.IsFalse(GameSimulation.ValidateLoadout(new[] { 0, 1, 2, 3, 4 }, config));
        }

        [Test]
        public void ValidateLoadout_OutOfBoundsIndex_ReturnsFalse()
        {
            var config = BaseConfig();
            int outOfBounds = config.Weapons.Length + 10;
            Assert.IsFalse(GameSimulation.ValidateLoadout(new[] { 0, 1, 2, outOfBounds }, config));
        }

        [Test]
        public void AIWeaponLoadout_SetByCreateMatch_StoresLoadoutButDoesNotRestrict()
        {
            var config = BaseConfig();
            // Don't set AIWeaponLoadout — let CreateMatch auto-select it
            var state = GameSimulation.CreateMatch(config, 42);

            // Loadout is still computed and stored in config for reference
            Assert.IsNotNull(config.AIWeaponLoadout, "CreateMatch should auto-assign AI weapon loadout");

            // But AI keeps all weapons — SelectWeapon references all slot indices
            int aiActiveCount = 0;
            foreach (var slot in state.Players[1].WeaponSlots)
                if (slot.WeaponId != null) aiActiveCount++;

            Assert.AreEqual(config.Weapons.Length, aiActiveCount, "AI should have all weapons (loadout not applied)");
        }

        [Test]
        public void GetDefaultLoadout_Returns4Slots()
        {
            var config = BaseConfig();
            var loadout = GameSimulation.GetDefaultLoadout(config);
            Assert.AreEqual(4, loadout.Length);
        }
    }
}
