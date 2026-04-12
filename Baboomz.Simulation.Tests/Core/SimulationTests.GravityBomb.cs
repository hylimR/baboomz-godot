using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        // --- Gravity Bomb weapon tests ---

        [Test]
        public void GravityBomb_ExistsInConfig()
        {
            var config = new GameConfig();
            Assert.IsTrue(config.Weapons.Length >= 17, "Should have at least 17 weapons");
            Assert.AreEqual("gravity_bomb", config.Weapons[16].WeaponId);
            Assert.AreEqual(65f, config.Weapons[16].MaxDamage);
            Assert.AreEqual(2, config.Weapons[16].Ammo);
            Assert.AreEqual(2.5f, config.Weapons[16].FuseTime);
            Assert.IsTrue(config.Weapons[16].IsSticky);
            Assert.IsTrue(config.Weapons[16].IsGravityBomb);
            Assert.AreEqual(6f, config.Weapons[16].PullRadius);
            Assert.AreEqual(9f, config.Weapons[16].PullForce);
        }

        [Test]
        public void GravityBomb_CreatesProjectileWithCorrectFlags()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Players[0].ActiveWeaponSlot = 16;
            state.Players[0].AimAngle = 45f;
            state.Players[0].AimPower = 15f;
            state.Players[0].Energy = 100f;
            GameSimulation.Fire(state, 0);

            Assert.AreEqual(1, state.Projectiles.Count);
            Assert.IsTrue(state.Projectiles[0].IsSticky);
            Assert.IsTrue(state.Projectiles[0].IsGravityBomb);
            Assert.AreEqual(6f, state.Projectiles[0].PullRadius, 0.01f);
            Assert.AreEqual(9f, state.Projectiles[0].PullForce, 0.01f);
        }

        [Test]
        public void GravityBomb_VortexPullsNearbyPlayer()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Place bomb high above terrain where LOS is clear
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 40f,
                KnockbackForce = 10f,
                Alive = true,
                FuseTimer = 2.5f,
                IsSticky = true,
                StuckToTerrain = true,
                StuckToPlayerId = -1,
                IsGravityBomb = true,
                PullRadius = 6f,
                PullForce = 3f
            });

            // Place player 1 within pull radius, high above terrain
            state.Players[1].Position = new Vec2(4f, 15f);
            state.Players[1].Velocity = Vec2.Zero;

            float startX = state.Players[1].Position.x;

            // Tick enough frames for pull to take noticeable effect
            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Less(state.Players[1].Position.x, startX,
                "Player should be pulled toward gravity bomb");
        }

        [Test]
        public void GravityBomb_DoesNotPullThroughTerrain()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Place bomb at a position
            Vec2 bombPos = new Vec2(0f, 5f);
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = bombPos,
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 40f,
                KnockbackForce = 10f,
                Alive = true,
                FuseTimer = 2.5f,
                IsSticky = true,
                StuckToTerrain = true,
                StuckToPlayerId = -1,
                IsGravityBomb = true,
                PullRadius = 6f,
                PullForce = 3f
            });

            // Place player 1 within pull radius but behind terrain
            // Fill terrain between bomb and player to block LOS
            state.Players[1].Position = new Vec2(4f, 5f);
            state.Players[1].Velocity = Vec2.Zero;
            int startPx = state.Terrain.WorldToPixelX(1f);
            int endPx = state.Terrain.WorldToPixelX(3f);
            int py = state.Terrain.WorldToPixelY(5.5f);
            for (int px = startPx; px <= endPx; px++)
                for (int dy = -10; dy <= 10; dy++)
                    state.Terrain.SetSolid(px, py + dy, true);

            float startX = state.Players[1].Position.x;

            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            // Player should NOT be significantly pulled (terrain blocks LOS)
            float moved = MathF.Abs(state.Players[1].Position.x - startX);
            // Allow some tolerance for gravity/physics but the vortex pull should be blocked
            Assert.Less(moved, 0.5f,
                "Player should not be pulled through solid terrain");
        }

        [Test]
        public void GravityBomb_ExplodesAfterFuse()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Disable AI so it doesn't fire stray projectiles that inflate the count
            state.Players[1].IsAI = false;

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 5f),
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 40f,
                KnockbackForce = 10f,
                Alive = true,
                FuseTimer = 0.1f, // very short fuse for test speed
                IsSticky = true,
                StuckToTerrain = true,
                StuckToPlayerId = -1,
                IsGravityBomb = true,
                PullRadius = 6f,
                PullForce = 3f
            });

            // Tick until fuse expires — check for explosion event each frame (events clear per tick)
            bool sawExplosion = false;
            for (int i = 0; i < 20; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.ExplosionEvents.Count > 0) sawExplosion = true;
                if (state.Projectiles.Count == 0) break;
            }
            // One more tick to clean up dead projectiles
            if (state.Projectiles.Count > 0)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(0, state.Projectiles.Count, "Gravity bomb should have exploded after fuse");
            Assert.IsTrue(sawExplosion, "Should have created an explosion event");
        }

        [Test]
        public void GravityBomb_PullsShooterToo()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            // Place bomb and put shooter within pull radius, high above terrain
            state.Players[0].Position = new Vec2(4f, 15f);
            state.Players[0].Velocity = Vec2.Zero;

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 40f,
                KnockbackForce = 10f,
                Alive = true,
                FuseTimer = 2.5f,
                IsSticky = true,
                StuckToTerrain = true,
                StuckToPlayerId = -1,
                IsGravityBomb = true,
                PullRadius = 6f,
                PullForce = 3f
            });

            float startX = state.Players[0].Position.x;

            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Less(state.Players[0].Position.x, startX,
                "Shooter should also be pulled toward their own gravity bomb");
        }

        [Test]
        public void GravityBomb_DoesNotPullOutsideRadius()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 5f),
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 40f,
                KnockbackForce = 10f,
                Alive = true,
                FuseTimer = 2.5f,
                IsSticky = true,
                StuckToTerrain = true,
                StuckToPlayerId = -1,
                IsGravityBomb = true,
                PullRadius = 6f,
                PullForce = 3f
            });

            // Place player well outside pull radius (>6 units away)
            state.Players[1].Position = new Vec2(10f, 5f);
            state.Players[1].Velocity = Vec2.Zero;

            float startX = state.Players[1].Position.x;

            // Tick a few frames — player should not be pulled
            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            float moved = MathF.Abs(state.Players[1].Position.x - startX);
            // Should not be pulled toward the bomb (may drift from normal physics but not toward bomb)
            Assert.Less(moved, 1f,
                "Player outside pull radius should not be significantly pulled");
        }

        [Test]
        public void GravityBomb_BuffedPullForceDisplacesFaster()
        {
            // Regression for #332: pull force buffed 5 -> 9 so the vortex functions
            // as a real setup tool. Uses the config's actual PullForce to guard
            // against future regressions.
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);
            state.Phase = MatchPhase.Playing;

            var weapon = new GameConfig().Weapons[16];
            Assert.AreEqual("gravity_bomb", weapon.WeaponId);

            // Bomb well above terrain so LOS is clear
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 15f),
                Velocity = Vec2.Zero,
                OwnerIndex = 0,
                ExplosionRadius = weapon.ExplosionRadius,
                MaxDamage = weapon.MaxDamage,
                KnockbackForce = weapon.KnockbackForce,
                Alive = true,
                FuseTimer = weapon.FuseTime,
                IsSticky = true,
                StuckToTerrain = true,
                StuckToPlayerId = -1,
                IsGravityBomb = true,
                PullRadius = weapon.PullRadius,
                PullForce = weapon.PullForce
            });

            // Stationary player 5 units from bomb — inside pull radius (6)
            state.Players[1].Position = new Vec2(5f, 15f);
            state.Players[1].Velocity = Vec2.Zero;
            float startX = state.Players[1].Position.x;

            // 20 ticks at 0.016s = 0.32s. Expected horizontal displacement:
            //   old PullForce=5 -> ~1.6 units
            //   new PullForce=9 -> ~2.88 units
            for (int i = 0; i < 20; i++)
                GameSimulation.Tick(state, 0.016f);

            float movedLeft = startX - state.Players[1].Position.x;
            Assert.Greater(movedLeft, 2.0f,
                "Buffed PullForce should displace a nearby player > 2 units in 0.32s; old 5f could not reach this.");
        }

    }
}
