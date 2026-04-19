using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class UnlockRegistryTests
    {
        [Test]
        public void GetTier_ZeroWins_ReturnsTier0()
        {
            Assert.AreEqual(0, UnlockRegistry.GetTier(0));
        }

        [Test]
        public void GetTier_FiveWins_ReturnsTier1()
        {
            Assert.AreEqual(1, UnlockRegistry.GetTier(5));
        }

        [Test]
        public void GetTier_FifteenWins_ReturnsTier2()
        {
            Assert.AreEqual(2, UnlockRegistry.GetTier(15));
        }

        [Test]
        public void GetTier_ThirtyWins_ReturnsTier3()
        {
            Assert.AreEqual(3, UnlockRegistry.GetTier(30));
        }

        [Test]
        public void GetTier_FiftyWins_ReturnsTier4()
        {
            Assert.AreEqual(4, UnlockRegistry.GetTier(50));
        }

        [Test]
        public void GetTier_BetweenThresholds_ReturnsLowerTier()
        {
            Assert.AreEqual(1, UnlockRegistry.GetTier(10)); // between 5 and 15
            Assert.AreEqual(3, UnlockRegistry.GetTier(45)); // between 30 and 50
        }

        [Test]
        public void GetWinsForNextTier_AtZero_Returns5()
        {
            Assert.AreEqual(5, UnlockRegistry.GetWinsForNextTier(0));
        }

        [Test]
        public void GetWinsForNextTier_AtMaxTier_ReturnsZero()
        {
            Assert.AreEqual(0, UnlockRegistry.GetWinsForNextTier(50));
            Assert.AreEqual(0, UnlockRegistry.GetWinsForNextTier(100));
        }

        [Test]
        public void GetWinsForNextTier_MidTier_ReturnsCorrectDifference()
        {
            // 10 wins = tier 1, next tier at 15, so 5 more
            Assert.AreEqual(5, UnlockRegistry.GetWinsForNextTier(10));
        }

        [Test]
        public void IsWeaponUnlocked_CannonAlwaysUnlocked()
        {
            Assert.IsTrue(UnlockRegistry.IsWeaponUnlocked("cannon", 0));
        }

        [Test]
        public void IsWeaponUnlocked_ClusterLockedAtTier0()
        {
            Assert.IsFalse(UnlockRegistry.IsWeaponUnlocked("cluster", 0));
        }

        [Test]
        public void IsWeaponUnlocked_ClusterUnlockedAtTier1()
        {
            Assert.IsTrue(UnlockRegistry.IsWeaponUnlocked("cluster", 1));
        }

        [Test]
        public void IsWeaponUnlocked_SheepLockedUntilTier4()
        {
            Assert.IsFalse(UnlockRegistry.IsWeaponUnlocked("sheep", 3));
            Assert.IsTrue(UnlockRegistry.IsWeaponUnlocked("sheep", 4));
        }

        [Test]
        public void IsSkillIndexUnlocked_FirstSixAlwaysUnlocked()
        {
            for (int i = 0; i < 6; i++)
                Assert.IsTrue(UnlockRegistry.IsSkillIndexUnlocked(i, 0), $"Skill {i} should be unlocked at tier 0");
        }

        [Test]
        public void IsSkillIndexUnlocked_GirderLockedAtTier0()
        {
            Assert.IsFalse(UnlockRegistry.IsSkillIndexUnlocked(6, 0));
        }

        [Test]
        public void IsSkillIndexUnlocked_GirderUnlockedAtTier1()
        {
            Assert.IsTrue(UnlockRegistry.IsSkillIndexUnlocked(6, 1));
        }

        [Test]
        public void GetUnlockedWeaponIds_Tier0_ReturnsFiveWeapons()
        {
            var ids = UnlockRegistry.GetUnlockedWeaponIds(0);
            Assert.AreEqual(5, ids.Count);
            Assert.Contains("cannon", ids);
            Assert.Contains("rocket", ids);
            Assert.Contains("dynamite", ids);
            Assert.Contains("shotgun", ids);
            Assert.Contains("drill", ids);
        }

        [Test]
        public void GetUnlockedWeaponIds_Tier4_ReturnsAll22Weapons()
        {
            var ids = UnlockRegistry.GetUnlockedWeaponIds(4);
            Assert.AreEqual(22, ids.Count);
        }

        [Test]
        public void GetUnlockedSkillIndices_Tier0_ReturnsSixSkills()
        {
            var indices = UnlockRegistry.GetUnlockedSkillIndices(0);
            Assert.AreEqual(6, indices.Count);
        }

        [Test]
        public void GetUnlockedSkillIndices_Tier4_ReturnsAll20Skills()
        {
            var indices = UnlockRegistry.GetUnlockedSkillIndices(4);
            Assert.AreEqual(20, indices.Count);
        }

        [Test]
        public void CreateMatch_Tier0_PlayerHasOnlyStarterWeapons()
        {
            var config = new GameConfig { UnlockedTier = 0 };
            var state = GameSimulation.CreateMatch(config, 42);
            var player = state.Players[0];

            int unlocked = 0;
            for (int i = 0; i < player.WeaponSlots.Length; i++)
                if (player.WeaponSlots[i].WeaponId != null) unlocked++;

            Assert.AreEqual(5, unlocked, "Tier 0 player should have exactly 5 weapons");
            Assert.IsNotNull(player.WeaponSlots[0].WeaponId); // cannon
        }

        [Test]
        public void CreateMatch_Tier0_AIHasAllWeapons()
        {
            var config = new GameConfig { UnlockedTier = 0 };
            var state = GameSimulation.CreateMatch(config, 42);
            var ai = state.Players[1];

            int unlocked = 0;
            for (int i = 0; i < ai.WeaponSlots.Length; i++)
                if (ai.WeaponSlots[i].WeaponId != null) unlocked++;

            Assert.AreEqual(22, unlocked, "AI should always have all 22 weapons regardless of unlock tier");
        }

        [Test]
        public void CreateMatch_Tier4_PlayerHasAllWeapons()
        {
            var config = new GameConfig { UnlockedTier = 4 };
            var state = GameSimulation.CreateMatch(config, 42);
            var player = state.Players[0];

            int unlocked = 0;
            for (int i = 0; i < player.WeaponSlots.Length; i++)
                if (player.WeaponSlots[i].WeaponId != null) unlocked++;

            Assert.AreEqual(22, unlocked, "Tier 4 player should have all 22 weapons");
        }

        [Test]
        public void CreateMatch_Tier0_LockedSkillFallsBackToDefault()
        {
            var config = new GameConfig { UnlockedTier = 0 };
            // Skill index 16 = overcharge, locked at tier 0
            var state = GameSimulation.CreateMatch(config, 42, playerSkill0: 0, playerSkill1: 16);
            var player = state.Players[0];

            // Slot 1 should have fallen back to default (dash, index 3)
            Assert.AreEqual("dash", player.SkillSlots[1].SkillId,
                "Locked skill should fall back to default skill slot");
        }

        [Test]
        public void GetTierName_ValidTiers_ReturnCorrectNames()
        {
            Assert.AreEqual("Starter", UnlockRegistry.GetTierName(0));
            Assert.AreEqual("Veteran", UnlockRegistry.GetTierName(1));
            Assert.AreEqual("Expert", UnlockRegistry.GetTierName(2));
            Assert.AreEqual("Master", UnlockRegistry.GetTierName(3));
            Assert.AreEqual("Legend", UnlockRegistry.GetTierName(4));
        }
    }
}
