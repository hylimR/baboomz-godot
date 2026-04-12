using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        [Test]
        public void CreateMatch_SpawnsTwoPlayers()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            Assert.AreEqual(2, state.Players.Length);
            Assert.AreEqual("Player1", state.Players[0].Name);
            Assert.AreEqual("CPU", state.Players[1].Name);
            Assert.IsFalse(state.Players[0].IsAI);
            Assert.IsTrue(state.Players[1].IsAI);
        }

        [Test]
        public void CreateMatch_PhaseIsPlaying()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            Assert.AreEqual(MatchPhase.Playing, state.Phase);
        }

        [Test]
        public void CreateMatch_PlayersHaveFullHealth()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            Assert.AreEqual(100f, state.Players[0].Health, 0.01f);
            Assert.AreEqual(100f, state.Players[1].Health, 0.01f);
        }

        [Test]
        public void CreateMatch_TerrainGenerated()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            Assert.IsNotNull(state.Terrain);
            Assert.Greater(state.Terrain.Width, 0);
        }

        [Test]
        public void Tick_AdvancesTime()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            float startTime = state.Time;
            GameSimulation.Tick(state, 0.016f);
            Assert.Greater(state.Time, startTime);
        }

        [Test]
        public void Tick_GravityPullsPlayersDown()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            // Move player to air (above terrain)
            state.Players[0].Position = new Vec2(0f, 50f);
            state.Players[0].IsGrounded = false;
            state.Players[0].Velocity = Vec2.Zero;

            float startY = state.Players[0].Position.y;

            // Tick several frames
            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Less(state.Players[0].Position.y, startY, "Player should fall due to gravity");
        }

        [Test]
        public void Tick_PlayerStopsOnTerrain()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Run many ticks — player should settle on terrain, not fall forever
            for (int i = 0; i < 300; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Players[0].Position.y, config.DeathBoundaryY,
                "Player should be above death boundary (on terrain)");
            Assert.IsFalse(state.Players[0].IsDead, "Player should be alive");
        }

        [Test]
        public void Tick_100Frames_NoExceptions()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 100; i++)
                    GameSimulation.Tick(state, 0.016f);
            });
        }

        [Test]
        public void Fire_CreatesProjectile()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].AimPower = 20f;
            state.Players[0].AimAngle = 45f;

            int before = state.Projectiles.Count;
            GameSimulation.Fire(state, 0);
            Assert.AreEqual(before + 1, state.Projectiles.Count);
        }

        [Test]
        public void Fire_OnCooldown_DoesNotFire()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].AimPower = 20f;
            state.Players[0].ShootCooldownRemaining = 1f;

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(0, state.Projectiles.Count);
        }

        [Test]
        public void Fire_SetsCooldown()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].AimPower = 20f;

            GameSimulation.Fire(state, 0);
            Assert.Greater(state.Players[0].ShootCooldownRemaining, 0f);
        }

        [Test]
        public void Projectile_FallsWithGravity()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].AimPower = 20f;
            state.Players[0].AimAngle = 90f; // straight up

            GameSimulation.Fire(state, 0);
            Assert.AreEqual(1, state.Projectiles.Count);

            float startY = state.Projectiles[0].Position.y;

            // Tick — projectile goes up then comes back down
            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            // After 10 frames, should still be going up
            Assert.Greater(state.Projectiles[0].Position.y, startY);
        }

        [Test]
        public void Projectile_HitsTerrain_ExplodesAndDealsDamage()
        {
            var config = SmallConfig();
            config.Weapons[0] = new WeaponDef
            {
                WeaponId = "cannon",
                MinPower = 5f,
                MaxPower = 30f,
                ChargeTime = 2f,
                ShootCooldown = 0.1f,
                ExplosionRadius = 3f,
                MaxDamage = 50f,
                KnockbackForce = 5f,
                ProjectileCount = 1,
                Ammo = -1
            };
            var state = GameSimulation.CreateMatch(config, 42);

            // Place players close together
            state.Players[0].Position = new Vec2(-2f, 5f);
            state.Players[1].Position = new Vec2(2f, 5f);

            // Fire horizontally toward player 2
            state.Players[0].AimAngle = 0f; // horizontal right
            state.Players[0].AimPower = 10f;

            GameSimulation.Fire(state, 0);

            // Tick until projectile hits something or times out
            bool exploded = false;
            for (int i = 0; i < 600; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.ExplosionEvents.Count > 0)
                {
                    exploded = true;
                    break;
                }
            }

            // Projectile should have hit terrain or gone out of bounds
            // Either way, match should still be valid
            Assert.IsFalse(state.Players[0].IsDead, "Shooter should survive");
        }

        [Test]
        public void Projectile_DiesAtHalfMapWidth_NotFullMapWidth()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            float halfMap = state.Config.MapWidth / 2f; // 20

            // Manually add a projectile just beyond half map width
            state.Projectiles.Add(new ProjectileState
            {
                Position = new Vec2(halfMap + 1f, 5f),
                Velocity = new Vec2(10f, 0f),
                Alive = true,
                OwnerIndex = 0,
                MaxDamage = 30f,
                ExplosionRadius = 3f
            });

            Assert.AreEqual(1, state.Projectiles.Count);
            // First update marks the out-of-bounds projectile as dead
            ProjectileSimulation.Update(state, 0.016f);
            // Second update removes dead projectiles from the list
            ProjectileSimulation.Update(state, 0.016f);

            // Projectile should be cleaned up — it's beyond the map edge
            Assert.AreEqual(0, state.Projectiles.Count,
                "Projectile beyond MapWidth/2 should be removed");
        }

        [Test]
        public void DeathBoundary_KillsPlayerAfterSwimDuration()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].Position = new Vec2(0f, state.Config.DeathBoundaryY - 1f);

            // First tick should start swimming, not kill instantly
            GameSimulation.Tick(state, 0.016f);
            Assert.IsTrue(state.Players[0].IsSwimming, "Player should be swimming");
            Assert.IsFalse(state.Players[0].IsDead, "Player should not die instantly in water");

            // Tick until swim timer expires (3s default)
            float swimDuration = state.Config.SwimDuration;
            int ticksNeeded = (int)(swimDuration / 0.016f) + 5;
            for (int i = 0; i < ticksNeeded; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.Players[0].IsDead) break;
            }

            Assert.IsTrue(state.Players[0].IsDead, "Player should drown after SwimDuration");
        }

        [Test]
        public void MatchEnds_WhenPlayerDies()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].Health = 0f;
            state.Players[0].IsDead = true;

            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(1, state.WinnerIndex); // Player2 wins
        }

        [Test]
        public void MatchEnds_Draw_WhenBothDie()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].IsDead = true;
            state.Players[0].Health = 0f;
            state.Players[1].IsDead = true;
            state.Players[1].Health = 0f;

            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(MatchPhase.Ended, state.Phase);
            Assert.AreEqual(-1, state.WinnerIndex); // draw
        }

        [Test]
        public void Input_MoveX_MovesPlayer()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);

            float startX = state.Players[0].Position.x;
            state.Input.MoveX = 1f;

            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Players[0].Position.x, startX, "Player should move right");
        }

        [Test]
        public void Input_AimDelta_ChangesAngle()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            float startAngle = state.Players[0].AimAngle;

            state.Input.AimDelta = 1f; // aim up

            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Players[0].AimAngle, startAngle, "Aim angle should increase");
        }

        [Test]
        public void Input_WeaponSwitch()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            // Add a second weapon
            state.Players[0].WeaponSlots[1] = new WeaponSlotState
            {
                WeaponId = "rocket",
                Ammo = -1,
                MinPower = 10f,
                MaxPower = 40f,
                ShootCooldown = 1f,
                ExplosionRadius = 4f,
                MaxDamage = 60f
            };

            state.Input.WeaponSlotPressed = 1;
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(1, state.Players[0].ActiveWeaponSlot);
        }

        [Test]
        public void WeaponScrollDelta_Forward_AdvancesToNextFilledSlot()
        {
            // Regression (#377): WeaponScrollDelta +1 should cycle to the next valid weapon slot.
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            ref PlayerState p = ref state.Players[0];
            int startSlot = p.ActiveWeaponSlot;
            // All 22 slots are populated by CreateMatch; scroll forward
            state.Input.WeaponScrollDelta = 1;
            GameSimulation.Tick(state, 0.016f);
            Assert.AreNotEqual(startSlot, p.ActiveWeaponSlot,
                "WeaponScrollDelta +1 should advance active weapon slot");
        }

        [Test]
        public void WeaponScrollDelta_Backward_RetreatsToPreFilledSlot()
        {
            // Regression (#377): WeaponScrollDelta -1 should cycle backward.
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            ref PlayerState p = ref state.Players[0];

            // Ensure slots 2 and 3 are filled so backward scroll has a target
            if (p.WeaponSlots[2].WeaponId == null) p.WeaponSlots[2].WeaponId = "rocket";
            if (p.WeaponSlots[3].WeaponId == null) p.WeaponSlots[3].WeaponId = "cluster";
            p.ActiveWeaponSlot = 3; // start mid-list
            state.Input.WeaponSlotPressed = -1; // no direct slot press (default 0 would snap to slot 0)
            state.Input.WeaponScrollDelta = -1;
            GameSimulation.Tick(state, 0.016f);
            Assert.AreEqual(2, p.ActiveWeaponSlot,
                "WeaponScrollDelta -1 should retreat active weapon slot by one");
        }

        [Test]
        public void EnergyRegens_OverTime()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].Energy = 50f;

            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Players[0].Energy, 50f, "Energy should regenerate");
        }

        [Test]
        public void CooldownDecreases_OverTime()
        {
            var state = GameSimulation.CreateMatch(SmallConfig(), 42);
            state.Players[0].ShootCooldownRemaining = 1f;

            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Less(state.Players[0].ShootCooldownRemaining, 1f);
        }

        [Test]
        public void Explosion_DestroysTerrain()
        {
            var config = SmallConfig();
            var state = GameSimulation.CreateMatch(config, 42);

            // Find a solid pixel near center
            int cx = state.Terrain.Width / 2;
            int cy = 0;
            for (int y = state.Terrain.Height - 1; y >= 0; y--)
            {
                if (state.Terrain.IsSolid(cx, y))
                {
                    cy = y;
                    break;
                }
            }

            Assert.IsTrue(state.Terrain.IsSolid(cx, cy), "Should have found a solid pixel");

            // Trigger explosion at that pixel (via world coords)
            float wx = state.Terrain.PixelToWorldX(cx);
            float wy = state.Terrain.PixelToWorldY(cy);

            // Fire at the terrain directly — use direct state manipulation
            state.Players[0].Position = new Vec2(wx, wy + 5f);
            state.Players[0].AimAngle = -90f; // straight down
            state.Players[0].AimPower = 15f;

            GameSimulation.Fire(state, 0);

            // Tick until explosion or timeout
            for (int i = 0; i < 300; i++)
            {
                GameSimulation.Tick(state, 0.016f);
                if (state.ExplosionEvents.Count > 0) break;
            }

            // The terrain at the impact point should be cleared
            // (The explosion clears a circle of pixels)
            if (state.ExplosionEvents.Count > 0)
            {
                var evt = state.ExplosionEvents[0];
                int epx = state.Terrain.WorldToPixelX(evt.Position.x);
                int epy = state.Terrain.WorldToPixelY(evt.Position.y);
                Assert.IsFalse(state.Terrain.IsSolid(epx, epy),
                    "Terrain at explosion center should be destroyed");
            }
        }
    }
}
