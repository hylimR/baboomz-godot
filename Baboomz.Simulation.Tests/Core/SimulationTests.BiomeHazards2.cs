using System;
using NUnit.Framework;
using Baboomz.Simulation;

namespace Baboomz.Tests.Editor
{
    public partial class GameSimulationTests
    {
        [Test]
        public void BiomeHazards_SpawnedAtMatchCreation()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            Assert.Greater(state.BiomeHazards.Count, 0,
                "Biome hazards should be spawned at match creation");
            Assert.IsTrue(state.BiomeHazards[0].Active);
            Assert.AreEqual(state.Biome.HazardType, state.BiomeHazards[0].Type,
                "Hazard type should match biome");
        }

        [Test]
        public void BiomeHazard_Lava_DamagesPlayer()
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
                Type = BiomeHazardType.Lava,
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

            Assert.Less(state.Players[0].Health, healthBefore,
                "Lava hazard should damage player standing in it");
        }

        [Test]
        public void BiomeHazard_Bounce_LaunchesPlayer()
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
                Type = BiomeHazardType.Bounce,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(playerPos.x);
            int py = state.Terrain.WorldToPixelY(playerPos.y - 0.5f);
            state.Terrain.SetSolid(px, py, true);

            float yBefore = state.Players[0].Position.y;

            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.Greater(state.Players[0].Position.y, yBefore + 0.5f,
                "Bounce hazard should launch player upward");
        }

        [Test]
        public void BiomeHazard_Ice_AcceleratesPlayer()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].Velocity = new Vec2(3f, 0f);

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f),
                Radius = 5f,
                Type = BiomeHazardType.Ice,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            state.Phase = MatchPhase.Playing;
            for (int i = 0; i < 10; i++)
                GameSimulation.Tick(state, 0.016f);

            Assert.AreNotEqual(0f, state.Players[0].Velocity.x,
                "Ice hazard should give player sliding velocity");
        }

        [Test]
        public void BiomeHazard_Lava_SkipsInvulnerable()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].IsInvulnerable = true;

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f), Radius = 5f,
                Type = BiomeHazardType.Lava, Active = true
            });

            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            float healthBefore = state.Players[0].Health;
            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.1f);

            Assert.AreEqual(healthBefore, state.Players[0].Health, 0.01f,
                "Lava should not damage invulnerable players");
        }

        [Test]
        public void BiomeHazard_Lava_RespectsArmorMultiplier()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;

            var stateNoArmor = GameSimulation.CreateMatch(config, 42);
            stateNoArmor.Players[0].Position = new Vec2(0f, 5f);
            stateNoArmor.Players[0].ArmorMultiplier = 1f;
            stateNoArmor.BiomeHazards.Clear();
            stateNoArmor.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f), Radius = 5f,
                Type = BiomeHazardType.Lava, Active = true
            });
            int pxNa = stateNoArmor.Terrain.WorldToPixelX(0f);
            int pyNa = stateNoArmor.Terrain.WorldToPixelY(4.5f);
            stateNoArmor.Terrain.SetSolid(pxNa, pyNa, true);

            var stateArmored = GameSimulation.CreateMatch(config, 42);
            stateArmored.Players[0].Position = new Vec2(0f, 5f);
            stateArmored.Players[0].ArmorMultiplier = 3f;
            stateArmored.BiomeHazards.Clear();
            stateArmored.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f), Radius = 5f,
                Type = BiomeHazardType.Lava, Active = true
            });
            int pxA = stateArmored.Terrain.WorldToPixelX(0f);
            int pyA = stateArmored.Terrain.WorldToPixelY(4.5f);
            stateArmored.Terrain.SetSolid(pxA, pyA, true);

            stateNoArmor.Phase = MatchPhase.Playing;
            stateArmored.Phase = MatchPhase.Playing;

            float noArmorStart = stateNoArmor.Players[0].Health;
            float armoredStart = stateArmored.Players[0].Health;

            for (int i = 0; i < 30; i++)
            {
                GameSimulation.Tick(stateNoArmor, 0.016f);
                GameSimulation.Tick(stateArmored, 0.016f);
            }

            float noArmorDmg = noArmorStart - stateNoArmor.Players[0].Health;
            float armoredDmg = armoredStart - stateArmored.Players[0].Health;

            Assert.Greater(noArmorDmg, 0f, "Unarmored player should take lava damage");
            Assert.Greater(armoredDmg, 0f, "Armored player should still take some lava damage");
            Assert.Less(armoredDmg, noArmorDmg * 0.5f,
                "Armored player (3x) should take substantially less lava damage than unarmored");
        }

        [Test]
        public void BiomeHazard_Lava_ZeroArmorMultiplier_DoesNotCauseInfinityDamage()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);

            state.Players[0].Position = new Vec2(0f, 5f);
            state.Players[0].ArmorMultiplier = 0f;
            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(0f, 5f), Radius = 5f,
                Type = BiomeHazardType.Lava, Active = true
            });
            int px = state.Terrain.WorldToPixelX(0f);
            int py = state.Terrain.WorldToPixelY(4.5f);
            state.Terrain.SetSolid(px, py, true);

            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(float.IsInfinity(state.Players[0].Health),
                "Lava with ArmorMultiplier=0 should not produce Infinity damage");
            Assert.IsFalse(float.IsNaN(state.Players[0].Health),
                "Lava with ArmorMultiplier=0 should not produce NaN damage");
        }

        [Test]
        public void BiomeHazard_DisabledWhenTerrainDestroyed()
        {
            var config = SmallConfig();
            config.MineCount = 0;
            config.BarrelCount = 0;
            var state = GameSimulation.CreateMatch(config, 42);
            AILogic.Reset(42);

            float hx = 0f;
            float hy = GamePhysics.FindGroundY(state.Terrain, hx, config.SpawnProbeY, 0.1f);

            state.BiomeHazards.Clear();
            state.BiomeHazards.Add(new BiomeHazardState
            {
                Position = new Vec2(hx, hy),
                Radius = 3f,
                Type = BiomeHazardType.Mud,
                Active = true
            });

            int px = state.Terrain.WorldToPixelX(hx);
            int py = state.Terrain.WorldToPixelY(hy - 0.5f);
            state.Terrain.ClearCircleDestructible(px, py, 20);

            state.Phase = MatchPhase.Playing;
            GameSimulation.Tick(state, 0.016f);

            Assert.IsFalse(state.BiomeHazards[0].Active,
                "Hazard should deactivate when terrain underneath is destroyed");
        }

        [Test]
        public void Chinatown_BiomeExists_InAllArray()
        {
            bool found = false;
            for (int i = 0; i < TerrainBiome.All.Length; i++)
            {
                if (TerrainBiome.All[i].Name == "Chinatown")
                {
                    found = true;
                    Assert.AreEqual(BiomeHazardType.Firecracker, TerrainBiome.All[i].HazardType);
                    Assert.AreEqual(4, TerrainBiome.All[i].HazardCount);
                    break;
                }
            }
            Assert.IsTrue(found, "Chinatown biome should exist in TerrainBiome.All");
        }
    }
}
