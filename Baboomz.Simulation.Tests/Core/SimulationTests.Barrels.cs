using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Oil barrel tests ---

        [Test]
        public void Barrels_SpawnOnTerrain()
        {
            var config = SmallConfig();
            config.BarrelCount = 3;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.AreEqual(3, state.Barrels.Count);
            foreach (var barrel in state.Barrels)
            {
                Assert.IsTrue(barrel.Active);
                Assert.Greater(barrel.ExplosionRadius, 0f);
            }
        }

        [Test]
        public void Barrels_SkipSpawnWhenNoGroundAtX()
        {
            var config = SmallConfig();
            config.TerrainWidth = 20; // very narrow terrain
            config.MapWidth = 200f;   // wide map — most X samples miss terrain
            config.MineCount = 0;
            config.BarrelCount = 10;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.LessOrEqual(state.Barrels.Count, config.BarrelCount);
            foreach (var barrel in state.Barrels)
            {
                Assert.That(barrel.Position.y, Is.Not.EqualTo(config.SpawnProbeY).Within(0.3f),
                    "Barrel should not spawn at SpawnProbeY fallback height");
            }
        }

        [Test]
        public void Barrels_ExplodeWhenHitByExplosion()
        {
            var config = SmallConfig();
            config.BarrelCount = 0; // no auto-spawn
            config.MineCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place a barrel at a known position
            Vec2 barrelPos = new Vec2(5f, 5f);
            state.Barrels.Add(new BarrelState
            {
                Position = barrelPos,
                ExplosionRadius = 3f,
                Damage = 40f,
                Active = true
            });

            // Fire a projectile at the barrel
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(4.5f, 5f),
                Velocity = new Vec2(2f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 2f,
                MaxDamage = 10f,
                KnockbackForce = 5f,
                Alive = true
            });

            // Build a terrain wall to force impact near the barrel
            int wallPx = state.Terrain.WorldToPixelX(5f);
            int wallPy = state.Terrain.WorldToPixelY(5f);
            for (int py = wallPy - 5; py < wallPy + 5; py++)
                state.Terrain.SetSolid(wallPx, py, true);

            // Tick until explosion (barrel removed from list when deactivated)
            bool barrelExploded = false;
            for (int i = 0; i < 300; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Barrels.Count == 0)
                {
                    barrelExploded = true;
                    break;
                }
            }

            Assert.IsTrue(barrelExploded, "Barrel should explode when hit by nearby explosion");
        }

        [Test]
        public void Barrels_Disabled_WhenCountIsZero()
        {
            var config = SmallConfig();
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.AreEqual(0, state.Barrels.Count);
        }

        [Test]
        public void Barrels_ChainReaction_TwoBarrelsNearby()
        {
            var config = SmallConfig();
            config.BarrelCount = 0;
            config.MineCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Place two barrels close together
            state.Barrels.Add(new BarrelState
            {
                Position = new Vec2(0f, 5f),
                ExplosionRadius = 3f,
                Damage = 40f,
                Active = true
            });
            state.Barrels.Add(new BarrelState
            {
                Position = new Vec2(2f, 5f), // within blast radius of first
                ExplosionRadius = 3f,
                Damage = 40f,
                Active = true
            });

            // Place a projectile that will hit terrain near the first barrel to trigger it
            // Build a terrain wall at barrel position
            int wallPx = state.Terrain.WorldToPixelX(0f);
            int wallPy = state.Terrain.WorldToPixelY(5f);
            for (int py = wallPy - 3; py <= wallPy + 3; py++)
                state.Terrain.SetSolid(wallPx, py, true);

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(-1f, 5f),
                Velocity = new Vec2(5f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 2f,
                MaxDamage = 10f,
                KnockbackForce = 5f,
                Alive = true
            });

            // Tick until chain reaction propagates (both barrels removed when deactivated)
            for (int i = 0; i < 60; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Barrels.Count == 0) break;
            }

            Assert.AreEqual(0, state.Barrels.Count, "Both barrels should explode and be removed (chain reaction)");
        }

        [Test]
        public void Barrels_ChainReaction_IndirectPropagation_ThreeBarrels()
        {
            var config = SmallConfig();
            config.BarrelCount = 0;
            config.MineCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // B1 at x=0, B2 at x=4, B3 at x=8 — each radius 5
            // Projectile explosion (radius 2) at x=0 hits B1 only
            // B1 explosion (radius 5) reaches B2 (dist 4 < 5.5) but not B3 (dist 8 > 5.5)
            // B2 explosion (radius 5) reaches B3 (dist 4 < 5.5)
            // B3 is ONLY reachable through indirect chain: projectile → B1 → B2 → B3
            state.Barrels.Add(new BarrelState
            {
                Position = new Vec2(0f, 5f),
                ExplosionRadius = 5f,
                Damage = 40f,
                Active = true
            });
            state.Barrels.Add(new BarrelState
            {
                Position = new Vec2(4f, 5f),
                ExplosionRadius = 5f,
                Damage = 40f,
                Active = true
            });
            state.Barrels.Add(new BarrelState
            {
                Position = new Vec2(8f, 5f),
                ExplosionRadius = 5f,
                Damage = 40f,
                Active = true
            });

            // Build terrain wall at B1 to force projectile impact
            int wallPx = state.Terrain.WorldToPixelX(0f);
            int wallPy = state.Terrain.WorldToPixelY(5f);
            for (int py = wallPy - 3; py <= wallPy + 3; py++)
                state.Terrain.SetSolid(wallPx, py, true);

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(-1f, 5f),
                Velocity = new Vec2(5f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 2f,
                MaxDamage = 10f,
                KnockbackForce = 5f,
                Alive = true
            });

            // Tick until all three barrels have detonated (removed when deactivated)
            for (int i = 0; i < 60; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Barrels.Count == 0) break;
            }

            Assert.AreEqual(0, state.Barrels.Count, "All 3 barrels should chain-react and be removed (indirect propagation)");
        }

        [Test]
        public void Barrels_ChainKill_AttributedToTriggeringPlayer()
        {
            var config = SmallConfig();
            config.BarrelCount = 0;
            config.MineCount = 0;
            config.SuddenDeathTime = 0f;

            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            // Disable AI so it doesn't fire stray projectiles that interfere
            state.Players[1].IsAI = false;

            // Move player 1 next to where the barrel will be
            state.Players[1].Position = new Vec2(5f, 5f);
            state.Players[1].Health = 10f; // low HP so barrel kills them

            // Place a barrel next to player 1
            state.Barrels.Add(new BarrelState
            {
                Position = new Vec2(5f, 5.5f),
                ExplosionRadius = 3f,
                Damage = 50f,
                Active = true,
                OwnerIndex = -1
            });

            float dmgBefore = state.Players[0].TotalDamageDealt;

            // Player 0 fires a projectile that hits terrain near the barrel.
            // Build a tall wall so the projectile hits even after gravity pulls it down.
            int wallPx = state.Terrain.WorldToPixelX(5f);
            int wallPy = state.Terrain.WorldToPixelY(5f);
            for (int py = wallPy - 20; py <= wallPy + 3; py++)
                state.Terrain.SetSolid(wallPx, py, true);

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(3f, 5f),
                Velocity = new Vec2(5f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 2f,
                MaxDamage = 5f,
                KnockbackForce = 3f,
                Alive = true
            });

            // Tick until barrel explodes and player 1 dies (barrel removed when deactivated)
            for (int i = 0; i < 120; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Barrels.Count == 0 && state.Players[1].IsDead) break;
            }

            Assert.AreEqual(0, state.Barrels.Count, "Barrel should have exploded and been removed");
            Assert.IsTrue(state.Players[1].IsDead, "Player 1 should be dead from barrel");
            Assert.Greater(state.Players[0].TotalDamageDealt, dmgBefore,
                "Player 0 should get damage credit for barrel chain kill");
        }

    }
}
