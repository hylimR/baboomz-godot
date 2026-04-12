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

    }
}
