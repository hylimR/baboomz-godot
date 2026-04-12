using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Cycle 5: Napalm weapon tests ---

        [Test]
        public void Napalm_ExistsInConfig_Slot5()
        {
            var config = new GameConfig();
            Assert.AreEqual("napalm", config.Weapons[5].WeaponId);
            Assert.IsTrue(config.Weapons[5].IsNapalm);
            Assert.AreEqual(2, config.Weapons[5].Ammo);
            Assert.Greater(config.Weapons[5].FireZoneDuration, 0f);
            Assert.Greater(config.Weapons[5].FireZoneDPS, 0f);
        }

        [Test]
        public void Napalm_CreatesFireZoneOnTerrainImpact()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Fire napalm (slot 5)
            state.Players[0].ActiveWeaponSlot = 5;
            state.Players[0].AimPower = 15f;
            state.Players[0].AimAngle = 60f;
            state.Players[0].Energy = 100f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Projectiles.Count);
            Assert.IsTrue(state.Projectiles[0].IsNapalm);

            // Tick until projectile hits terrain
            bool fireZoneCreated = false;
            for (int i = 0; i < 600; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.FireZones.Count > 0)
                {
                    fireZoneCreated = true;
                    break;
                }
            }

            Assert.IsTrue(fireZoneCreated, "Napalm should create a fire zone on terrain impact");
            Assert.IsTrue(state.FireZones[0].Active);
            Assert.Greater(state.FireZones[0].RemainingTime, 0f);
        }

        [Test]
        public void FireZone_DamagesPlayersStandingInIt()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place a fire zone directly at player 2's position
            state.FireZones.Add(new FireZoneState
            {
                Position = state.Players[1].Position,
                Radius = 3f,
                DamagePerSecond = 20f,
                RemainingTime = 5f,
                OwnerIndex = 0,
                Active = true
            });

            float healthBefore = state.Players[1].Health;

            // Tick a few frames
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Less(state.Players[1].Health, healthBefore,
                "Fire zone should damage players standing in it");
        }

        [Test]
        public void FireZone_ExpiresAfterDuration()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.FireZones.Add(new FireZoneState
            {
                Position = new Vec2(50f, 50f), // far from players
                Radius = 3f,
                DamagePerSecond = 10f,
                RemainingTime = 0.5f, // very short
                OwnerIndex = 0,
                Active = true
            });

            Assert.AreEqual(1, state.FireZones.Count);

            // Tick past the duration
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.FireZones.Count, "Fire zone should be removed after expiry");
        }

        [Test]
        public void FireZone_DoesNotDamageOwner()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            float initialHealth = state.Players[0].Health;

            // Place fire zone at player 0's position, owned by player 0
            state.FireZones.Add(new FireZoneState
            {
                Position = state.Players[0].Position,
                Radius = 5f,
                DamagePerSecond = 50f,
                RemainingTime = 2f,
                OwnerIndex = 0,
                Active = true
            });

            // Tick a few frames
            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(initialHealth, state.Players[0].Health, 0.01f,
                "Fire zone should not damage its owner");
        }

        [Test]
        public void FireZone_EmitsDamageEvents()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place a fire zone at player 2's position
            state.FireZones.Add(new FireZoneState
            {
                Position = state.Players[1].Position,
                Radius = 3f,
                DamagePerSecond = 20f,
                RemainingTime = 5f,
                OwnerIndex = 0,
                Active = true
            });

            state.DamageEvents.Clear();

            // Tick one frame
            GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.DamageEvents.Count, 0,
                "Fire zone damage should emit DamageEvents for damage numbers and kill feed");

            var dmgEvent = state.DamageEvents[state.DamageEvents.Count - 1];
            Assert.AreEqual(1, dmgEvent.TargetIndex);
            Assert.Greater(dmgEvent.Amount, 0f);
        }

        [Test]
        public void FireZone_AppliesCasterDamageMultiplier()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Kill player 0 so AI doesn't fire, but keep DamageMultiplier readable
            state.Players[0].IsDead = true;
            state.Players[0].DamageMultiplier = 2f;

            // Clear any projectiles from AI pre-fire
            state.Projectiles.Clear();

            // Place a fire zone at player 1's position, owned by player 0
            state.FireZones.Add(new FireZoneState
            {
                Position = state.Players[1].Position,
                Radius = 3f,
                DamagePerSecond = 20f,
                RemainingTime = 5f,
                OwnerIndex = 0,
                Active = true
            });

            state.DamageEvents.Clear();

            // Tick one frame
            GameSimulation.Tick(state, 0.016f);

            // Find the fire zone damage event (SourceIndex == 0, TargetIndex == 1)
            float fireZoneDamage = 0f;
            for (int i = 0; i < state.DamageEvents.Count; i++)
            {
                var evt = state.DamageEvents[i];
                if (evt.SourceIndex == 0 && evt.TargetIndex == 1)
                    fireZoneDamage += evt.Amount;
            }

            Assert.Greater(fireZoneDamage, 0f, "Fire zone should deal damage");

            // With throttled events (0.5s intervals), the reported damage is
            // DamagePerSecond * 0.5 * DamageMultiplier = 20 * 0.5 * 2 = 20
            float expectedWithMultiplier = 20f * 0.5f * 2f;
            Assert.AreEqual(expectedWithMultiplier, fireZoneDamage, 0.1f,
                "Fire zone damage event should apply caster's DamageMultiplier");
        }

        [Test]
        public void FireZone_KillCreditsOwnerWithComboAndWeaponMastery()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Disable AI so it doesn't interfere with the test
            state.Players[1].IsAI = false;

            // Set player 1 to very low health so fire zone kills them
            state.Players[1].Health = 1f;

            // Place a fire zone at player 1's position, owned by player 0
            var p1Pos = state.Players[1].Position;
            state.FireZones.Add(new FireZoneState
            {
                Position = p1Pos,
                Radius = 3f,
                DamagePerSecond = 200f,
                RemainingTime = 5f,
                OwnerIndex = 0,
                SourceWeaponId = "napalm",
                Active = true
            });

            // Single tick — 200 DPS * 0.016s = 3.2 dmg > 1 HP → kill
            GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.Players[1].IsDead, "Player 1 should die from fire zone");

            // Verify kill combo tracking
            Assert.AreEqual(1, state.Players[0].KillsInWindow,
                "Fire zone kill should credit owner with kill combo tracking");

            // Verify weapon mastery kill tracking
            Assert.IsTrue(state.WeaponKills[0].ContainsKey("napalm"),
                "Fire zone kill should track weapon mastery kill for napalm");
            Assert.AreEqual(1, state.WeaponKills[0]["napalm"]);
        }
    }
}
