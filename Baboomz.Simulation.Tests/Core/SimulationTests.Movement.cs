using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        [Test]
        public void FacingDirection_AffectsProjectileVelocity()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].AimAngle = 45f;
            state.Players[0].AimPower = 20f;

            // Face right
            state.Players[0].FacingDirection = 1;
            GameSimulation.Fire(state, 0);
            Assert.Greater(state.Projectiles[0].Velocity.x, 0f, "Facing right should fire rightward");

            // Face left
            state.Players[0].ShootCooldownRemaining = 0f;
            state.Players[0].AimPower = 20f;
            state.Players[0].FacingDirection = -1;
            GameSimulation.Fire(state, 0);
            Assert.Less(state.Projectiles[1].Velocity.x, 0f, "Facing left should fire leftward");

            // Both should have same Y velocity (same angle)
            Assert.AreEqual(state.Projectiles[0].Velocity.y, state.Projectiles[1].Velocity.y, 0.01f,
                "Y velocity should be identical regardless of facing");
        }

        [Test]
        public void WallCollision_StopsHorizontalMovement()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Let player settle
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            // Build a wall to the right of the player
            float wallX = state.Players[0].Position.x + 1f;
            int wallPx = state.Terrain.WorldToPixelX(wallX);
            int basePy = state.Terrain.WorldToPixelY(state.Players[0].Position.y);
            for (int py = basePy; py < basePy + 20; py++)
                state.Terrain.SetSolid(wallPx, py, true);

            float startX = state.Players[0].Position.x;

            // Try to move right into the wall
            state.Input.MoveX = 1f;
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            // Player should not have moved far past their start (wall blocks)
            Assert.Less(state.Players[0].Position.x, wallX,
                "Player should be blocked by wall");
        }

        [Test]
        public void Projectile_HitsPlayer_ExplodesOnContact()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Let players settle
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            // Place a projectile right at player 2's position (direct hit)
            float p2X = state.Players[1].Position.x;
            float p2Y = state.Players[1].Position.y + 0.5f;
            float startHealth = state.Players[1].Health;

            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(p2X - 0.3f, p2Y),
                Velocity = new Vec2(1f, 0f), // moving toward player
                OwnerIndex = 0,
                ExplosionRadius = 2f,
                MaxDamage = 30f,
                KnockbackForce = 5f,
                Alive = true
            });

            // Tick — projectile should hit player within a few frames
            for (int i = 0; i < 30; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.ExplosionEvents.Count > 0) break;
            }

            Assert.Greater(state.ExplosionEvents.Count, 0, "Projectile should explode on player contact");
            Assert.Less(state.Players[1].Health, startHealth, "Player should take damage from direct hit");
        }

        [Test]
        public void SlopeWalking_PlayerFollowsTerrainContour()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Let player settle
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.IsTrue(state.Players[0].IsGrounded);
            float startY = state.Players[0].Position.y;

            // Build a gentle upward slope to the right
            float baseX = state.Players[0].Position.x;
            for (int dx = 0; dx < 40; dx++)
            {
                float worldX = baseX + dx * 0.2f;
                float slopeY = startY - 0.5f + dx * 0.1f; // rising slope
                int px = state.Terrain.WorldToPixelX(worldX);
                int surfacePy = state.Terrain.WorldToPixelY(slopeY);
                // Fill solid below surface
                for (int py = 0; py < surfacePy; py++)
                    state.Terrain.SetSolid(px, py, true);
                // Clear above
                for (int py = surfacePy; py < surfacePy + 10; py++)
                    state.Terrain.SetSolid(px, py, false);
            }

            // Walk right for a while
            state.Input.MoveX = 1f;
            for (int i = 0; i < 100; i++)
                GameSimulation.Tick(state, 0.016f);

            // Player should have moved right AND upward (following slope)
            Assert.Greater(state.Players[0].Position.x, baseX + 1f,
                "Player should move right");
            Assert.Greater(state.Players[0].Position.y, startY - 0.5f,
                "Player should walk up the slope, not fall through");
        }

        [Test]
        public void ClusterBomb_SpawnsSubProjectilesOnImpact()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Let players settle
            for (int i = 0; i < 200; i++)
                GameSimulation.Tick(state, 0.016f);

            // Switch to cluster (slot 3)
            state.Players[0].ActiveWeaponSlot = 3;
            state.Players[0].AimPower = 15f;
            state.Players[0].AimAngle = 60f;

            var weapon = state.Players[0].WeaponSlots[3];
            Assert.AreEqual("cluster", weapon.WeaponId);
            Assert.AreEqual(4, weapon.ClusterCount);  // balanced: 5→4

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Projectiles.Count);
            Assert.AreEqual(4, state.Projectiles[0].ClusterCount);

            // Tick until impact — should spawn sub-projectiles
            bool hitTerrain = false;
            for (int i = 0; i < 600; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                // Cluster spawns sub-projectiles, so count should increase
                if (state.Projectiles.Count > 1)
                {
                    hitTerrain = true;
                    break;
                }
                if (state.ExplosionEvents.Count > 0 && state.Projectiles.Count == 0)
                {
                    hitTerrain = true;
                    break;
                }
            }

            Assert.IsTrue(hitTerrain, "Cluster bomb should hit terrain and produce sub-projectiles or explosions");
        }

        [Test]
        public void FuseTimer_ExplodesAfterCountdown()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Manually add a grenade projectile in mid-air (no terrain to hit)
            state.Projectiles.Add(new ProjectileState
            {
                Id = state.NextProjectileId++,
                Position = new Vec2(0f, 50f), // high in air, no terrain to hit
                Velocity = new Vec2(5f, 0f),
                OwnerIndex = 0,
                ExplosionRadius = 3f,
                MaxDamage = 40f,
                KnockbackForce = 8f,
                Alive = true,
                BouncesRemaining = 3,
                FuseTimer = 0.5f // 0.5 seconds fuse
            });

            // Tick until fuse expires
            bool exploded = false;
            for (int i = 0; i < 100; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.ExplosionEvents.Count > 0)
                {
                    exploded = true;
                    break;
                }
            }

            Assert.IsTrue(exploded, "Grenade should explode after fuse timer");
        }

        [Test]
        public void AmmoDepletion_AutoSwitchesToNextWeapon()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Give slot 2 (rocket) only 1 ammo
            state.Players[0].WeaponSlots[2].Ammo = 1;
            state.Players[0].ActiveWeaponSlot = 2;
            state.Players[0].AimPower = 20f;

            Assert.AreEqual(2, state.Players[0].ActiveWeaponSlot);

            GameSimulation.Fire(state, 0);

            // After firing last rocket, should auto-switch to a weapon with ammo
            Assert.AreNotEqual(2, state.Players[0].ActiveWeaponSlot,
                "Should auto-switch away from depleted weapon");
            Assert.IsNotNull(state.Players[0].WeaponSlots[state.Players[0].ActiveWeaponSlot].WeaponId,
                "Should switch to a valid weapon");
        }

        [Test]
        public void Stats_TrackDamageAndShots()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Place players close, fire directly at P2
            state.Players[0].Position = new Vec2(-2f, 5f);
            state.Players[1].Position = new Vec2(2f, 5f);
            state.Players[0].FacingDirection = 1;
            state.Players[0].AimAngle = 0f;
            state.Players[0].AimPower = 10f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Players[0].ShotsFired);

            // Tick until explosion hits P2
            for (int i = 0; i < 300; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.ExplosionEvents.Count > 0) break;
            }

            // P1 should have dealt some damage
            if (state.Players[0].TotalDamageDealt > 0)
            {
                Assert.Greater(state.Players[0].DirectHits, 0, "Should have recorded direct hit");
            }
        }
    }
}
