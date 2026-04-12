using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class WeaponMasteryTests
    {
        // --- Mastery Mods (issue #446) ---

        [Test]
        public void ApplyMasteryMods_None_NoChanges()
        {
            var slot = MakeSlot("cannon", 30f);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.None);
            Assert.AreEqual(30f, slot.MaxDamage, 0.01f);
        }

        [Test]
        public void ApplyMasteryMods_Bronze_OnlyDamage()
        {
            var slot = MakeSlot("cannon", 30f);
            float originalPower = slot.MaxPower;
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Bronze);
            Assert.AreEqual(30f * 1.01f, slot.MaxDamage, 0.01f);
            Assert.AreEqual(originalPower, slot.MaxPower, 0.01f); // no silver mod
        }

        [Test]
        public void ApplyMasteryMods_Silver_Cannon_SpeedBoost()
        {
            var slot = MakeSlot("cannon", 30f, minPower: 10f, maxPower: 30f);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Silver);
            Assert.AreEqual(10f * 1.05f, slot.MinPower, 0.01f);
            Assert.AreEqual(30f * 1.05f, slot.MaxPower, 0.01f);
        }

        [Test]
        public void ApplyMasteryMods_Silver_Shotgun_ExtraPellet()
        {
            var slot = MakeSlot("shotgun", 15f, projectileCount: 4);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Silver);
            Assert.AreEqual(5, slot.ProjectileCount);
        }

        [Test]
        public void ApplyMasteryMods_Silver_Cluster_ExtraSub()
        {
            var slot = MakeSlot("cluster", 20f, clusterCount: 4);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Silver);
            Assert.AreEqual(5, slot.ClusterCount);
        }

        [Test]
        public void ApplyMasteryMods_Silver_Dynamite_ShorterFuse()
        {
            var slot = MakeSlot("dynamite", 80f, fuseTime: 3f);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Silver);
            Assert.AreEqual(2.5f, slot.FuseTime, 0.01f);
        }

        [Test]
        public void ApplyMasteryMods_Silver_LightningRod_ChainDamage()
        {
            var slot = MakeSlot("lightning_rod", 40f, chainDamage: 20f);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Silver);
            Assert.AreEqual(30f, slot.ChainDamage, 0.01f);
        }

        [Test]
        public void ApplyMasteryMods_Silver_Harpoon_ExtraPierce()
        {
            var slot = MakeSlot("harpoon", 40f, maxPierceCount: 1);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Silver);
            Assert.AreEqual(2, slot.MaxPierceCount);
        }

        [Test]
        public void ApplyMasteryMods_Gold_Cannon_ExtraBounce()
        {
            var slot = MakeSlot("cannon", 30f, bounces: 0);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Gold);
            Assert.AreEqual(1, slot.Bounces);
        }

        [Test]
        public void ApplyMasteryMods_Gold_Rocket_ReducedEnergyCost()
        {
            var slot = MakeSlot("rocket", 60f, energyCost: 25f);
            slot.ExplosionRadius = 4f;
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Gold);
            Assert.AreEqual(4.5f, slot.ExplosionRadius, 0.01f); // silver: +0.5
            Assert.AreEqual(25f * 0.9f, slot.EnergyCost, 0.01f); // gold: -10%
        }

        [Test]
        public void ApplyMasteryMods_Gold_Shotgun_TighterSpread()
        {
            var slot = MakeSlot("shotgun", 15f, projectileCount: 4, spreadAngle: 25f);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Gold);
            Assert.AreEqual(5, slot.ProjectileCount); // silver mod
            Assert.AreEqual(25f * 0.85f, slot.SpreadAngle, 0.01f); // gold mod
        }

        [Test]
        public void ApplyMasteryMods_Gold_FreezeGrenade_LargerRadius()
        {
            var slot = MakeSlot("freeze_grenade", 5f, explosionRadius: 3f);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Gold);
            Assert.AreEqual(3.5f, slot.ExplosionRadius, 0.01f);
        }

        [Test]
        public void ApplyMasteryMods_Gold_LightningRod_ExtraChainRange()
        {
            var slot = MakeSlot("lightning_rod", 40f, chainDamage: 20f, chainRange: 6f);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Gold);
            Assert.AreEqual(30f, slot.ChainDamage, 0.01f); // silver
            Assert.AreEqual(9f, slot.ChainRange, 0.01f); // gold: +3
        }

        [Test]
        public void ApplyMasteryMods_Gold_Harpoon_ExtraDamage()
        {
            var slot = MakeSlot("harpoon", 40f, maxPierceCount: 1);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Gold);
            Assert.AreEqual(2, slot.MaxPierceCount); // silver
            Assert.AreEqual((40f * 1.03f) + 10f, slot.MaxDamage, 0.1f); // gold +10 on top of +3% tier dmg
        }

        [Test]
        public void GetFreezeBonus_SilverPlus_Returns05()
        {
            Assert.AreEqual(0.5f, WeaponMasteryCalc.GetFreezeBonus("freeze_grenade", MasteryTier.Silver));
            Assert.AreEqual(0.5f, WeaponMasteryCalc.GetFreezeBonus("freeze_grenade", MasteryTier.Master));
        }

        [Test]
        public void GetFreezeBonus_BelowSilver_ReturnsZero()
        {
            Assert.AreEqual(0f, WeaponMasteryCalc.GetFreezeBonus("freeze_grenade", MasteryTier.Bronze));
            Assert.AreEqual(0f, WeaponMasteryCalc.GetFreezeBonus("cannon", MasteryTier.Master));
        }

        [Test]
        public void GetBoomerangSteerBonus_SilverPlus_Returns5()
        {
            Assert.AreEqual(5f, WeaponMasteryCalc.GetBoomerangSteerBonus(MasteryTier.Silver));
            Assert.AreEqual(5f, WeaponMasteryCalc.GetBoomerangSteerBonus(MasteryTier.Master));
        }

        [Test]
        public void GetBoomerangSteerBonus_BelowSilver_ReturnsZero()
        {
            Assert.AreEqual(0f, WeaponMasteryCalc.GetBoomerangSteerBonus(MasteryTier.None));
            Assert.AreEqual(0f, WeaponMasteryCalc.GetBoomerangSteerBonus(MasteryTier.Bronze));
        }

        // --- Issue #56: new weapon mods for remaining 12 weapons ---

        [Test]
        public void ApplyMasteryMods_Silver_Napalm_LongerFireZone()
        {
            var slot = MakeSlot("napalm", 20f, fireZoneDuration: 5f);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Silver);
            Assert.AreEqual(6f, slot.FireZoneDuration, 0.01f);
        }

        [Test]
        public void ApplyMasteryMods_Gold_Napalm_HigherFireDPS()
        {
            var slot = MakeSlot("napalm", 20f, fireZoneDuration: 5f, fireZoneDPS: 15f);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Gold);
            Assert.AreEqual(6f, slot.FireZoneDuration, 0.01f); // silver
            Assert.AreEqual(18f, slot.FireZoneDPS, 0.01f); // gold: +3
        }

        [Test]
        public void ApplyMasteryMods_Silver_Airstrike_ExtraBomb()
        {
            var slot = MakeSlot("airstrike", 35f, airstrikeCount: 4);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Silver);
            Assert.AreEqual(5, slot.AirstrikeCount);
        }

        [Test]
        public void ApplyMasteryMods_Gold_Airstrike_ReducedEnergy()
        {
            var slot = MakeSlot("airstrike", 35f, airstrikeCount: 4, energyCost: 40f);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Gold);
            Assert.AreEqual(5, slot.AirstrikeCount); // silver
            Assert.AreEqual(35f, slot.EnergyCost, 0.01f); // gold: -5
        }

        [Test]
        public void ApplyMasteryMods_Silver_BananaBomb_ExtraCluster()
        {
            var slot = MakeSlot("banana_bomb", 22f, clusterCount: 6);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Silver);
            Assert.AreEqual(7, slot.ClusterCount);
        }

        [Test]
        public void ApplyMasteryMods_Gold_GravityBomb_StrongerPull()
        {
            var slot = MakeSlot("gravity_bomb", 30f, pullRadius: 5f, pullForce: 10f);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Gold);
            Assert.AreEqual(6f, slot.PullRadius, 0.01f); // silver: +1
            Assert.AreEqual(15f, slot.PullForce, 0.01f); // gold: +5
        }

        [Test]
        public void ApplyMasteryMods_Silver_FlakCannon_ExtraFragments()
        {
            var slot = MakeSlot("flak_cannon", 10f, clusterCount: 8);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Silver);
            Assert.AreEqual(10, slot.ClusterCount); // +2
        }

        [Test]
        public void ApplyMasteryMods_Gold_FlakCannon_LongerBurstRange()
        {
            var slot = MakeSlot("flak_cannon", 10f, clusterCount: 8, flakMaxDist: 25f);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Gold);
            Assert.AreEqual(10, slot.ClusterCount); // silver: +2
            Assert.AreEqual(30f, slot.FlakMaxDist, 0.01f); // gold: +5
        }

        [Test]
        public void ApplyMasteryMods_Silver_GustCannon_StrongerKnockback()
        {
            var slot = MakeSlot("gust_cannon", 15f, knockbackForce: 8f);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Silver);
            Assert.AreEqual(11f, slot.KnockbackForce, 0.01f); // +3
        }

        [Test]
        public void ApplyMasteryMods_Silver_RicochetDisc_ExtraBounce()
        {
            var slot = MakeSlot("ricochet_disc", 25f, bounces: 3);
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Silver);
            Assert.AreEqual(4, slot.Bounces);
        }

        [Test]
        public void ApplyMasteryMods_NullWeaponId_NoOp()
        {
            var slot = new WeaponSlotState { WeaponId = null, MaxDamage = 10f };
            WeaponMasteryCalc.ApplyMasteryMods(ref slot, MasteryTier.Master);
            Assert.AreEqual(10f, slot.MaxDamage, 0.01f);
        }

        [Test]
        public void CreatePlayer_WithMasteryTiers_AppliesMods()
        {
            var config = new GameConfig();
            // Set cannon (slot 0) to Silver mastery
            config.WeaponMasteryTiers = new MasteryTier[config.Weapons.Length];
            config.WeaponMasteryTiers[0] = MasteryTier.Silver;

            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            var cannonSlot = state.Players[0].WeaponSlots[0];
            float expectedDmg = 30f * 1.02f; // Silver damage bonus
            Assert.AreEqual(expectedDmg, cannonSlot.MaxDamage, 0.1f);
            // Silver cannon mod: +5% power
            Assert.AreEqual(10f * 1.05f, cannonSlot.MinPower, 0.1f);
        }

        [Test]
        public void Fire_WithMasterySpeedBonus_IncreasesVelocity()
        {
            var config = new GameConfig();
            config.WeaponMasteryTiers = new MasteryTier[config.Weapons.Length];
            config.WeaponMasteryTiers[0] = MasteryTier.Gold; // cannon at Gold = +5% speed

            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;
            state.Players[0].AimAngle = 45f;
            state.Players[0].AimPower = 20f;
            state.Players[0].ShootCooldownRemaining = 0f;

            GameSimulation.Fire(state, 0);
            Assert.IsTrue(state.Projectiles.Count > 0);

            // Velocity magnitude should be 20 * 1.05 = 21
            var v = state.Projectiles[0].Velocity;
            float speed = MathF.Sqrt(v.x * v.x + v.y * v.y);
            Assert.AreEqual(21f, speed, 0.5f);
        }
    }
}
