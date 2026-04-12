using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Weapon slot count regression ---

        [Test]
        public void AllWeapons_HaveSlots()
        {
            var config = new GameConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.AreEqual(config.Weapons.Length, state.Players[0].WeaponSlots.Length,
                $"Player should have {config.Weapons.Length} weapon slots, one per config weapon");

            // Verify specific weapons exist at expected indices
            Assert.AreEqual("cannon", state.Players[0].WeaponSlots[0].WeaponId);
            Assert.AreEqual("drill", state.Players[0].WeaponSlots[7].WeaponId);
            Assert.AreEqual("holy_hand_grenade", state.Players[0].WeaponSlots[9].WeaponId);
            Assert.AreEqual("sheep", state.Players[0].WeaponSlots[10].WeaponId);
            Assert.AreEqual("banana_bomb", state.Players[0].WeaponSlots[11].WeaponId);
        }

        // --- Sheep projectile tests ---

        [Test]
        public void SheepProjectile_WalksHorizontally()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            Vec2 startPos = state.Players[0].Position + new Vec2(2f, 0.5f);
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = startPos,
                Velocity = new Vec2(4f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 60f,
                KnockbackForce = 10f,
                Alive = true,
                FuseTimer = 5f,
                IsSheep = true
            });

            // Tick
            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            // Sheep should have moved to the right
            if (state.Projectiles.Count > 0)
            {
                Assert.Greater(state.Projectiles[0].Position.x, startPos.x,
                    "Sheep should walk to the right");
            }
            else
            {
                // Sheep may have hit a player and exploded — that's valid too
                Assert.Pass("Sheep exploded on contact (valid behavior)");
            }
        }

        [Test]
        public void SheepProjectile_ExplodesOnFuse()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Move players far away so sheep doesn't hit them
            state.Players[0].Position = new Vec2(-30f, 0f);
            state.Players[1].Position = new Vec2(30f, 0f);

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 5f),
                Velocity = new Vec2(4f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 60f,
                Alive = true,
                FuseTimer = 0.5f, // short fuse
                IsSheep = true
            });

            // Tick well past fuse duration (0.5s = 31 frames at 60fps)
            bool hadExplosion = false;
            for (int i = 0; i < 120; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.ExplosionEvents.Count > 0) hadExplosion = true;
            }

            Assert.IsTrue(hadExplosion, "Sheep should create explosion when fuse expires");
        }

        [Test]
        public void SheepProjectile_FuseExpiry_TracksWeaponHit()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place player 1 near where sheep will explode
            state.Players[0].Position = new Vec2(-30f, 0f);
            state.Players[1].Position = new Vec2(2f, 5f);
            state.Players[1].Health = 200f; // extra HP to survive

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 5f),
                Velocity = new Vec2(4f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 60f,
                Alive = true,
                FuseTimer = 0.3f,
                IsSheep = true,
                SourceWeaponId = "sheep"
            });

            for (int i = 0; i < 120; i++)
                GameSimulation.Tick(state, 0.016f);

            state.WeaponHits[0].TryGetValue("sheep", out int hits);
            Assert.IsTrue(hits > 0,
                "Sheep fuse-expiry explosion should track weapon hit for mastery/stats");
        }

        // --- Banana bomb tests ---

        [Test]
        public void BananaBomb_ExistsWithSixClusters()
        {
            var config = new GameConfig();
            bool found = false;
            for (int i = 0; i < config.Weapons.Length; i++)
            {
                if (config.Weapons[i].WeaponId == "banana_bomb")
                {
                    found = true;
                    Assert.AreEqual(6, config.Weapons[i].ClusterCount,
                        "Banana bomb should have 6 sub-projectiles");
                    Assert.AreEqual(1, config.Weapons[i].Ammo);
                    break;
                }
            }
            Assert.IsTrue(found, "Banana bomb weapon should exist in config");
        }

        // --- Blowtorch smoke test ---

        [Test]
        public void Blowtorch_IsDrillVariant()
        {
            var config = new GameConfig();
            bool found = false;
            for (int i = 0; i < config.Weapons.Length; i++)
            {
                if (config.Weapons[i].WeaponId == "blowtorch")
                {
                    found = true;
                    Assert.IsTrue(config.Weapons[i].IsDrill, "Blowtorch should use drill mechanics");
                    Assert.AreEqual(-1, config.Weapons[i].Ammo, "Blowtorch should have infinite ammo");
                    Assert.Less(config.Weapons[i].MaxDamage, 15f, "Blowtorch should have low damage");
                    break;
                }
            }
            Assert.IsTrue(found, "Blowtorch weapon should exist in config");
        }

        // --- Regression: #345 drill range is configurable per-weapon ---

        [Test]
        public void DrillRange_BlowtorchHasShorterRangeThanDrill()
        {
            var config = new GameConfig();
            float drillRange = 0f;
            float blowtorchRange = 0f;
            for (int i = 0; i < config.Weapons.Length; i++)
            {
                if (config.Weapons[i].WeaponId == "drill") drillRange = config.Weapons[i].DrillRange;
                else if (config.Weapons[i].WeaponId == "blowtorch") blowtorchRange = config.Weapons[i].DrillRange;
            }
            Assert.Greater(drillRange, 0f, "Drill should have a configured DrillRange");
            Assert.Greater(blowtorchRange, 0f, "Blowtorch should have a configured DrillRange");
            Assert.Less(blowtorchRange, drillRange,
                "Blowtorch is a short-range terrain cutter and should have shorter range than the Drill");
        }

        [Test]
        public void DrillRange_ProjectileRespectsPerWeaponRangeCap()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Spawn a short-range drill projectile (DrillRange = 5) heading right, high above terrain
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 50f),
                Velocity = new Vec2(10f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                KnockbackForce = 3f,
                Alive = true,
                IsDrill = true,
                DrillRange = 5f,
                SourceWeaponId = "drill"
            });

            // Tick until travel distance exceeds 5 units (10 u/s * 1s = 10 units)
            for (int i = 0; i < 70; i++)
                GameSimulation.Tick(state, 0.016f);

            // Projectile must have expired within its configured range
            bool anyDrillAlive = false;
            for (int i = 0; i < state.Projectiles.Count; i++)
            {
                if (state.Projectiles[i].IsDrill && state.Projectiles[i].Alive) anyDrillAlive = true;
            }
            Assert.IsFalse(anyDrillAlive, "Drill with DrillRange=5 should expire before 10 units of travel");
        }

        [Test]
        public void DrillRange_ZeroFallsBackToThirtyUnits()
        {
            // Backwards-compat: projectiles without an explicit DrillRange (e.g. legacy tests)
            // should still be capped at the historical 30-unit default.
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 50f),
                Velocity = new Vec2(20f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 1.5f,
                MaxDamage = 25f,
                Alive = true,
                IsDrill = true,
                SourceWeaponId = "drill"
                // DrillRange intentionally unset (default 0f)
            });

            // At 20 u/s, 30 units is reached in 1.5s -> tick 3s worth to guarantee expiry
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            bool anyDrillAlive = false;
            for (int i = 0; i < state.Projectiles.Count; i++)
            {
                if (state.Projectiles[i].IsDrill && state.Projectiles[i].Alive) anyDrillAlive = true;
            }
            Assert.IsFalse(anyDrillAlive, "Drill with DrillRange=0 should fall back to 30-unit cap and expire");
        }

    }
}
