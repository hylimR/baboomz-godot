using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public class WeaponMasteryTests
    {
        // --- MasteryTier thresholds ---

        [Test]
        public void GetTier_None_BelowBronze()
        {
            Assert.AreEqual(MasteryTier.None, WeaponMasteryState.GetTier(0));
            Assert.AreEqual(MasteryTier.None, WeaponMasteryState.GetTier(99));
        }

        [Test]
        public void GetTier_Bronze_At100()
        {
            Assert.AreEqual(MasteryTier.Bronze, WeaponMasteryState.GetTier(100));
            Assert.AreEqual(MasteryTier.Bronze, WeaponMasteryState.GetTier(299));
        }

        [Test]
        public void GetTier_Silver_At300()
        {
            Assert.AreEqual(MasteryTier.Silver, WeaponMasteryState.GetTier(300));
            Assert.AreEqual(MasteryTier.Silver, WeaponMasteryState.GetTier(699));
        }

        [Test]
        public void GetTier_Gold_At700()
        {
            Assert.AreEqual(MasteryTier.Gold, WeaponMasteryState.GetTier(700));
            Assert.AreEqual(MasteryTier.Gold, WeaponMasteryState.GetTier(1499));
        }

        [Test]
        public void GetTier_Diamond_At1500()
        {
            Assert.AreEqual(MasteryTier.Diamond, WeaponMasteryState.GetTier(1500));
            Assert.AreEqual(MasteryTier.Diamond, WeaponMasteryState.GetTier(2999));
        }

        [Test]
        public void GetTier_Master_At3000()
        {
            Assert.AreEqual(MasteryTier.Master, WeaponMasteryState.GetTier(3000));
            Assert.AreEqual(MasteryTier.Master, WeaponMasteryState.GetTier(99999));
        }

        [Test]
        public void GetThreshold_ReturnsCorrectValues()
        {
            Assert.AreEqual(0, WeaponMasteryState.GetThreshold(MasteryTier.None));
            Assert.AreEqual(100, WeaponMasteryState.GetThreshold(MasteryTier.Bronze));
            Assert.AreEqual(300, WeaponMasteryState.GetThreshold(MasteryTier.Silver));
            Assert.AreEqual(700, WeaponMasteryState.GetThreshold(MasteryTier.Gold));
            Assert.AreEqual(1500, WeaponMasteryState.GetThreshold(MasteryTier.Diamond));
            Assert.AreEqual(3000, WeaponMasteryState.GetThreshold(MasteryTier.Master));
        }

        // --- WeaponMasteryCalc ---

        [Test]
        public void Calculate_HitsOnly()
        {
            int xp = WeaponMasteryCalc.Calculate(5, 0, false);
            Assert.AreEqual(50, xp); // 5 * 10
        }

        [Test]
        public void Calculate_KillsOnly()
        {
            int xp = WeaponMasteryCalc.Calculate(0, 2, false);
            Assert.AreEqual(100, xp); // 2 * 50
        }

        [Test]
        public void Calculate_MatchUsedBonus()
        {
            int xp = WeaponMasteryCalc.Calculate(0, 0, true);
            Assert.AreEqual(5, xp);
        }

        [Test]
        public void Calculate_AllCombined()
        {
            // 3 hits (30) + 1 kill (50) + used (5) = 85
            int xp = WeaponMasteryCalc.Calculate(3, 1, true);
            Assert.AreEqual(85, xp);
        }

        [Test]
        public void Calculate_ZeroEverything()
        {
            int xp = WeaponMasteryCalc.Calculate(0, 0, false);
            Assert.AreEqual(0, xp);
        }

        [Test]
        public void DamageMultiplier_GraduatedPerTier()
        {
            Assert.AreEqual(1f, WeaponMasteryCalc.GetDamageMultiplier(MasteryTier.None), 0.001f);
            Assert.AreEqual(1.01f, WeaponMasteryCalc.GetDamageMultiplier(MasteryTier.Bronze), 0.001f);
            Assert.AreEqual(1.02f, WeaponMasteryCalc.GetDamageMultiplier(MasteryTier.Silver), 0.001f);
            Assert.AreEqual(1.03f, WeaponMasteryCalc.GetDamageMultiplier(MasteryTier.Gold), 0.001f);
            Assert.AreEqual(1.05f, WeaponMasteryCalc.GetDamageMultiplier(MasteryTier.Diamond), 0.001f);
            Assert.AreEqual(1.08f, WeaponMasteryCalc.GetDamageMultiplier(MasteryTier.Master), 0.001f);
        }

        [Test]
        public void SpeedMultiplier_GraduatedPerTier()
        {
            Assert.AreEqual(1f, WeaponMasteryCalc.GetSpeedMultiplier(MasteryTier.None), 0.001f);
            Assert.AreEqual(1f, WeaponMasteryCalc.GetSpeedMultiplier(MasteryTier.Bronze), 0.001f);
            Assert.AreEqual(1f, WeaponMasteryCalc.GetSpeedMultiplier(MasteryTier.Silver), 0.001f);
            Assert.AreEqual(1.05f, WeaponMasteryCalc.GetSpeedMultiplier(MasteryTier.Gold), 0.001f);
            Assert.AreEqual(1.10f, WeaponMasteryCalc.GetSpeedMultiplier(MasteryTier.Diamond), 0.001f);
            Assert.AreEqual(1.15f, WeaponMasteryCalc.GetSpeedMultiplier(MasteryTier.Master), 0.001f);
        }

        // --- Struct Tier property ---

        [Test]
        public void WeaponMasteryState_Tier_ReflectsXP()
        {
            var state = new WeaponMasteryState { WeaponId = "cannon", XP = 750 };
            Assert.AreEqual(MasteryTier.Gold, state.Tier);
        }

        // --- GameState weapon tracking ---

        [Test]
        public void InitWeaponTracking_CreatesArrays()
        {
            var gs = new GameState();
            gs.InitWeaponTracking(2);
            Assert.IsNotNull(gs.WeaponHits);
            Assert.IsNotNull(gs.WeaponKills);
            Assert.IsNotNull(gs.WeaponsUsed);
            Assert.AreEqual(2, gs.WeaponHits.Length);
            Assert.AreEqual(2, gs.WeaponKills.Length);
            Assert.AreEqual(2, gs.WeaponsUsed.Length);
        }

        [Test]
        public void TrackWeaponHit_IncrementsCount()
        {
            var gs = CreateStateWithTracking();
            CombatResolver.TrackWeaponHit(gs, 0, "cannon");
            CombatResolver.TrackWeaponHit(gs, 0, "cannon");
            CombatResolver.TrackWeaponHit(gs, 0, "rocket");

            Assert.AreEqual(2, gs.WeaponHits[0]["cannon"]);
            Assert.AreEqual(1, gs.WeaponHits[0]["rocket"]);
        }

        [Test]
        public void TrackWeaponKill_IncrementsCount()
        {
            var gs = CreateStateWithTracking();
            CombatResolver.TrackWeaponKill(gs, 0, "rocket");
            CombatResolver.TrackWeaponKill(gs, 0, "rocket");

            Assert.AreEqual(2, gs.WeaponKills[0]["rocket"]);
        }

        [Test]
        public void TrackWeaponHit_NullWeaponId_NoOp()
        {
            var gs = CreateStateWithTracking();
            CombatResolver.TrackWeaponHit(gs, 0, null);
            Assert.AreEqual(0, gs.WeaponHits[0].Count);
        }

        [Test]
        public void TrackWeaponKill_NullWeaponId_NoOp()
        {
            var gs = CreateStateWithTracking();
            CombatResolver.TrackWeaponKill(gs, 0, null);
            Assert.AreEqual(0, gs.WeaponKills[0].Count);
        }

        [Test]
        public void TrackWeaponHit_OutOfBoundsPlayer_NoOp()
        {
            var gs = CreateStateWithTracking();
            CombatResolver.TrackWeaponHit(gs, 5, "cannon");
            // Should not throw
        }

        [Test]
        public void TrackWeaponHit_NegativeOwnerIndex_NoOp()
        {
            var gs = CreateStateWithTracking();
            CombatResolver.TrackWeaponHit(gs, -1, "cannon");
            // Should not throw — -1 is the "no owner" sentinel
        }

        [Test]
        public void TrackWeaponKill_NegativeOwnerIndex_NoOp()
        {
            var gs = CreateStateWithTracking();
            CombatResolver.TrackWeaponKill(gs, -1, "cannon");
            // Should not throw — -1 is the "no owner" sentinel
        }

        // --- Integration: Fire sets SourceWeaponId and WeaponsUsed ---

        [Test]
        public void Fire_SetsSourceWeaponIdOnProjectile()
        {
            var config = new GameConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Fire player 0's cannon
            state.Players[0].AimAngle = 45f;
            state.Players[0].AimPower = 20f;
            state.Players[0].ShootCooldownRemaining = 0f;
            GameSimulation.Fire(state, 0);

            Assert.IsTrue(state.Projectiles.Count > 0);
            Assert.AreEqual("cannon", state.Projectiles[0].SourceWeaponId);
        }

        [Test]
        public void Fire_MarksWeaponAsUsed()
        {
            var config = new GameConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].ShootCooldownRemaining = 0f;
            state.Players[0].AimPower = 20f;
            GameSimulation.Fire(state, 0);

            Assert.IsTrue(state.WeaponsUsed[0].Contains("cannon"));
        }

        // --- Mastery XP calculation for full match scenario ---

        [Test]
        public void MasteryXP_FullScenario()
        {
            // Simulate: 5 hits + 1 kill with cannon, used in match
            int xp = WeaponMasteryCalc.Calculate(5, 1, true);
            // 5*10 + 1*50 + 5 = 105
            Assert.AreEqual(105, xp);
            // 105 XP = Bronze tier
            Assert.AreEqual(MasteryTier.Bronze, WeaponMasteryState.GetTier(xp));
        }

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

        static WeaponSlotState MakeSlot(string weaponId, float maxDamage,
            float minPower = 10f, float maxPower = 30f,
            int projectileCount = 1, float spreadAngle = 0f,
            int clusterCount = 0, float fuseTime = 0f,
            float chainDamage = 0f, float chainRange = 0f,
            int maxPierceCount = 0, int bounces = 0,
            float explosionRadius = 2f, float energyCost = 10f)
        {
            return new WeaponSlotState
            {
                WeaponId = weaponId, MaxDamage = maxDamage,
                MinPower = minPower, MaxPower = maxPower,
                ProjectileCount = projectileCount, SpreadAngle = spreadAngle,
                ClusterCount = clusterCount, FuseTime = fuseTime,
                ChainDamage = chainDamage, ChainRange = chainRange,
                MaxPierceCount = maxPierceCount, Bounces = bounces,
                ExplosionRadius = explosionRadius, EnergyCost = energyCost
            };
        }

        private GameState CreateStateWithTracking()
        {
            var gs = new GameState();
            gs.InitWeaponTracking(2);
            return gs;
        }
    }
}
