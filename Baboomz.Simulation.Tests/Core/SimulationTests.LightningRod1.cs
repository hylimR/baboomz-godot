using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Lightning Rod weapon tests ---

        [Test]
        public void LightningRod_ExistsInConfig()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Weapons.Length >= 15, "Should have at least 15 weapons");
            Assert.AreEqual("lightning_rod", config.Weapons[14].WeaponId);
            Assert.AreEqual(40f, config.Weapons[14].MaxDamage);
            Assert.AreEqual(3, config.Weapons[14].Ammo);
            Assert.IsTrue(config.Weapons[14].IsHitscan);
            Assert.AreEqual(6f, config.Weapons[14].ChainRange);
            Assert.AreEqual(20f, config.Weapons[14].ChainDamage);
        }

        [Test]
        public void LightningRod_HitscanDoesNotCreateProjectile()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].ActiveWeaponSlot = 14; // lightning rod
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(0, state.Projectiles.Count,
                "Hitscan weapon should not create a projectile");
            Assert.AreEqual(1, state.HitscanEvents.Count,
                "Should emit one HitscanEvent");
        }

        [Test]
        public void LightningRod_DamagesTargetOnDirectHit()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place player 1 directly in front of player 0
            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(5f, 5f);

            float healthBefore = state.Players[1].Health;

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f; // aim straight right
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.Less(state.Players[1].Health, healthBefore,
                "Lightning rod should damage the target");
            Assert.AreEqual(1, state.HitscanEvents.Count);
            Assert.AreEqual(1, state.HitscanEvents[0].PrimaryTargetIndex);
        }

        [Test]
        public void LightningRod_ChainsToNearbyTarget()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Add a third player manually for chain testing
            var players = new PlayerState[3];
            players[0] = state.Players[0];
            players[1] = state.Players[1];
            players[2] = state.Players[1]; // clone player 1 as template
            players[2].Name = "Player3";
            state.Players = players;

            // Player 0 fires, player 1 is primary target, player 2 is chain target
            state.Players[0].Position = new Vec2(-10f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(0f, 5f);
            state.Players[2].Position = new Vec2(4f, 5f); // within ChainRange (6) of player 1

            float health1Before = state.Players[1].Health;
            float health2Before = state.Players[2].Health;

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.Less(state.Players[1].Health, health1Before,
                "Primary target should take damage");
            Assert.Less(state.Players[2].Health, health2Before,
                "Chain target should take damage");
            Assert.AreEqual(2, state.HitscanEvents[0].ChainTargetIndex,
                "Chain should hit player 2");
        }

        [Test]
        public void LightningRod_ChainRange_UsesBodyCenter()
        {
            // Regression: #261 — chain range was measured from primary body-center
            // to chain candidate's foot position, making range inconsistent by ±0.5
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            var players = new PlayerState[3];
            players[0] = state.Players[0];
            players[1] = state.Players[1];
            players[2] = state.Players[1];
            players[2].Name = "Player3";
            state.Players = players;

            // Player 0 fires at player 1 (primary). Player 2 is exactly at
            // ChainRange (6f) from player 1's body-center (pos + 0.5 up).
            // Place player 2 higher so foot-to-center distance > 6 but
            // center-to-center distance <= 6.
            state.Players[0].Position = new Vec2(-10f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(0f, 5f);
            // Player 1 body center = (0, 5.5). Place player 2 at y=10.8 so
            // body center = (5, 11.3). Distance = sqrt(25 + 33.64) = ~7.66 > 6.
            // Actually we want a case where body-to-body is within range but
            // body-to-foot is out of range. Player 2 higher => foot further from
            // primary center. Let's use: p2 at (0, 11.2).
            // Primary center = (0, 5.5), p2 foot = (0, 11.2), p2 center = (0, 11.7).
            // body-to-body = 6.2 > 6 (out of range either way). Too far.
            // Instead: place p2 so center-to-center ≈ 5.9 but center-to-foot ≈ 6.3.
            // Primary center = (0, 5.5). Want p2 center at distance 5.9 directly up:
            // p2 center y = 5.5 + 5.9 = 11.4, foot y = 10.9.
            // center-to-foot = |5.5 - 10.9| = 5.4. That's within range too.
            // Use horizontal offset: p2 at (5.8, 5f). foot = (5.8, 5), center = (5.8, 5.5).
            // center-to-center = 5.8, center-to-foot = sqrt(5.8^2 + 0.5^2) = ~5.82.
            // Both within 6. Need bigger gap.
            // p2 at (5.9, 4.5). foot=(5.9,4.5), center=(5.9,5.0).
            // primary center=(0,5.5). To foot: sqrt(34.81+1) = sqrt(35.81) = 5.98.
            // To center: sqrt(34.81+0.25) = sqrt(35.06) = 5.92. Both within 6.
            // p2 at (5.9, 4f). foot=(5.9,4), center=(5.9,4.5).
            // To foot: sqrt(34.81+2.25) = sqrt(37.06) = 6.09 > 6 (OUT of range).
            // To center: sqrt(34.81+1) = sqrt(35.81) = 5.98 < 6 (IN range).
            // This is the case: old code (foot) would miss, fixed code (center) should hit.
            state.Players[2].Position = new Vec2(5.9f, 4f);

            float health2Before = state.Players[2].Health;

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.Less(state.Players[2].Health, health2Before,
                "Chain target at body-center distance 5.98 should be in range (6f)");
        }

        [Test]
        public void LightningRod_StopsAtTerrain()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Player 0 aims downward into terrain, player 1 is behind/below
            state.Players[0].Position = new Vec2(0f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(0f, -5f); // below terrain

            float healthBefore = state.Players[1].Health;

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = -45f; // aim downward into terrain
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(healthBefore, state.Players[1].Health, 0.01f,
                "Hitscan should stop at terrain, not hit player behind it");
            Assert.AreEqual(-1, state.HitscanEvents[0].PrimaryTargetIndex,
                "No player should be hit when terrain blocks");
        }

        [Test]
        public void LightningRod_SkipsInvulnerableTargets()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(5f, 5f);
            state.Players[1].IsInvulnerable = true;

            float healthBefore = state.Players[1].Health;

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(healthBefore, state.Players[1].Health, 0.01f,
                "Hitscan should not damage invulnerable targets");
            Assert.AreEqual(-1, state.HitscanEvents[0].PrimaryTargetIndex,
                "Invulnerable target should not register as hit");
        }

        [Test]
        public void LightningRod_ShieldAbsorption()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(5f, 5f);
            state.Players[1].FacingDirection = -1;
            state.Players[1].ShieldHP = 100f;
            state.Players[1].MaxShieldHP = 100f;

            float healthBefore = state.Players[1].Health;
            float shieldBefore = state.Players[1].ShieldHP;

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.Less(state.Players[1].ShieldHP, shieldBefore,
                "Shield should absorb hitscan damage");
            Assert.AreEqual(healthBefore, state.Players[1].Health, 0.01f,
                "Health should be unchanged when shield absorbs full damage");
        }

        [Test]
        public void LightningRod_ShieldDoesNotAbsorbOverheadHit()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Place player 0 directly above player 1 (dx=0)
            state.Players[0].Position = new Vec2(5f, 10f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(5f, 5f);
            state.Players[1].FacingDirection = 1;
            state.Players[1].ShieldHP = 100f;
            state.Players[1].MaxShieldHP = 100f;

            float shieldBefore = state.Players[1].ShieldHP;

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = -90f; // aim straight down
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(shieldBefore, state.Players[1].ShieldHP, 0.01f,
                "Shield should NOT absorb overhead hitscan hit (dx=0 is not frontal)");
        }

        [Test]
        public void LightningRod_ChainHitDoesNotInflateDirectHits()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // 3 players: 0 fires, 1 is primary, 2 is chain target
            var players = new PlayerState[3];
            players[0] = state.Players[0];
            players[1] = state.Players[1];
            players[2] = state.Players[1];
            players[2].Name = "Player3";
            state.Players = players;

            state.Players[0].Position = new Vec2(-10f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(0f, 5f);
            state.Players[2].Position = new Vec2(4f, 5f); // within ChainRange

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            state.Players[0].DirectHits = 0;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(1, state.Players[0].DirectHits,
                "Chain hit should not increment DirectHits — only primary hit counts");
        }

        [Test]
        public void LightningRod_PrimaryHit_CallsTrackHit()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(5f, 5f);

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(1, state.Players[0].ConsecutiveHits,
                "Hitscan primary hit should call TrackHit, incrementing ConsecutiveHits");
        }

        [Test]
        public void LightningRod_Kill_CallsTrackKill()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(5f, 5f);
            state.Players[1].Health = 1f; // will die from hitscan hit

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.IsTrue(state.Players[1].IsDead, "Target should be dead");
            Assert.AreEqual(1, state.Players[0].KillsInWindow,
                "Hitscan kill should call TrackKill, incrementing KillsInWindow");
        }

        [Test]
        public void LightningRod_ChainHit_CallsTrackHit()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            var players = new PlayerState[3];
            players[0] = state.Players[0];
            players[1] = state.Players[1];
            players[2] = state.Players[1];
            players[2].Name = "Player3";
            state.Players = players;

            state.Players[0].Position = new Vec2(-10f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[1].Position = new Vec2(0f, 5f);
            state.Players[2].Position = new Vec2(4f, 5f);

            state.Players[0].ActiveWeaponSlot = 14;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(2, state.Players[0].ConsecutiveHits,
                "Hitscan primary + chain hit should call TrackHit twice");
        }
    }
}
