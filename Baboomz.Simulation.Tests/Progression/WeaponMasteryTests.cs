using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    [TestFixture]
    public partial class WeaponMasteryTests
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

        // --- Helpers ---

        static WeaponSlotState MakeSlot(string weaponId, float maxDamage,
            float minPower = 10f, float maxPower = 30f,
            int projectileCount = 1, float spreadAngle = 0f,
            int clusterCount = 0, float fuseTime = 0f,
            float chainDamage = 0f, float chainRange = 0f,
            int maxPierceCount = 0, int bounces = 0,
            float explosionRadius = 2f, float energyCost = 10f,
            float fireZoneDuration = 0f, float fireZoneDPS = 0f,
            int airstrikeCount = 0, float drillRange = 0f,
            float knockbackForce = 5f, float pullRadius = 0f,
            float pullForce = 0f, float lavaMeltRadius = 0f,
            float flakMaxDist = 0f)
        {
            return new WeaponSlotState
            {
                WeaponId = weaponId, MaxDamage = maxDamage,
                MinPower = minPower, MaxPower = maxPower,
                ProjectileCount = projectileCount, SpreadAngle = spreadAngle,
                ClusterCount = clusterCount, FuseTime = fuseTime,
                ChainDamage = chainDamage, ChainRange = chainRange,
                MaxPierceCount = maxPierceCount, Bounces = bounces,
                ExplosionRadius = explosionRadius, EnergyCost = energyCost,
                FireZoneDuration = fireZoneDuration, FireZoneDPS = fireZoneDPS,
                AirstrikeCount = airstrikeCount, DrillRange = drillRange,
                KnockbackForce = knockbackForce, PullRadius = pullRadius,
                PullForce = pullForce, LavaMeltRadius = lavaMeltRadius,
                FlakMaxDist = flakMaxDist
            };
        }

        [Test]
        public void FireZone_DoT_TracksWeaponMasteryHitsAndDamage()
        {
            var config = new GameConfig
            {
                MineCount = 0, BarrelCount = 0,
                TerrainWidth = 320, TerrainHeight = 160, TerrainPPU = 8f,
                MapWidth = 40f, TerrainMinHeight = -2f, TerrainMaxHeight = 5f,
                TerrainHillFrequency = 0.1f, TerrainFloorDepth = -10f,
                Player1SpawnX = -10f, Player2SpawnX = 10f,
                SpawnProbeY = 20f, DeathBoundaryY = -25f,
                Gravity = 9.81f, DefaultMaxHealth = 100f,
                DefaultMoveSpeed = 5f, DefaultJumpForce = 10f,
                DefaultShootCooldown = 0.5f
            };
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.FireZones.Add(new FireZoneState
            {
                Position = state.Players[1].Position,
                Radius = 5f,
                DamagePerSecond = 20f,
                RemainingTime = 5f,
                OwnerIndex = 0,
                SourceWeaponId = "napalm",
                Active = true,
                DamageEventTimer = 0f
            });

            GameSimulation.Tick(state, 0.1f);

            Assert.IsTrue(state.WeaponHits[0].ContainsKey("napalm"),
                "Fire zone DoT should track weapon hits for mastery");
            Assert.IsTrue(state.WeaponDamage[0].ContainsKey("napalm"),
                "Fire zone DoT should track weapon damage for mastery");
            Assert.Greater(state.WeaponDamage[0]["napalm"], 0f,
                "Fire zone DoT weapon damage should be positive");
        }

        private GameState CreateStateWithTracking()
        {
            var gs = new GameState();
            gs.InitWeaponTracking(2);
            return gs;
        }
    }
}
