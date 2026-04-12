using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {

        [Test]
        public void BiomeHazard_Firecracker_LaunchesPlayerUpward()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Settle players on terrain
            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Vec2 playerPos = state.Players[0].Position;
            state.Players[0].Velocity = Vec2.Zero;
            state.Players[0].IsGrounded = true;
            state.Players[0].FirecrackerCooldown = 0f;

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = playerPos,
                Radius = 5f,
                Type = BiomeHazardType.Firecracker,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(playerPos.x);
            int py = state.Terrain.WorldToPixelY(playerPos.y - 0.5f);
            state.Terrain.SetSolid(px, py, true);

            float yBefore = state.Players[0].Position.y;

            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Players[0].Position.y, yBefore + 0.5f,
                "Firecracker hazard should launch player upward");
        }

        [Test]
        public void BiomeHazard_Firecracker_DoesNoDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].IsGrounded = true;
            state.Players[0].FirecrackerCooldown = 0f;

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f),
                Radius = 5f,
                Type = BiomeHazardType.Firecracker,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            float healthBefore = state.Players[0].Health;
            state.DamageEvents.Clear();
            state.Phase = MatchPhase.Playing;

            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(healthBefore, state.Players[0].Health, 0.01f,
                "Firecracker hazard should deal no damage");
        }

        [Test]
        public void BiomeHazard_Firecracker_RespectsCooldown()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].IsGrounded = true;
            state.Players[0].FirecrackerCooldown = 1.0f; // still on cooldown

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f),
                Radius = 5f,
                Type = BiomeHazardType.Firecracker,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            float yBefore = state.Players[0].Velocity.y;
            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(yBefore, state.Players[0].Velocity.y, 0.01f,
                "Firecracker should not launch player while on cooldown");
        }

        [Test]
        public void ClockworkFoundry_BiomeExists_InAllArray()
        {
            bool found = false;
            for (int i = 0; i < TerrainBiome.All.Length; i++)
            {
                if (TerrainBiome.All[i].Name == "Clockwork Foundry")
                {
                    found = true;
                    Assert.AreEqual(BiomeHazardType.Gear, TerrainBiome.All[i].HazardType);
                    Assert.AreEqual(3, TerrainBiome.All[i].HazardCount);
                    break;
                }
            }
            Assert.IsTrue(found, "Clockwork Foundry biome should exist in TerrainBiome.All");
        }

        [Test]
        public void BiomeHazard_Gear_PushesPlayerHorizontally()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Vec2 playerPos = state.Players[0].Position;
            state.Players[0].Velocity = Vec2.Zero;

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = playerPos,
                Radius = 5f,
                Type = BiomeHazardType.Gear,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(playerPos.x);
            int py = state.Terrain.WorldToPixelY(playerPos.y - 0.5f);
            state.Terrain.SetSolid(px, py, true);

            // At time < 3s (first half of 6s cycle), push should be rightward (+x)
            state.Time = 1f;
            float vxBefore = state.Players[0].Velocity.x;

            GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Players[0].Velocity.x, vxBefore,
                "Gear hazard should push player to the right during first half of cycle");
        }

        [Test]
        public void BiomeHazard_Gear_AlternatesDirection()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < 60; i++)
                GameSimulation.Tick(state, 0.016f);

            Vec2 playerPos = state.Players[0].Position;

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = playerPos,
                Radius = 5f,
                Type = BiomeHazardType.Gear,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(playerPos.x);
            int py = state.Terrain.WorldToPixelY(playerPos.y - 0.5f);
            state.Terrain.SetSolid(px, py, true);

            // At time >= 3s (second half of 6s cycle), push should be leftward (-x)
            state.Time = 4f;
            state.Players[0].Velocity = Vec2.Zero;
            float vxBefore = state.Players[0].Velocity.x;

            GameSimulation.Tick(state, 0.016f);

            Assert.Less(state.Players[0].Velocity.x, vxBefore,
                "Gear hazard should push player to the left during second half of cycle");
        }

        [Test]
        public void BiomeHazard_Gear_DoesNoDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f),
                Radius = 5f,
                Type = BiomeHazardType.Gear,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            float healthBefore = state.Players[0].Health;
            state.DamageEvents.Clear();
            state.Phase = MatchPhase.Playing;
            state.Time = 1f;

            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(healthBefore, state.Players[0].Health, 0.01f,
                "Gear hazard should deal no damage");
        }

        [Test]
        public void SunkenRuins_BiomeExists_InAllArray()
        {
            bool found = false;
            for (int i = 0; i < TerrainBiome.All.Length; i++)
            {
                if (TerrainBiome.All[i].Name == "Sunken Ruins")
                {
                    found = true;
                    Assert.AreEqual(BiomeHazardType.Whirlpool, TerrainBiome.All[i].HazardType);
                    Assert.AreEqual(2, TerrainBiome.All[i].HazardCount);
                    break;
                }
            }
            Assert.IsTrue(found, "Sunken Ruins biome should exist in TerrainBiome.All");
        }

        [Test]
        public void BiomeHazard_Whirlpool_PullsPlayerTowardCenter()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Place player to the right of hazard center
            state.Players[0].Position = new Vec2(3f, 5f);
            state.Players[0].Velocity = new Vec2(0f, 0f);

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f),
                Radius = 5f,
                Type = BiomeHazardType.Whirlpool,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.1f);

            Assert.Less(state.Players[0].Velocity.x, 0f,
                "Whirlpool should pull player toward center (negative X when player is to the right)");
        }

        [Test]
        public void BiomeHazard_Whirlpool_IgnoresFreefallPlayer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            // Player in freefall (velocity.y <= -2)
            state.Players[0].Position = new Vec2(3f, 5f);
            state.Players[0].Velocity = new Vec2(0f, -3f);

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f),
                Radius = 5f,
                Type = BiomeHazardType.Whirlpool,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            float vxBefore = state.Players[0].Velocity.x;
            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.1f);

            Assert.AreEqual(vxBefore, state.Players[0].Velocity.x, 0.001f,
                "Whirlpool should not affect player in freefall");
        }

        [Test]
        public void BiomeHazard_Whirlpool_DoesNoDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f),
                Radius = 5f,
                Type = BiomeHazardType.Whirlpool,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            float healthBefore = state.Players[0].Health;
            state.DamageEvents.Clear();
            state.Phase = MatchPhase.Playing;

            for (int i = 0; i < 30; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreEqual(healthBefore, state.Players[0].Health, 0.01f,
                "Whirlpool hazard should deal no damage");
        }

    }
}
